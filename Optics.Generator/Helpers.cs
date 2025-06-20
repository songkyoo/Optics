using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;
using static Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions;

namespace Macaron.Optics.Generator;

internal static class Helpers
{
    #region Constants
    private const string LensOfTypeString = "global::Macaron.Optics.Lens";
    private const string OptionalOfTypeString = "global::Macaron.Optics.Optional";
    private const string LensOfAttributeName = "Macaron.Optics.LensOfAttribute";
    private const string OptionalOfAttributeName = "Macaron.Optics.OptionalOfAttribute";
    private const string MaybeTypeString = "global::Macaron.Functional.Maybe";
    #endregion

    #region Types
    public record AttributeContext(
        ImmutableArray<Diagnostic> Diagnostics
    )
    {
        #region Static
        public static readonly AttributeContext Empty = new(
            Diagnostics: ImmutableArray<Diagnostic>.Empty
        );
        #endregion
    }

    public sealed record LensOfAttributeContext(
        INamedTypeSymbol ContainingTypeSymbol,
        INamedTypeSymbol TypeSymbol,
        ImmutableArray<Diagnostic> Diagnostics
    ) : AttributeContext(Diagnostics);

    public sealed record OptionalOfAttributeContext(
        INamedTypeSymbol ContainingTypeSymbol,
        INamedTypeSymbol TypeSymbol,
        ImmutableArray<Diagnostic> Diagnostics
    ) : AttributeContext(Diagnostics);

    public record TypeContext(
        ImmutableArray<Diagnostic> Diagnostics
    )
    {
        #region Static
        public static readonly TypeContext Empty = new(
            Diagnostics: ImmutableArray<Diagnostic>.Empty
        );
        #endregion
    }

    public sealed record LensOfTypeContext(
        INamedTypeSymbol Symbol,
        ImmutableArray<Diagnostic> Diagnostics
    ) : TypeContext(Diagnostics);

    public sealed record OptionalOfTypeContext(
        INamedTypeSymbol Symbol,
        ImmutableArray<Diagnostic> Diagnostics
    ) : TypeContext(Diagnostics);
    #endregion

    #region Diagnostics
    private static readonly DiagnosticDescriptor LensTargetTypeCannotBeNullableRule = new(
        id: "MOPT0001",
        title: "Lens target type cannot be nullable",
        messageFormat: "Type '{0}' is nullable. Nullable types are not supported as lens targets.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    private static readonly DiagnosticDescriptor LensTargetTypeMustSupportWithExpressionRule = new(
        id: "MOPT0002",
        title: "Lens target type must support 'with' expression",
        messageFormat: "Type '{0}' must be a record or struct to be used as a lens target",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    private static readonly DiagnosticDescriptor LensOfAttributeMustBeOnStaticClassRule = new(
        id: "MOPT0003",
        title: "LensOf attribute must be applied to a static class",
        messageFormat: "Class '{0}' must be static to use LensOf attribute",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    private static readonly DiagnosticDescriptor LensOfAttributeTargetMustSupportWithExpressionRule = new(
        id: "MOPT0004",
        title: "LensOf attribute target type must support 'with' expression",
        messageFormat: "Type '{0}' must be a record or struct to be used with LensOf attribute",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    #endregion

    #region Methods
    public static TypeContext GetTypeContext(GeneratorSyntaxContext generatorSyntaxContext)
    {
        if (generatorSyntaxContext.Node is not InvocationExpressionSyntax expressionSyntax)
        {
            return TypeContext.Empty;
        }

        if (GetGenericNameFromInvocation(expressionSyntax) is not { } genericNameSyntax)
        {
            return TypeContext.Empty;
        }

        var semanticModel = generatorSyntaxContext.SemanticModel;
        var methodSymbol = semanticModel.GetSymbolInfo(genericNameSyntax).Symbol as IMethodSymbol;
        if (methodSymbol?.IsStatic is not true || methodSymbol.Name != "Of")
        {
            return TypeContext.Empty;
        }

        var typeArgumentList = genericNameSyntax.TypeArgumentList;
        if (typeArgumentList.Arguments is not [{ } typeArgument] ||
            semanticModel.GetSymbolInfo(typeArgument).Symbol is not INamedTypeSymbol typeSymbol
        )
        {
            return TypeContext.Empty;
        }

        const int lensOfType = 1;
        const int optionalOfType = 2;

        var type = methodSymbol.ContainingType.ToDisplayString(FullyQualifiedFormat) switch
        {
            LensOfTypeString => lensOfType,
            OptionalOfTypeString => optionalOfType,
            _ => 0,
        };
        if (type == 0)
        {
            return TypeContext.Empty;
        }

        // Nullable한 형식은 지원하지 않는다.
        if ((typeSymbol.IsValueType && typeSymbol.ToString().EndsWith("?")) ||
            typeArgument.ToString().EndsWith("?")
        )
        {
            return TypeContext.Empty with
            {
                Diagnostics = ImmutableArray.Create(Diagnostic.Create(
                    descriptor: LensTargetTypeCannotBeNullableRule,
                    location: typeArgument.GetLocation(),
                    messageArgs: [typeArgument]
                )),
            };
        }

        // with 문을 지원하는 형식만 사용한다.
        if (typeSymbol is { IsRecord: false, TypeKind: not TypeKind.Struct })
        {
            return TypeContext.Empty with
            {
                Diagnostics = ImmutableArray.Create(Diagnostic.Create(
                    descriptor: LensTargetTypeMustSupportWithExpressionRule,
                    location: typeArgument.GetLocation(),
                    messageArgs: [typeArgument]
                )),
            };
        }

        return type switch
        {
            lensOfType => new LensOfTypeContext(
                Symbol: typeSymbol,
                Diagnostics: ImmutableArray<Diagnostic>.Empty
            ),
            optionalOfType => new OptionalOfTypeContext(
                Symbol: typeSymbol,
                Diagnostics: ImmutableArray<Diagnostic>.Empty
            ),
            _ => throw new InvalidOperationException($"Invalid type: {type}"),
        };

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

    public static AttributeContext GetAttributeContext(
        GeneratorSyntaxContext context,
        HashSet<INamedTypeSymbol> visitedTypes
    )
    {
        if (context.Node is not TypeDeclarationSyntax declarationSyntax)
        {
            return AttributeContext.Empty;
        }

        if (context.SemanticModel.GetDeclaredSymbol(declarationSyntax) is not { } containingTypeSymbol)
        {
            return AttributeContext.Empty;
        }

        var attribute = containingTypeSymbol
            .GetAttributes()
            .FirstOrDefault(static attributeData => attributeData.AttributeClass?.ToDisplayString()
                is LensOfAttributeName
                or OptionalOfAttributeName
            );
        if (attribute is null)
        {
            return AttributeContext.Empty;
        }

        if (!visitedTypes.Add(containingTypeSymbol))
        {
            return AttributeContext.Empty;
        }

        if (!containingTypeSymbol.IsStatic)
        {
            return AttributeContext.Empty with
            {
                Diagnostics = ImmutableArray.Create(Diagnostic.Create(
                    descriptor: LensOfAttributeMustBeOnStaticClassRule,
                    location: attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    messageArgs: [containingTypeSymbol]
                )),
            };
        }

        var typeSymbol = attribute.ConstructorArguments is [{ Value: INamedTypeSymbol symbolArgument }]
            ? symbolArgument
            : containingTypeSymbol.ContainingType;

        if (typeSymbol is { IsRecord: false, TypeKind: not TypeKind.Struct })
        {
            return AttributeContext.Empty with
            {
                Diagnostics = ImmutableArray.Create(Diagnostic.Create(
                    descriptor: LensOfAttributeTargetMustSupportWithExpressionRule,
                    location: attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    messageArgs: [typeSymbol]
                )),
            };
        }

        return attribute.AttributeClass?.ToDisplayString() switch
        {
            LensOfAttributeName => new LensOfAttributeContext(
                ContainingTypeSymbol: containingTypeSymbol,
                TypeSymbol: typeSymbol,
                Diagnostics: ImmutableArray<Diagnostic>.Empty
            ),
            OptionalOfAttributeName => new OptionalOfAttributeContext(
                ContainingTypeSymbol: containingTypeSymbol,
                TypeSymbol: typeSymbol,
                Diagnostics: ImmutableArray<Diagnostic>.Empty
            ),
            _ => throw new InvalidOperationException($"Invalid type: {attribute.AttributeClass}"),
        };
    }

    public static ImmutableArray<(string, ImmutableArray<string>)> GenerateLensOfMembers(INamedTypeSymbol typeSymbol)
    {
        return GenerateLensOfMembers(
            typeSymbol,
            getSourceByMember: (typeName, memberTypeName, memberName, isNullable) =>
            {
                var memberDeclaration = isNullable
                    ? $"{LensOfTypeString}<{typeName}, {MaybeTypeString}<{memberTypeName}>> {memberName}"
                    : $"{LensOfTypeString}<{typeName}, {memberTypeName}> {memberName}";

                var lines = isNullable
                    ? new List<string>
                    {
                        $"{LensOfTypeString}<{typeName}, {MaybeTypeString}<{memberTypeName}>>.Of(",
                        $"    getter: static source => source is {{ {memberName}: {{ }} value }}",
                        $"        ? {MaybeTypeString}.Just(value)",
                        $"        : {MaybeTypeString}.Nothing<{memberTypeName}>(),",
                        $"    setter: static (source, value) => source with",
                        $"    {{",
                        $"        {memberName} = value is {{ IsJust: true, Value: var value2 }} ? value2 : null,",
                        $"    }}",
                        $");",
                    }
                    :  new List<string>
                    {
                        $"{LensOfTypeString}<{typeName}, {memberTypeName}>.Of(",
                        $"    getter: static source => source.{memberName},",
                        $"    setter: static (source, value) => source with",
                        $"    {{",
                        $"        {memberName} = value,",
                        $"    }}",
                        $");",
                    };

                return (memberDeclaration, lines.ToImmutableArray());
            }
        );
    }

    public static ImmutableArray<(string, ImmutableArray<string>)> GenerateOptionalOfMembers(
        INamedTypeSymbol typeSymbol
    )
    {
        return GenerateLensOfMembers(
            typeSymbol,
            getSourceByMember: (typeName, memberTypeName, memberName, isNullable) =>
            {
                var memberDeclaration = isNullable
                    ? $"{LensOfTypeString}<{MaybeTypeString}<{typeName}>, {MaybeTypeString}<{memberTypeName}>> {memberName}"
                    : $"{OptionalOfTypeString}<{MaybeTypeString}<{typeName}>, {memberTypeName}> {memberName}";

                var lines = isNullable
                    ? ImmutableArray.Create(
                        $"{LensOfTypeString}<{MaybeTypeString}<{typeName}>, {MaybeTypeString}<{memberTypeName}>>.Of(",
                        $"    getter: static source => source is {{ IsJust: true, Value: {{ }} value }}",
                        $"        ? value.{memberName} is {{ }} value2",
                        $"            ? {MaybeTypeString}.Just(value2)",
                        $"            : {MaybeTypeString}.Nothing<{memberTypeName}>()",
                        $"        : {MaybeTypeString}.Nothing<{memberTypeName}>(),",
                        $"    setter: static (source, value) => source.IsJust",
                        $"        ? {MaybeTypeString}.Just(source.Value with",
                        $"        {{",
                        $"            {memberName} = value is {{ IsJust: true, Value: var value2 }} ? value2 : null,",
                        $"        }})",
                        $"        : {MaybeTypeString}.Nothing<{typeName}>()",
                        $");"
                    )
                    : ImmutableArray.Create(
                        $"{OptionalOfTypeString}<{MaybeTypeString}<{typeName}>, {memberTypeName}>.Of(",
                        $"    optionalGetter: static source => source.IsJust",
                        $"        ? {MaybeTypeString}.Just(source.Value.{memberName})",
                        $"        : {MaybeTypeString}.Nothing<{memberTypeName}>(),",
                        $"    setter: static (source, value) => source.IsJust",
                        $"        ? {MaybeTypeString}.Just(source.Value with",
                        $"        {{",
                        $"            {memberName} = value,",
                        $"        }})",
                        $"        : {MaybeTypeString}.Nothing<{typeName}>()",
                        $");"
                    );

                return (memberDeclaration, lines);
            }
        );
    }

    public static void AddSource(
        SourceProductionContext sourceProductionContext,
        string lensOfTypeName,
        ImmutableArray<INamedTypeSymbol> typeSymbols,
        Func<INamedTypeSymbol, ImmutableArray<(string, ImmutableArray<string>)>> generateMembers
    )
    {
        var uniqueTypeSymbols = typeSymbols
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .ToImmutableArray();
        if (uniqueTypeSymbols.Length == 0)
        {
            return;
        }

        var stringBuilder = CreateStringBuilderWithFileHeader();

        // begin namespace
        stringBuilder.AppendLine($"namespace Macaron.Optics");
        stringBuilder.AppendLine($"{{");

        // begin extension methods
        stringBuilder.AppendLine($"    internal static class {lensOfTypeName}Extensions");
        stringBuilder.AppendLine($"    {{");

        for (int i = 0; i < uniqueTypeSymbols.Length; ++i)
        {
            var typeSymbol = uniqueTypeSymbols[i];

            var members = generateMembers(typeSymbol);
            if (members.Length == 0)
            {
                continue;
            }

            var typeName = typeSymbol.ToDisplayString(FullyQualifiedFormat);

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
        stringBuilder.AppendLine($"    }}");

        // end namespace
        stringBuilder.AppendLine($"}}");

        sourceProductionContext.AddSource(
            hintName: $"{lensOfTypeName}Extensions.g.cs",
            sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
        );
    }

    public static void AddSource(
        SourceProductionContext sourceProductionContext,
        (INamedTypeSymbol ContainingTypeSymbol, INamedTypeSymbol TypeSymbol) attributeContext,
        Func<INamedTypeSymbol, ImmutableArray<(string, ImmutableArray<string>)>> generateMembers
    )
    {
        var (containingTypeSymbol, typeSymbol) = attributeContext;

        var members = generateMembers(typeSymbol);
        if (members.IsDefaultOrEmpty)
        {
            return;
        }

        var stringBuilder = CreateStringBuilderWithFileHeader();

        // begin namespace
        var hasNamespace = !containingTypeSymbol.ContainingNamespace.IsGlobalNamespace;
        if (hasNamespace)
        {
            stringBuilder.AppendLine($"namespace {containingTypeSymbol.ContainingNamespace.ToDisplayString()}");
            stringBuilder.AppendLine($"{{");
        }

        // get nestedTypes
        var nestedTypes = new List<INamedTypeSymbol>();
        var parentType = containingTypeSymbol.ContainingType;
        while (parentType != null)
        {
            nestedTypes.Add(parentType);
            parentType = parentType.ContainingType;
        }

        var depthSpacerText = hasNamespace ? "    " : "";

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

        // write members
        depthSpacerText += "    ";

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

        // end containingType
        stringBuilder.AppendLine($"{depthSpacerText}}}");

        // end nestedTypes
        for (var i = 0; i < nestedTypes.Count; ++i)
        {
            depthSpacerText = depthSpacerText[..^4];

            stringBuilder.AppendLine($"{depthSpacerText}}}");
        }

        // end namespace
        if (hasNamespace)
        {
            stringBuilder.AppendLine($"}}");
        }

        sourceProductionContext.AddSource(
            hintName: GetHintName(containingTypeSymbol),
            sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
        );

        #region Local Functions
        static string GetPartialTypeDeclarationString(INamedTypeSymbol typeSymbol)
        {
            var typeKindString = GetTypeKindString(typeSymbol);
            var typeNameString = typeSymbol.ToDisplayString(MinimallyQualifiedFormat);

            return $"partial {typeKindString} {typeNameString}";
        }

        static string GetTypeKindString(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol.IsRecord)
            {
                return typeSymbol.TypeKind is TypeKind.Struct ? "record struct" : "record";
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
            var assemblyName = typeSymbol.ContainingAssembly != null ? $"{typeSymbol.ContainingAssembly}," : "";
            var qualifiedName = typeSymbol.ToDisplayString(FullyQualifiedFormat);

            const uint fnvPrime = 16777619;
            const uint offsetBasis = 2166136261;

            var bytes = Encoding.UTF8.GetBytes($"{assemblyName}, {qualifiedName}");
            uint hash = offsetBasis;

            foreach (var b in bytes)
            {
                hash ^= b;
                hash *= fnvPrime;
            }

            return $"{typeSymbol.Name}_{typeSymbol.Arity}.{hash:x8}.g.cs";
        }
        #endregion
    }

    private static ImmutableArray<(string, ImmutableArray<string>)> GenerateLensOfMembers(
        INamedTypeSymbol typeSymbol,
        Func<string, string, string, bool, (string, ImmutableArray<string>)> getSourceByMember
    )
    {
        var members = GetValidMemberSymbols(typeSymbol);
        if (members.Length == 0)
        {
            return ImmutableArray<(string, ImmutableArray<string>)>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<(string, ImmutableArray<string>)>();
        var typeName = typeSymbol.ToDisplayString(FullyQualifiedFormat);

        foreach (var member in members)
        {
            var isNullable = IsNullable(member);
            var memberTypeName = GetMemberTypeName(member, isNullable);

            builder.Add(getSourceByMember(typeName, memberTypeName, GetEscapedKeyword(member.Name), isNullable));
        }

        return builder.ToImmutable();

        #region Local Functions
        static string GetMemberTypeName(ISymbol symbol, bool isNullable)
        {
            var underlyingType = GetUnderlyingType(symbol is IPropertySymbol propertySymbol
                ? propertySymbol.Type
                : ((IFieldSymbol)symbol).Type
            );
            var format = FullyQualifiedFormat.WithMiscellaneousOptions(isNullable
                ? None
                : IncludeNullableReferenceTypeModifier
            );
            return underlyingType.ToDisplayString(format);

            #region Local Functions
            static ISymbol GetUnderlyingType(ISymbol symbol)
            {
                return symbol
                    is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedTypeSymbol
                    ? namedTypeSymbol.TypeArguments[0]
                    : symbol;
            }
            #endregion
        }

        static bool IsNullable(ISymbol symbol)
        {
            return symbol is
                IPropertySymbol { Type.NullableAnnotation: NullableAnnotation.Annotated } or
                IFieldSymbol { Type.NullableAnnotation: NullableAnnotation.Annotated };
        }
        #endregion
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
        if (propertySymbol.IsStatic ||
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
        return fieldSymbol is
        {
            IsConst: false,
            IsStatic: false,
            IsReadOnly: false,
        };
    }

    private static StringBuilder CreateStringBuilderWithFileHeader()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("// <auto-generated />");
        stringBuilder.AppendLine("#nullable enable");
        stringBuilder.AppendLine();

        return stringBuilder;
    }

    private static string GetEscapedKeyword(string keyword)
    {
        return GetKeywordKind(keyword) != SyntaxKind.None || GetContextualKeywordKind(keyword) != SyntaxKind.None
            ? "@" + keyword
            : keyword;
    }
    #endregion
}
