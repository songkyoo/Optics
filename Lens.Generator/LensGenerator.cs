using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Macaron.Optics.Generator;

[Generator]
public class LensGenerator : IIncrementalGenerator
{
    #region Static
    private const string LensTypeName = "global::Macaron.Optics.Lens";
    private const string LensOfTypeName = "global::Macaron.Optics.LensOf";
    private const string OptionLensTypeName = "global::Macaron.Optics.OptionLens";
    private const string OptionLensOfTypeName = "global::Macaron.Optics.OptionLensOf";
    private const string MaybeTypeName = "global::Macaron.Functional.Maybe";

    private static string? ToFullyQualifiedName(ISymbol? symbol)
    {
        return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static INamedTypeSymbol? GetLensOfType(GeneratorSyntaxContext generatorSyntaxContext)
    {
        var genericNameSyntax = GetGenericNameFromInvocation((InvocationExpressionSyntax)generatorSyntaxContext.Node);
        if (genericNameSyntax is null)
        {
            return null;
        }

        var methodSymbol = generatorSyntaxContext.SemanticModel
            .GetSymbolInfo(genericNameSyntax).Symbol as IMethodSymbol;
        if (methodSymbol?.IsStatic is not true ||
            ToFullyQualifiedName(methodSymbol.ContainingType) is not LensTypeName or OptionLensTypeName
        )
        {
            return null;
        }

        var typeArgument = genericNameSyntax.TypeArgumentList.Arguments[0];
        var symbolInfo = generatorSyntaxContext.SemanticModel.GetSymbolInfo(typeArgument);
        var namedTypeSymbol = symbolInfo.Symbol as INamedTypeSymbol;

        if (namedTypeSymbol is null)
        {
            return null;
        }

        // Nullable한 형식은 지원하지 않는다.
        if ((namedTypeSymbol.IsValueType && namedTypeSymbol.ToString().EndsWith("?")) ||
            typeArgument.ToString().EndsWith("?")
        )
        {
            return null;
        }

        // with 문을 지원하는 형식만 사용한다.
        return namedTypeSymbol.IsRecord is true || namedTypeSymbol.TypeKind == TypeKind.Struct
            ? namedTypeSymbol
            : null;
    }

    private static GenericNameSyntax? GetGenericNameFromInvocation(
        InvocationExpressionSyntax invocationExpressionSyntax
    )
    {
        return invocationExpressionSyntax.Expression switch
        {
            MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } => genericName,
            GenericNameSyntax genericName => genericName,
            _ => null
        };
    }

    private static bool IsValidProperty(IPropertySymbol propertySymbol)
    {
        if (propertySymbol.NullableAnnotation == NullableAnnotation.Annotated ||
            propertySymbol.GetMethod is null ||
            propertySymbol.IsIndexer
        )
        {
            return false;
        }

        return propertySymbol.SetMethod is not null or { IsReadOnly: true };
    }

    private static bool IsValidField(IFieldSymbol fieldSymbol)
    {
        return  !fieldSymbol.IsConst &&
            !fieldSymbol.IsStatic &&
            !fieldSymbol.IsReadOnly &&
            fieldSymbol.NullableAnnotation != NullableAnnotation.Annotated;
    }

    private static void GenerateSource(
        SourceProductionContext sourceProductionContext,
        ImmutableArray<INamedTypeSymbol> lensTypeSymbols
    )
    {
        var uniqueTypeSymbols = lensTypeSymbols.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToArray();
        if (uniqueTypeSymbols.Length == 0)
        {
            return;
        }

        var lensOfStringBuilder = new StringBuilder();
        var optionLensOfStringBuilder = new StringBuilder();

        lensOfStringBuilder.AppendLine($"    public static class LensOfExtensions");
        lensOfStringBuilder.AppendLine("    {");
        optionLensOfStringBuilder.AppendLine($"    public static class OptionLensOfExtensions");
        optionLensOfStringBuilder.AppendLine("    {");

        for (int i = 0; i < uniqueTypeSymbols.Length; ++i)
        {
            var typeSymbol = uniqueTypeSymbols[i];
            var members = typeSymbol
                .GetMembers()
                .Where(symbol => symbol.DeclaredAccessibility == Accessibility.Public)
                .Where(symbol =>
                    (symbol is IPropertySymbol propertySymbol && IsValidProperty(propertySymbol)) ||
                    (symbol is IFieldSymbol fieldSymbol && IsValidField(fieldSymbol))
                )
                .ToArray();
            if (members.Length == 0)
            {
                continue;
            }

            var typeName = ToFullyQualifiedName(typeSymbol)!;

            foreach (var member in members)
            {
                var memberTypeName = ToFullyQualifiedName(member is IPropertySymbol propertySymbol
                    ? propertySymbol.Type
                    : ((IFieldSymbol)member).Type
                );

                lensOfStringBuilder.AppendLine($"        public static {LensTypeName}<{typeName}, {memberTypeName}> {member.Name}(");
                lensOfStringBuilder.AppendLine($"            this {LensOfTypeName}<{typeName}> lensOf");
                lensOfStringBuilder.AppendLine("        )");
                lensOfStringBuilder.AppendLine("        {");
                lensOfStringBuilder.AppendLine($"            return {LensTypeName}<{typeName}, {memberTypeName}>.Of(");
                lensOfStringBuilder.AppendLine($"                getter: static source => source.{member.Name},");
                lensOfStringBuilder.AppendLine($"                setter: static (source, value) => source with");
                lensOfStringBuilder.AppendLine("                {");
                lensOfStringBuilder.AppendLine($"                    {member.Name} = value,");
                lensOfStringBuilder.AppendLine("                }");
                lensOfStringBuilder.AppendLine("            );");
                lensOfStringBuilder.AppendLine("        }");

                optionLensOfStringBuilder.AppendLine($"        public static {OptionLensTypeName}<{MaybeTypeName}<{typeName}>, {memberTypeName}> {member.Name}(");
                optionLensOfStringBuilder.AppendLine($"            this {OptionLensOfTypeName}<{typeName}> optionLensOf");
                optionLensOfStringBuilder.AppendLine("        )");
                optionLensOfStringBuilder.AppendLine("        {");
                optionLensOfStringBuilder.AppendLine($"            return {OptionLensTypeName}<{MaybeTypeName}<{typeName}>, {memberTypeName}>.Of(");
                optionLensOfStringBuilder.AppendLine($"                getter: static source => source.IsJust ? {MaybeTypeName}.Just(source.Value.{member.Name}) : {MaybeTypeName}.Nothing<{memberTypeName}>(),");
                optionLensOfStringBuilder.AppendLine($"                setter: static (source, value) => source.IsJust");
                optionLensOfStringBuilder.AppendLine($"                    ? {MaybeTypeName}.Just(source.Value with");
                optionLensOfStringBuilder.AppendLine("                    {");
                optionLensOfStringBuilder.AppendLine($"                        {member.Name} = value,");
                optionLensOfStringBuilder.AppendLine("                    })");
                optionLensOfStringBuilder.AppendLine($"                    : {MaybeTypeName}.Nothing<{typeName}>()");
                optionLensOfStringBuilder.AppendLine("            );");
                optionLensOfStringBuilder.AppendLine("        }");

                if (i < uniqueTypeSymbols.Length - 1)
                {
                    lensOfStringBuilder.AppendLine();
                    optionLensOfStringBuilder.AppendLine();
                }
            }
        }

        lensOfStringBuilder.AppendLine("    }");
        optionLensOfStringBuilder.AppendLine("    }");

        AddSource(sourceProductionContext, "LensOfExtensions.g.cs", lensOfStringBuilder.ToString());
        AddSource(sourceProductionContext, "OptionLensOfExtensions.g.cs", optionLensOfStringBuilder.ToString());

        #region Local Functions
        static void AddSource(SourceProductionContext sourceProductionContext, string hintName, string sourceText)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("// <auto-generated />");
            stringBuilder.AppendLine("#nullable enable");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace Macaron.Optics");
            stringBuilder.AppendLine("{");
            stringBuilder.Append(sourceText);
            stringBuilder.AppendLine("}");

            sourceProductionContext.AddSource(
                hintName: hintName,
                sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
            );
        }
        #endregion
    }
    #endregion

    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var lensOfCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is InvocationExpressionSyntax,
                transform: static (generatorSyntaxContext, _) => GetLensOfType(generatorSyntaxContext)
            )
            .Where(static typeName => typeName is not null)
            .Select(static (typeName, _) => typeName!)
            .Collect();

        context.RegisterSourceOutput(
            source: lensOfCalls,
            action: GenerateSource
        );
    }
    #endregion
}
