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
    public const string LensOfAttributeName = "Macaron.Optics.LensOfAttribute";
    public const string OptionalOfAttributeName = "Macaron.Optics.OptionalOfAttribute";

    private const string LensOfTypeString = "global::Macaron.Optics.Lens";
    private const string OptionalOfTypeString = "global::Macaron.Optics.Optional";
    private const string MaybeTypeString = "global::Macaron.Functional.Maybe";
    #endregion

    #region Types
    public abstract record AnalysisResult<TContext>
    {
        public sealed record Success(TContext Context) : AnalysisResult<TContext>;

        public sealed record Failure(Diagnostic Diagnostic) : AnalysisResult<TContext>;
    }

    public abstract record AttributeContext;

    public sealed record LensOfAttributeContext(
        INamedTypeSymbol ContainingTypeSymbol,
        INamedTypeSymbol TypeSymbol
    ) : AttributeContext;

    public sealed record OptionalOfAttributeContext(
        INamedTypeSymbol ContainingTypeSymbol,
        INamedTypeSymbol TypeSymbol
    ) : AttributeContext;

    public abstract record TypeContext;

    public sealed record LensOfTypeContext(
        INamedTypeSymbol Symbol
    ) : TypeContext;

    public sealed record OptionalOfTypeContext(
        INamedTypeSymbol Symbol
    ) : TypeContext;

    public sealed record TypeGenerationModel(
        string TypeName,
        ImmutableArray<MemberGenerationModel> Members
    );

    public readonly record struct MemberGenerationModel(
        string Name,
        string TypeName,
        bool IsNullable
    );
    #endregion

    #region Diagnostics
    private static readonly DiagnosticDescriptor OpticsTargetTypeCannotBeNullableRule = new(
        id: "MOPT0001",
        title: "Optics target type cannot be nullable",
        messageFormat: "Type '{0}' is nullable. Nullable types are not supported as optics targets.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    private static readonly DiagnosticDescriptor OpticsTargetTypeMustSupportWithExpressionRule = new(
        id: "MOPT0002",
        title: "Optics target type must support 'with' expression",
        messageFormat: "Type '{0}' must be a record or struct to be used as an optics target",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    private static readonly DiagnosticDescriptor OpticsAttributeMustBeOnStaticClassRule = new(
        id: "MOPT0003",
        title: "Optics attribute must be applied to a static class",
        messageFormat: "Class '{0}' must be static to use optics attribute",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    private static readonly DiagnosticDescriptor OpticsAttributeTargetMustSupportWithExpressionRule = new(
        id: "MOPT0004",
        title: "Optics attribute target type must support 'with' expression",
        messageFormat: "Type '{0}' must be a record or struct to be used with optics attribute",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    private static readonly DiagnosticDescriptor OpticsAttributeTargetMustBeSpecifiedRule = new(
        id: "MOPT0005",
        title: "Target type must be specified for optics attribute",
        messageFormat: "Class '{0}' is not nested in a target type. Specify the target type explicitly.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    #endregion

    #region Methods
    public static bool IsOfInvocationCandidate(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } invocationExpressionSyntax)
        {
            return false;
        }

        return invocationExpressionSyntax.Expression switch
        {
            GenericNameSyntax
            {
                Identifier.ValueText: "Of",
                TypeArgumentList.Arguments.Count: 1,
            } => true,
            MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax
                {
                    Identifier.ValueText: "Of",
                    TypeArgumentList.Arguments.Count: 1,
                },
            } => true,
            _ => false,
        };
    }

    public static AnalysisResult<TypeContext>? GetTypeContext(GeneratorSyntaxContext generatorSyntaxContext)
    {
        var expressionSyntax = (InvocationExpressionSyntax)generatorSyntaxContext.Node;
        var genericNameSyntax = GetGenericNameFromInvocation(expressionSyntax);

        if (generatorSyntaxContext.SemanticModel.GetSymbolInfo(expressionSyntax).Symbol is not IMethodSymbol
            {
                IsStatic: true,
                Name: "Of",
                Arity: 1,
                Parameters.Length: 0,
            } methodSymbol
        )
        {
            return null;
        }

        var typeArgumentList = genericNameSyntax.TypeArgumentList;

        if (typeArgumentList.Arguments is not [{ } typeArgument]
            || methodSymbol.TypeArguments is not [{ } typeArgumentSymbol]
            || typeArgumentSymbol is not INamedTypeSymbol typeSymbol
        )
        {
            return null;
        }

        const int lensOfType = 1;
        const int optionalOfType = 2;

        var type = methodSymbol.ContainingType is
        {
            Arity: 0,
            ContainingType: null,
            ContainingNamespace:
            {
                Name: "Optics",
                ContainingNamespace:
                {
                    Name: "Macaron",
                    ContainingNamespace.IsGlobalNamespace: true,
                },
            },
            Name: var containingTypeName,
        }
            ? containingTypeName switch
            {
                "Lens" => lensOfType,
                "Optional" => optionalOfType,
                _ => 0,
            }
            : 0;

        if (type == 0)
        {
            return null;
        }

        // Nullable한 형식은 지원하지 않는다.
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated ||
            typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        )
        {
            return new AnalysisResult<TypeContext>.Failure(
                Diagnostic.Create(
                    descriptor: OpticsTargetTypeCannotBeNullableRule,
                    location: typeArgument.GetLocation(),
                    messageArgs: [typeArgument]
                )
            );
        }

        // with 문을 지원하는 형식만 사용한다.
        if (typeSymbol is { IsRecord: false, TypeKind: not TypeKind.Struct })
        {
            return new AnalysisResult<TypeContext>.Failure(
                Diagnostic.Create(
                    descriptor: OpticsTargetTypeMustSupportWithExpressionRule,
                    location: typeArgument.GetLocation(),
                    messageArgs: [typeArgument]
                )
            );
        }

        var typeContext = type switch
        {
            lensOfType => (TypeContext)new LensOfTypeContext(Symbol: typeSymbol),
            optionalOfType => new OptionalOfTypeContext(Symbol: typeSymbol),
            _ => throw new InvalidOperationException($"Invalid type: {type}"),
        };

        return new AnalysisResult<TypeContext>.Success(typeContext);

        #region Local Functions
        static GenericNameSyntax GetGenericNameFromInvocation(
            InvocationExpressionSyntax invocationExpressionSyntax
        )
        {
            return invocationExpressionSyntax.Expression switch
            {
                MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } => genericName,
                GenericNameSyntax genericName => genericName,
                _ => throw new InvalidOperationException("Invocation is not a valid Of<T>() candidate"),
            };
        }
        #endregion
    }

    public static AnalysisResult<AttributeContext>? GetAttributeContext(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        if (context.TargetSymbol is not INamedTypeSymbol containingTypeSymbol)
        {
            return null;
        }

        var attribute = context.Attributes[0];
        var location = attribute.ApplicationSyntaxReference?
            .GetSyntax(cancellationToken)
            .GetLocation();

        if (!containingTypeSymbol.IsStatic)
        {
            return new AnalysisResult<AttributeContext>.Failure(
                Diagnostic.Create(
                    descriptor: OpticsAttributeMustBeOnStaticClassRule,
                    location,
                    messageArgs: [containingTypeSymbol]
                )
            );
        }

        if (attribute.ConstructorArguments is [{ Value: IErrorTypeSymbol }])
        {
            return null;
        }

        var typeSymbol = attribute.ConstructorArguments is [{ Value: INamedTypeSymbol symbolArgument }]
            ? symbolArgument
            : containingTypeSymbol.ContainingType;

        if (typeSymbol is null)
        {
            return new AnalysisResult<AttributeContext>.Failure(
                Diagnostic.Create(
                    descriptor: OpticsAttributeTargetMustBeSpecifiedRule,
                    location,
                    messageArgs: [containingTypeSymbol]
                )
            );
        }

        if (typeSymbol is { IsRecord: false, TypeKind: not TypeKind.Struct })
        {
            return new AnalysisResult<AttributeContext>.Failure(
                Diagnostic.Create(
                    descriptor: OpticsAttributeTargetMustSupportWithExpressionRule,
                    location,
                    messageArgs: [typeSymbol]
                )
            );
        }

        var attributeContext = attribute.AttributeClass?.ToDisplayString() switch
        {
            LensOfAttributeName => (AttributeContext)new LensOfAttributeContext(
                ContainingTypeSymbol: containingTypeSymbol,
                TypeSymbol: typeSymbol
            ),
            OptionalOfAttributeName => new OptionalOfAttributeContext(
                ContainingTypeSymbol: containingTypeSymbol,
                TypeSymbol: typeSymbol
            ),
            _ => throw new InvalidOperationException($"Invalid type: {attribute.AttributeClass}"),
        };

        return new AnalysisResult<AttributeContext>.Success(attributeContext);
    }

    public static ImmutableArray<(string, ImmutableArray<string>)> GenerateLensOfMembers(INamedTypeSymbol typeSymbol)
    {
        return GenerateLensOfMembers(CreateTypeGenerationModel(typeSymbol));
    }

    public static ImmutableArray<(string, ImmutableArray<string>)> GenerateLensOfMembers(
        TypeGenerationModel typeModel
    )
    {
        return GenerateMembers(
            typeModel,
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
        return GenerateOptionalOfMembers(CreateTypeGenerationModel(typeSymbol));
    }

    public static ImmutableArray<(string, ImmutableArray<string>)> GenerateOptionalOfMembers(
        TypeGenerationModel typeModel
    )
    {
        return GenerateMembers(
            typeModel,
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
        ImmutableArray<TypeGenerationModel> typeModels,
        Func<TypeGenerationModel, ImmutableArray<(string, ImmutableArray<string>)>> generateMembers
    )
    {
        if (typeModels.IsDefaultOrEmpty)
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

        for (int i = 0; i < typeModels.Length; ++i)
        {
            var typeModel = typeModels[i];
            var members = generateMembers(typeModel);
            var typeName = typeModel.TypeName;

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

            if (i < typeModels.Length - 1)
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
            var hash = offsetBasis;

            foreach (var b in bytes)
            {
                hash ^= b;
                hash *= fnvPrime;
            }

            return $"{typeSymbol.Name}_{typeSymbol.Arity}.{hash:x8}.g.cs";
        }
        #endregion
    }

    public static TypeGenerationModel CreateTypeGenerationModel(INamedTypeSymbol typeSymbol)
    {
        var members = GetValidMemberSymbols(typeSymbol);
        var builder = ImmutableArray.CreateBuilder<MemberGenerationModel>(members.Length);

        foreach (var member in members)
        {
            var isNullable = IsNullable(member);

            builder.Add(new MemberGenerationModel(
                Name: GetEscapedKeyword(member.Name),
                TypeName: GetMemberTypeName(member, isNullable),
                IsNullable: isNullable
            ));
        }

        return new TypeGenerationModel(
            TypeName: typeSymbol.ToDisplayString(FullyQualifiedFormat),
            Members: builder.MoveToImmutable()
        );

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

    private static ImmutableArray<(string, ImmutableArray<string>)> GenerateMembers(
        TypeGenerationModel typeModel,
        Func<string, string, string, bool, (string, ImmutableArray<string>)> getSourceByMember
    )
    {
        if (typeModel.Members.IsEmpty)
        {
            return ImmutableArray<(string, ImmutableArray<string>)>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<(string, ImmutableArray<string>)>(typeModel.Members.Length);

        foreach (var member in typeModel.Members)
        {
            builder.Add(getSourceByMember(
                typeModel.TypeName,
                member.TypeName,
                member.Name,
                member.IsNullable
            ));
        }

        return builder.MoveToImmutable();
    }

    private static ImmutableArray<ISymbol> GetValidMemberSymbols(INamedTypeSymbol typeSymbol)
    {
        var builder = ImmutableArray.CreateBuilder<ISymbol>();

        for (var current = typeSymbol; current != null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if ((member is IPropertySymbol propertySymbol && IsValidProperty(propertySymbol)) ||
                    (member is IFieldSymbol fieldSymbol && IsValidField(fieldSymbol))
                )
                {
                    builder.Add(member);
                }
            }
        }

        return builder.ToImmutable();
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

        return propertySymbol.SetMethod is { DeclaredAccessibility: Accessibility.Public };
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
