using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;
using static Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions;
using static Macaron.Optics.Generator.DiagnosticDescriptors;

namespace Macaron.Optics.Generator;

internal static class Helpers
{
    #region Constants
    public const string LensOfAttributeName = "Macaron.Optics.LensOfAttribute";
    public const string OptionalOfAttributeName = "Macaron.Optics.OptionalOfAttribute";

    private const string LensOfTypeString = "global::Macaron.Optics.Lens";
    private const string OptionalOfTypeString = "global::Macaron.Optics.Optional";
    private const string MaybeTypeString = "global::Macaron.Functional.Maybe";

    private const string Indent = "    ";
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
            return new AnalysisFailure<TypeContext>(
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
            return new AnalysisFailure<TypeContext>(
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

        return new AnalysisSuccess<TypeContext>(typeContext);

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
        OpticsKind kind,
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
            return new AnalysisFailure<AttributeContext>(
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
            return new AnalysisFailure<AttributeContext>(
                Diagnostic.Create(
                    descriptor: OpticsAttributeTargetMustBeSpecifiedRule,
                    location,
                    messageArgs: [containingTypeSymbol]
                )
            );
        }

        if (typeSymbol is { IsRecord: false, TypeKind: not TypeKind.Struct })
        {
            return new AnalysisFailure<AttributeContext>(
                Diagnostic.Create(
                    descriptor: OpticsAttributeTargetMustSupportWithExpressionRule,
                    location,
                    messageArgs: [typeSymbol]
                )
            );
        }

        var attributeContext = kind switch
        {
            OpticsKind.Lens => (AttributeContext)new LensOfAttributeContext(
                ContainingTypeSymbol: containingTypeSymbol,
                TypeSymbol: typeSymbol
            ),
            OpticsKind.Optional => new OptionalOfAttributeContext(
                ContainingTypeSymbol: containingTypeSymbol,
                TypeSymbol: typeSymbol
            ),
            _ => throw new InvalidOperationException($"Invalid optics kind: {kind}"),
        };

        return new AnalysisSuccess<AttributeContext>(attributeContext);
    }

    public static OfGenerationModel CreateOfGenerationModel(ImmutableArray<TypeContext> typeContexts)
    {
        var lensTypes = ImmutableArray.CreateBuilder<TypeGenerationModel>();
        var optionalTypes = ImmutableArray.CreateBuilder<TypeGenerationModel>();
        var visitedLensTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var visitedOptionalTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var typeGenerationModels = new Dictionary<INamedTypeSymbol, TypeGenerationModel>(
            SymbolEqualityComparer.Default
        );

        foreach (var typeContext in typeContexts)
        {
            switch (typeContext)
            {
                case LensOfTypeContext { Symbol: { } symbol }:
                {
                    AppendTypeGenerationModel(
                        symbol,
                        builder: lensTypes,
                        visitedLensTypes,
                        typeGenerationModels
                    );

                    break;
                }
                case OptionalOfTypeContext { Symbol: { } symbol }:
                {
                    AppendTypeGenerationModel(
                        symbol,
                        builder: optionalTypes,
                        visitedOptionalTypes,
                        typeGenerationModels
                    );

                    break;
                }
            }
        }

        lensTypes.Sort(static (x, y) => string.CompareOrdinal(x.FullyQualifiedName, y.FullyQualifiedName));
        optionalTypes.Sort(static (x, y) => string.CompareOrdinal(x.FullyQualifiedName, y.FullyQualifiedName));

        return new OfGenerationModel(
            LensTypes: lensTypes.ToImmutable(),
            OptionalTypes: optionalTypes.ToImmutable()
        );

        #region Local Functions
        static void AppendTypeGenerationModel(
            INamedTypeSymbol typeSymbol,
            ImmutableArray<TypeGenerationModel>.Builder builder,
            HashSet<INamedTypeSymbol> visitedTypes,
            Dictionary<INamedTypeSymbol, TypeGenerationModel> typeGenerationModels
        )
        {
            if (!visitedTypes.Add(typeSymbol))
            {
                return;
            }

            if (!typeGenerationModels.TryGetValue(typeSymbol, out var typeModel))
            {
                typeModel = CreateTypeGenerationModel(typeSymbol);
                typeGenerationModels.Add(typeSymbol, typeModel);
            }

            if (!typeModel.Members.IsEmpty)
            {
                builder.Add(typeModel);
            }
        }
        #endregion
    }

    public static AttributeGenerationModel CreateAttributeGenerationModel(AttributeContext attributeContext)
    {
        var (kind, containingTypeSymbol, typeSymbol) = attributeContext switch
        {
            LensOfAttributeContext context => (
                OpticsKind.Lens,
                context.ContainingTypeSymbol,
                context.TypeSymbol
            ),
            OptionalOfAttributeContext context => (
                OpticsKind.Optional,
                context.ContainingTypeSymbol,
                context.TypeSymbol
            ),
            _ => throw new InvalidOperationException($"Invalid attribute context: {attributeContext}"),
        };
        var containingTypes = new Stack<INamedTypeSymbol>();

        for (var current = containingTypeSymbol; current != null; current = current.ContainingType)
        {
            containingTypes.Push(current);
        }

        var typeDeclarations = ImmutableArray.CreateBuilder<string>(containingTypes.Count);

        foreach (var containingType in containingTypes)
        {
            typeDeclarations.Add(GetPartialTypeDeclarationString(containingType));
        }

        return new AttributeGenerationModel(
            Kind: kind,
            NamespaceName: containingTypeSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : containingTypeSymbol.ContainingNamespace.ToDisplayString(),
            TypeDeclarations: typeDeclarations.MoveToImmutable(),
            HintName: GetHintName(containingTypeSymbol),
            TargetType: CreateTypeGenerationModel(typeSymbol)
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

        #endregion
    }

    public static void AddSource(
        SourceProductionContext sourceProductionContext,
        string lensOfTypeName,
        TypeGenerationModel typeModel,
        OpticsKind kind
    )
    {
        if (typeModel.Members.IsDefaultOrEmpty)
        {
            return;
        }

        var stringBuilder = CreateStringBuilderWithFileHeader();
        var typeIndent = Indent;
        var memberIndent = $"{typeIndent}{Indent}";
        var bodyIndent = $"{memberIndent}{Indent}";

        // begin namespace
        stringBuilder.AppendLine($"namespace Macaron.Optics");
        stringBuilder.AppendLine($"{{");

        // begin extension methods
        stringBuilder.AppendLine($"{typeIndent}internal static partial class {lensOfTypeName}Extensions");
        stringBuilder.AppendLine($"{typeIndent}{{");

        var fullyQualifiedName = typeModel.FullyQualifiedName;

        for (var i = 0; i < typeModel.Members.Length; ++i)
        {
            var member = typeModel.Members[i];
            var opticType = GetOpticType(typeModel, member, kind);

            stringBuilder.AppendLine($"{memberIndent}public static {opticType} {member.Name}(");
            stringBuilder.AppendLine($"{bodyIndent}this {lensOfTypeName}<{fullyQualifiedName}> {char.ToLower(lensOfTypeName[0])}{lensOfTypeName[1..]}");
            stringBuilder.AppendLine($"{memberIndent})");
            stringBuilder.AppendLine($"{memberIndent}{{");

            stringBuilder.Append($"{bodyIndent}return ");
            AppendFactoryCall(stringBuilder, typeModel, member, kind, bodyIndent);

            stringBuilder.AppendLine($"{memberIndent}}}");

            if (i < typeModel.Members.Length - 1)
            {
                stringBuilder.AppendLine();
            }
        }

        // end extension methods
        stringBuilder.AppendLine($"{typeIndent}}}");

        // end namespace
        stringBuilder.AppendLine($"}}");

        var hintName = GetHintName(typeModel.Name, typeModel.Arity, fullyQualifiedName);

        sourceProductionContext.AddSource(
            hintName: $"{lensOfTypeName}Extensions.{hintName}",
            sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
        );
    }

    public static void AddSource(
        SourceProductionContext sourceProductionContext,
        AttributeGenerationModel generationModel
    )
    {
        var typeModel = generationModel.TargetType;

        if (typeModel.Members.IsDefaultOrEmpty)
        {
            return;
        }

        var stringBuilder = CreateStringBuilderWithFileHeader();

        // begin namespace
        var hasNamespace = generationModel.NamespaceName is not null;

        if (hasNamespace)
        {
            stringBuilder.AppendLine($"namespace {generationModel.NamespaceName}");
            stringBuilder.AppendLine($"{{");
        }

        var depthSpacerText = hasNamespace ? Indent : "";

        // begin containing types
        foreach (var typeDeclaration in generationModel.TypeDeclarations)
        {
            stringBuilder.AppendLine($"{depthSpacerText}{typeDeclaration}");
            stringBuilder.AppendLine($"{depthSpacerText}{{");

            depthSpacerText += Indent;
        }

        // write members
        for (var i = 0; i < typeModel.Members.Length; ++i)
        {
            var member = typeModel.Members[i];
            var opticType = GetOpticType(typeModel, member, generationModel.Kind);

            stringBuilder.Append($"{depthSpacerText}public static readonly {opticType} {member.Name} = ");
            AppendFactoryCall(
                stringBuilder,
                typeModel,
                member,
                generationModel.Kind,
                depthSpacerText
            );

            if (i < typeModel.Members.Length - 1)
            {
                stringBuilder.AppendLine();
            }
        }

        // end containing types
        for (var i = 0; i < generationModel.TypeDeclarations.Length; ++i)
        {
            depthSpacerText = depthSpacerText[..^Indent.Length];
            stringBuilder.AppendLine($"{depthSpacerText}}}");
        }

        // end namespace
        if (hasNamespace)
        {
            stringBuilder.AppendLine($"}}");
        }

        sourceProductionContext.AddSource(
            hintName: generationModel.HintName,
            sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
        );
    }

    private static string GetOpticType(
        TypeGenerationModel typeModel,
        MemberGenerationModel member,
        OpticsKind kind
    )
    {
        return (kind, member.IsNullable) switch
        {
            (OpticsKind.Lens, false) => $"{LensOfTypeString}<{typeModel.FullyQualifiedName}, {member.TypeName}>",
            (OpticsKind.Lens, true) => $"{LensOfTypeString}<{typeModel.FullyQualifiedName}, {MaybeTypeString}<{member.TypeName}>>",
            (OpticsKind.Optional, false) => $"{OptionalOfTypeString}<{MaybeTypeString}<{typeModel.FullyQualifiedName}>, {member.TypeName}>",
            (OpticsKind.Optional, true) => $"{LensOfTypeString}<{MaybeTypeString}<{typeModel.FullyQualifiedName}>, {MaybeTypeString}<{member.TypeName}>>",
            _ => throw new InvalidOperationException($"Invalid optics kind: {kind}"),
        };
    }

    private static void AppendFactoryCall(
        StringBuilder stringBuilder,
        TypeGenerationModel typeModel,
        MemberGenerationModel member,
        OpticsKind kind,
        string indentation
    )
    {
        stringBuilder.AppendLine($"{GetOpticType(typeModel, member, kind)}.Of(");

        switch (kind, member.IsNullable)
        {
            case (OpticsKind.Lens, false):
            {
                stringBuilder.AppendLine($"{indentation}{Indent}getter: static source => source.{member.Name},");
                stringBuilder.AppendLine($"{indentation}{Indent}setter: static (source, value) => source with");
                stringBuilder.AppendLine($"{indentation}{Indent}{{");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}{member.Name} = value,");
                stringBuilder.AppendLine($"{indentation}{Indent}}}");

                break;
            }
            case (OpticsKind.Lens, true):
            {
                stringBuilder.AppendLine($"{indentation}{Indent}getter: static source => source is {{ {member.Name}: {{ }} value }}");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}? {MaybeTypeString}.Just(value)");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}: {MaybeTypeString}.Nothing<{member.TypeName}>(),");
                stringBuilder.AppendLine($"{indentation}{Indent}setter: static (source, value) => source with");
                stringBuilder.AppendLine($"{indentation}{Indent}{{");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}{member.Name} = value is {{ IsJust: true, Value: var value2 }} ? value2 : null,");
                stringBuilder.AppendLine($"{indentation}{Indent}}}");

                break;
            }
            case (OpticsKind.Optional, false):
            {
                stringBuilder.AppendLine($"{indentation}{Indent}optionalGetter: static source => source.IsJust");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}? {MaybeTypeString}.Just(source.Value.{member.Name})");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}: {MaybeTypeString}.Nothing<{member.TypeName}>(),");
                AppendOptionalSetter(
                    stringBuilder,
                    typeModel,
                    member,
                    isNullable: false,
                    indentation: $"{indentation}{Indent}"
                );

                break;
            }
            case (OpticsKind.Optional, true):
            {
                stringBuilder.AppendLine($"{indentation}{Indent}getter: static source => source is {{ IsJust: true, Value: {{ }} value }}");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}? value.{member.Name} is {{ }} value2");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}{Indent}? {MaybeTypeString}.Just(value2)");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}{Indent}: {MaybeTypeString}.Nothing<{member.TypeName}>()");
                stringBuilder.AppendLine($"{indentation}{Indent}{Indent}: {MaybeTypeString}.Nothing<{member.TypeName}>(),");
                AppendOptionalSetter(
                    stringBuilder,
                    typeModel,
                    member,
                    isNullable: true,
                    indentation: $"{indentation}{Indent}"
                );

                break;
            }
            default:
                throw new InvalidOperationException($"Invalid optics kind: {kind}");
        }

        stringBuilder.AppendLine($"{indentation});");

        #region Local Functions
        static void AppendOptionalSetter(
            StringBuilder stringBuilder,
            TypeGenerationModel typeModel,
            MemberGenerationModel member,
            bool isNullable,
            string indentation
        )
        {
            var valueExpression = isNullable
                ? "value is { IsJust: true, Value: var value2 } ? value2 : null"
                : "value";

            stringBuilder.AppendLine($"{indentation}setter: static (source, value) => source.IsJust");
            stringBuilder.AppendLine($"{indentation}{Indent}? {MaybeTypeString}.Just(source.Value with");
            stringBuilder.AppendLine($"{indentation}{Indent}{{");
            stringBuilder.AppendLine($"{indentation}{Indent}{Indent}{member.Name} = {valueExpression},");
            stringBuilder.AppendLine($"{indentation}{Indent}}})");
            stringBuilder.AppendLine($"{indentation}{Indent}: {MaybeTypeString}.Nothing<{typeModel.FullyQualifiedName}>()");
        }
        #endregion
    }

    private static TypeGenerationModel CreateTypeGenerationModel(INamedTypeSymbol typeSymbol)
    {
        var builder = ImmutableArray.CreateBuilder<MemberGenerationModel>();

        for (var current = typeSymbol; current != null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (!IsValidMember(member))
                {
                    continue;
                }

                var isNullable = IsNullable(member);

                builder.Add(new MemberGenerationModel(
                    Name: GetEscapedKeyword(member.Name),
                    TypeName: GetMemberTypeName(member, isNullable),
                    IsNullable: isNullable
                ));
            }
        }

        return new TypeGenerationModel(
            Name: typeSymbol.Name,
            Arity: typeSymbol.Arity,
            FullyQualifiedName: typeSymbol.ToDisplayString(FullyQualifiedFormat),
            Members: builder.ToImmutable()
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

    private static bool IsValidMember(ISymbol member)
    {
        if (member.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        return (member is IPropertySymbol propertySymbol && IsValidProperty(propertySymbol))
            || (member is IFieldSymbol fieldSymbol && IsValidField(fieldSymbol));
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

    private static uint GetStableHash(string value)
    {
        const uint fnvPrime = 16777619;
        const uint offsetBasis = 2166136261;

        var hash = offsetBasis;

        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= fnvPrime;
        }

        return hash;
    }

    private static string GetHintName(INamedTypeSymbol typeSymbol)
    {
        return GetHintName(
            typeSymbol.Name,
            typeSymbol.Arity,
            typeSymbol.ToDisplayString(FullyQualifiedFormat)
        );
    }

    private static string GetHintName(string name, int arity, string fullyQualifiedName)
    {
        return $"{name}_{arity}.{GetStableHash(fullyQualifiedName):x8}.g.cs";
    }

    private static string GetEscapedKeyword(string keyword)
    {
        return GetKeywordKind(keyword) != SyntaxKind.None || GetContextualKeywordKind(keyword) != SyntaxKind.None
            ? "@" + keyword
            : keyword;
    }
    #endregion
}
