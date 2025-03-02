using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Macaron.Optics.Generator.Helper;

namespace Macaron.Optics.Generator;

[Generator]
public class OptionalGenerator : IIncrementalGenerator
{
    #region Static
    private const string OptionalTypeName = "global::Macaron.Optics.Optional";
    private const string OptionalOfTypeName = "global::Macaron.Optics.OptionalOf";

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

        var optionalOfStringBuilder = new StringBuilder();

        optionalOfStringBuilder.AppendLine($"public static class OptionalOfExtensions");
        optionalOfStringBuilder.AppendLine("{");

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

                optionalOfStringBuilder.AppendLine($"    public static {OptionalTypeName}<{MaybeTypeName}<{typeName}>, {memberTypeName}> {member.Name}(");
                optionalOfStringBuilder.AppendLine($"        this {OptionalOfTypeName}<{typeName}> optionLensOf");
                optionalOfStringBuilder.AppendLine("    )");
                optionalOfStringBuilder.AppendLine("    {");
                optionalOfStringBuilder.AppendLine($"        return {OptionalTypeName}<{MaybeTypeName}<{typeName}>, {memberTypeName}>.Of(");
                optionalOfStringBuilder.AppendLine($"            getter: static source => source.IsJust ? {MaybeTypeName}.Just(source.Value.{member.Name}) : {MaybeTypeName}.Nothing<{memberTypeName}>(),");
                optionalOfStringBuilder.AppendLine($"            setter: static (source, value) => source.IsJust");
                optionalOfStringBuilder.AppendLine($"                ? {MaybeTypeName}.Just(source.Value with");
                optionalOfStringBuilder.AppendLine("                {");
                optionalOfStringBuilder.AppendLine($"                    {member.Name} = value,");
                optionalOfStringBuilder.AppendLine("                })");
                optionalOfStringBuilder.AppendLine($"                : {MaybeTypeName}.Nothing<{typeName}>()");
                optionalOfStringBuilder.AppendLine("        );");
                optionalOfStringBuilder.AppendLine("    }");

                if (i < uniqueTypeSymbols.Length - 1)
                {
                    optionalOfStringBuilder.AppendLine();
                }
            }
        }

        optionalOfStringBuilder.AppendLine("}");

        AddSource(sourceProductionContext, "OptionalOfExtensions.g.cs", optionalOfStringBuilder.ToString());

        #region Local Functions
        static void AddSource(SourceProductionContext sourceProductionContext, string hintName, string sourceText)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("// <auto-generated />");
            stringBuilder.AppendLine("#nullable enable");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace Macaron.Optics;");
            stringBuilder.Append(sourceText);

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
        var optionalOfCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is InvocationExpressionSyntax,
                transform: static (generatorSyntaxContext, _) => GetLensOfType(generatorSyntaxContext, OptionalTypeName)
            )
            .Where(static typeName => typeName is not null)
            .Select(static (typeName, _) => typeName!)
            .Collect();

        context.RegisterSourceOutput(
            source: optionalOfCalls,
            action: GenerateSource
        );
    }
    #endregion
}
