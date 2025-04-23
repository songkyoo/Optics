using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Macaron.Optics.Generator;

internal static class Helper
{
    #region Constants
    public const string LensTypeName = "global::Macaron.Optics.Lens";
    public const string OptionalTypeName = "global::Macaron.Optics.Optional";
    private const string MaybeTypeName = "global::Macaron.Functional.Maybe";
    #endregion

    #region Types
    public sealed record LensOfContext(
        INamedTypeSymbol ContainingTypeSymbol,
        INamedTypeSymbol TargetTypeSymbol
    );
    #endregion

    #region Methods
    public static INamedTypeSymbol? GetLensOfType(GeneratorSyntaxContext generatorSyntaxContext, string containingType)
    {
        var genericNameSyntax = GetGenericNameFromInvocation((InvocationExpressionSyntax)generatorSyntaxContext.Node);
        if (genericNameSyntax is null)
        {
            return null;
        }

        var semanticModel = generatorSyntaxContext.SemanticModel;
        var methodSymbol = semanticModel.GetSymbolInfo(genericNameSyntax).Symbol as IMethodSymbol;
        if (methodSymbol?.IsStatic is not true ||
            methodSymbol.Name != "Of" ||
            ToFullyQualifiedName(methodSymbol.ContainingType) != containingType
        )
        {
            return null;
        }

        var typeArgumentList = genericNameSyntax.TypeArgumentList;
        if (typeArgumentList.Arguments.Count != 1)
        {
            return null;
        }

        var typeArgument = genericNameSyntax.TypeArgumentList.Arguments[0];
        var symbolInfo = semanticModel.GetSymbolInfo(typeArgument);
        if (symbolInfo.Symbol is not INamedTypeSymbol namedTypeSymbol)
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
        return namedTypeSymbol.IsRecord || namedTypeSymbol.TypeKind == TypeKind.Struct
            ? namedTypeSymbol
            : null;

        #region Local Functions
        static GenericNameSyntax? GetGenericNameFromInvocation(
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
        #endregion
    }

    public static LensOfContext? GetClassWithLensOfAttribute(GeneratorSyntaxContext context, string lensOfAttributeName)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!typeSymbol.IsStatic || typeSymbol.IsGenericType)
        {
            return null;
        }

        var lensOfAttribute = typeSymbol
            .GetAttributes()
            .FirstOrDefault(attributeData => ToFullyQualifiedName(attributeData.AttributeClass) == lensOfAttributeName);
        if (lensOfAttribute is null)
        {
            return null;
        }

        var targetTypeSymbol = lensOfAttribute.ConstructorArguments.Length == 1
            ? lensOfAttribute.ConstructorArguments[0].Value as INamedTypeSymbol
            : typeSymbol.ContainingType;

        return targetTypeSymbol == null ? null : new LensOfContext(
            ContainingTypeSymbol: typeSymbol,
            TargetTypeSymbol: targetTypeSymbol
        );
    }

    public static ImmutableArray<(string, ImmutableArray<string>)> GenerateLensOfMembers(INamedTypeSymbol typeSymbol)
    {
        return GenerateLensOfMembers(typeSymbol, getSourceByMember: (typeName, memberTypeName, memberName) =>
        {
            var lines = new List<string>
            {
                $"{LensTypeName}<{typeName}, {memberTypeName}>.Of(",
                $"    getter: static source => source.{memberName},",
                $"    setter: static (source, value) => source with",
                $"    {{",
                $"        {memberName} = value,",
                $"    }}",
                $");",
            };

            return ($"{LensTypeName}<{typeName}, {memberTypeName}> {memberName}", lines.ToImmutableArray());
        });
    }

    public static ImmutableArray<(string, ImmutableArray<string>)> GenerateOptionalOfMembers(INamedTypeSymbol typeSymbol)
    {
        return GenerateLensOfMembers(typeSymbol, getSourceByMember: (typeName, memberTypeName, memberName) =>
        {
            var lines = ImmutableArray.Create(
                $"{OptionalTypeName}<{MaybeTypeName}<{typeName}>, {memberTypeName}>.Of(",
                $"    optionalGetter: static source => source.IsJust",
                $"        ? {MaybeTypeName}.Just(source.Value.{memberName})",
                $"        : {MaybeTypeName}.Nothing<{memberTypeName}>(),",
                $"    setter: static (source, value) => source.IsJust",
                $"        ? {MaybeTypeName}.Just(source.Value with",
                $"        {{",
                $"            {memberName} = value,",
                $"        }})",
                $"        : {MaybeTypeName}.Nothing<{typeName}>()",
                $");"
            );

            return ($"{OptionalTypeName}<{MaybeTypeName}<{typeName}>, {memberTypeName}> {memberName}", lines);
        });
    }

    public static void AddSource(
        SourceProductionContext sourceProductionContext,
        string lensOfTypeName,
        ImmutableArray<INamedTypeSymbol> lensTypeSymbols,
        Func<INamedTypeSymbol, ImmutableArray<(string, ImmutableArray<string>)>> generateLensOfMembers
    )
    {
        var uniqueTypeSymbols = lensTypeSymbols.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToArray();
        if (uniqueTypeSymbols.Length == 0)
        {
            return;
        }

        var stringBuilder = CreateStringBuilderWithFileHeader();

        // begin namespace
        stringBuilder.AppendLine("namespace Macaron.Optics");
        stringBuilder.AppendLine("{");

        // begin extension methods
        stringBuilder.AppendLine($"    internal static class {lensOfTypeName}Extensions");
        stringBuilder.AppendLine($"    {{");

        for (int i = 0; i < uniqueTypeSymbols.Length; ++i)
        {
            var typeSymbol = uniqueTypeSymbols[i];

            var members = generateLensOfMembers(typeSymbol);
            if (members.Length == 0)
            {
                continue;
            }

            var typeName = ToFullyQualifiedName(typeSymbol)!;

            for (int j = 0; j < members.Length; ++j)
            {
                var (memberDeclaration, lines) = members[j];

                stringBuilder.AppendLine($"        public static {memberDeclaration}(");
                stringBuilder.AppendLine($"            this {lensOfTypeName}<{typeName}> {char.ToLower(lensOfTypeName[0])}{lensOfTypeName[1..]}");
                stringBuilder.AppendLine($"        )");
                stringBuilder.AppendLine($"        {{");

                stringBuilder.AppendLine($"            return {lines[0]}");
                foreach (var line in lines.Skip(1))
                {
                    stringBuilder.AppendLine($"            {line}");
                }

                stringBuilder.AppendLine($"        }}");

                if (j < members.Length - 1)
                {
                    stringBuilder.AppendLine();
                }
            }

            if (i < uniqueTypeSymbols.Length - 1)
            {
                stringBuilder.AppendLine();
            }
        }

        // end extension methods
        stringBuilder.AppendLine("    }");

        // end namespace
        stringBuilder.AppendLine("}");

        sourceProductionContext.AddSource(
            hintName: $"{lensOfTypeName}Extensions.g.cs",
            sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
        );
    }

    public static void AddSource(
        SourceProductionContext sourceProductionContext,
        LensOfContext lensOfContext,
        Func<INamedTypeSymbol, ImmutableArray<(string, ImmutableArray<string>)>> generateLensOfMembers
    )
    {
        var (containingTypeSymbol, targetTypeSymbol) = lensOfContext;
        var stringBuilder = CreateStringBuilderWithFileHeader();

        // begin namespace
        stringBuilder.AppendLine($"namespace {containingTypeSymbol.ContainingNamespace.ToDisplayString()}");
        stringBuilder.AppendLine($"{{");

        // get nestedTypes
        var nestedTypes = new List<INamedTypeSymbol>();
        var parentType = containingTypeSymbol.ContainingType;
        while (parentType != null)
        {
            nestedTypes.Add(parentType);
            parentType = parentType.ContainingType;
        }

        var depthSpacerText = "    ";

        // begin nestedTypes
        for (var i = nestedTypes.Count - 1; i >= 0; --i)
        {
            var nestedType = nestedTypes[i];

            stringBuilder.AppendLine($"{depthSpacerText}{GetPartialTypeDeclarationString(nestedType)}");
            stringBuilder.AppendLine($"{depthSpacerText}{{");

            depthSpacerText += "    ";
        }

        // begin containingType
        stringBuilder.AppendLine($"{depthSpacerText}{GetPartialTypeDeclarationString(containingTypeSymbol)}");
        stringBuilder.AppendLine($"{depthSpacerText}{{");

        // generate targetType members
        depthSpacerText += "    ";

        var members = generateLensOfMembers(targetTypeSymbol);
        for (var i = 0; i < members.Length; ++i)
        {
            var (memberDeclaration, lines) = members[i];

            stringBuilder.AppendLine($"{depthSpacerText}public static readonly {memberDeclaration} = {lines[0]}");
            foreach (var line in lines.Skip(1))
            {
                stringBuilder.AppendLine($"{depthSpacerText}{line}");
            }

            if (i < members.Length - 1)
            {
                stringBuilder.AppendLine();
            }
        }

        depthSpacerText = depthSpacerText[..^4];

        // end containedType
        stringBuilder.AppendLine($"{depthSpacerText}}}");

        // end nestedTypes
        for (var i = 0; i < nestedTypes.Count; ++i)
        {
            depthSpacerText = depthSpacerText[..^4];

            stringBuilder.AppendLine($"{depthSpacerText}}}");
        }

        // end namespace
        stringBuilder.AppendLine($"}}");

        sourceProductionContext.AddSource(
            hintName: GetHintName(targetTypeSymbol),
            sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
        );

        #region Local Functions
        static string GetPartialTypeDeclarationString(INamedTypeSymbol typeSymbol)
        {
            var typeKindString = GetTypeKindString(typeSymbol);
            var typeNameString = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            return $"partial {typeKindString} {typeNameString}";
        }

        static string GetTypeKindString(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol.IsRecord)
            {
                return "record";
            }

            return typeSymbol.TypeKind switch
            {
                TypeKind.Class => "class",
                TypeKind.Struct => "struct",
                TypeKind.Interface => "interface",
                _ => throw new InvalidOperationException($"Invalid type kind: {typeSymbol.TypeKind}")
            };
        }

        static string GetHintName(INamedTypeSymbol typeSymbol)
        {
            var qualifiedName = ToFullyQualifiedName(typeSymbol)!;

            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(qualifiedName);
            var hash = sha.ComputeHash(bytes);
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()[..8];

            return $"{hashString}_{typeSymbol.Name}_{typeSymbol.Arity}.g.cs";
        }
        #endregion
    }

    private static ImmutableArray<(string, ImmutableArray<string>)> GenerateLensOfMembers(
        INamedTypeSymbol typeSymbol,
        Func<string, string, string, (string, ImmutableArray<string>)> getSourceByMember
    )
    {
        var members = GetValidMemberSymbols(typeSymbol);
        if (members.Length == 0)
        {
            return ImmutableArray<(string, ImmutableArray<string>)>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<(string, ImmutableArray<string>)>();
        var typeName = ToFullyQualifiedName(typeSymbol)!;

        foreach (var member in members)
        {
            var memberTypeName = ToFullyQualifiedName(member is IPropertySymbol propertySymbol
                ? propertySymbol.Type
                : ((IFieldSymbol)member).Type
            );
            builder.Add(getSourceByMember(typeName, memberTypeName!, member.Name));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<ISymbol> GetValidMemberSymbols(INamedTypeSymbol typeSymbol)
    {
        var result = new List<ISymbol>();

        for (var current = typeSymbol; current != null; current = current.BaseType)
        {
            var members = current
                .GetMembers()
                .Where(symbol => symbol.DeclaredAccessibility == Accessibility.Public)
                .Where(symbol =>
                    (symbol is IPropertySymbol propertySymbol && IsValidProperty(propertySymbol)) ||
                    (symbol is IFieldSymbol fieldSymbol && IsValidField(fieldSymbol))
                );

            result.AddRange(members);
        }

        return result.Distinct(SymbolEqualityComparer.Default).ToImmutableArray();
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
        return
            !fieldSymbol.IsConst &&
            !fieldSymbol.IsStatic &&
            !fieldSymbol.IsReadOnly &&
            fieldSymbol.NullableAnnotation != NullableAnnotation.Annotated;
    }

    private static StringBuilder CreateStringBuilderWithFileHeader()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("// <auto-generated />");
        stringBuilder.AppendLine("#nullable enable");
        stringBuilder.AppendLine();

        return stringBuilder;
    }

    private static string? ToFullyQualifiedName(ISymbol? symbol)
    {
        return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
    #endregion
}
