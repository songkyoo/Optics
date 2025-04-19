using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Macaron.Optics.Generator;

internal static class Helper
{
    #region Constants
    public const string MaybeTypeName = "global::Macaron.Functional.Maybe";
    public const string LensTypeName = "global::Macaron.Optics.Lens";
    public const string OptionalTypeName = "global::Macaron.Optics.Optional";
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

    public static ImmutableArray<(string, string[])> GenerateLensOfMembers(INamedTypeSymbol typeSymbol)
    {
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
            return ImmutableArray<(string, string[])>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<(string, string[])>();
        var typeName = ToFullyQualifiedName(typeSymbol)!;

        for (int j = 0; j < members.Length; ++j)
        {
            var member = members[j];
            var memberTypeName = ToFullyQualifiedName(member is IPropertySymbol propertySymbol
                ? propertySymbol.Type
                : ((IFieldSymbol)member).Type
            );

            var lines = new List<string>
            {
                $"{LensTypeName}<{typeName}, {memberTypeName}>.Of(",
                $"    getter: static source => source.{member.Name},",
                $"    setter: static (source, value) => source with",
                $"    {{",
                $"        {member.Name} = value,",
                $"    }}",
                $");"
            };

            builder.Add(($"{LensTypeName}<{typeName}, {memberTypeName}> {member.Name}", lines.ToArray()));
        }

        return builder.ToImmutable();
    }

    public static ImmutableArray<(string, string[])> GenerateOptionalOfMembers(INamedTypeSymbol typeSymbol)
    {
        var members = GetValidMemberSymbols(typeSymbol);
        if (members.Length == 0)
        {
            return ImmutableArray<(string, string[])>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<(string, string[])>();
        var typeName = ToFullyQualifiedName(typeSymbol)!;

        foreach (var member in members)
        {
            var memberTypeName = ToFullyQualifiedName(member is IPropertySymbol propertySymbol
                ? propertySymbol.Type
                : ((IFieldSymbol)member).Type
            );

            var lines = new List<string>
            {
                $"{OptionalTypeName}<{MaybeTypeName}<{typeName}>, {memberTypeName}>.Of(",
                $"    optionalGetter: static source => source.IsJust",
                $"        ? {MaybeTypeName}.Just(source.Value.{member.Name})",
                $"        : {MaybeTypeName}.Nothing<{memberTypeName}>(),",
                $"    setter: static (source, value) => source.IsJust",
                $"        ? {MaybeTypeName}.Just(source.Value with",
                $"        {{",
                $"            {member.Name} = value,",
                $"        }})",
                $"        : {MaybeTypeName}.Nothing<{typeName}>()",
                $");",
            };

            builder.Add(($"{OptionalTypeName}<{MaybeTypeName}<{typeName}>, {memberTypeName}> {member.Name}", lines.ToArray()));
        }

        return builder.ToImmutable();
    }

    public static StringBuilder CreateStringBuilderWithFileHeader()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("// <auto-generated />");
        stringBuilder.AppendLine("#nullable enable");
        stringBuilder.AppendLine();

        return stringBuilder;
    }

    public static string? ToFullyQualifiedName(ISymbol? symbol)
    {
        return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public static void AddSource(
        SourceProductionContext sourceProductionContext,
        LensOfContext lensOfContext,
        Func<INamedTypeSymbol, ImmutableArray<(string, string[])>> generateLensOfMembers
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

    public static ImmutableArray<ISymbol> GetValidMemberSymbols(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol
            .GetMembers()
            .Where(symbol => symbol.DeclaredAccessibility == Accessibility.Public)
            .Where(symbol =>
                (symbol is IPropertySymbol propertySymbol && IsValidProperty(propertySymbol)) ||
                (symbol is IFieldSymbol fieldSymbol && IsValidField(fieldSymbol))
            )
            .ToImmutableArray();
    }

    public static bool IsValidProperty(IPropertySymbol propertySymbol)
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

    public static bool IsValidField(IFieldSymbol fieldSymbol)
    {
        return !fieldSymbol.IsConst &&
            !fieldSymbol.IsStatic &&
            !fieldSymbol.IsReadOnly &&
            fieldSymbol.NullableAnnotation != NullableAnnotation.Annotated;
    }
    #endregion
}
