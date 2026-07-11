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

        return new AnalysisSuccess<AttributeContext>(attributeContext);
    }

    public static OfGenerationModel CreateOfGenerationModel(ImmutableArray<TypeContext> typeContexts)
    {
        var lensTypeSymbols = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        var optionalTypeSymbols = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        var visitedLensTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var visitedOptionalTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var typeContext in typeContexts)
        {
            switch (typeContext)
            {
                case LensOfTypeContext { Symbol: { } symbol } when visitedLensTypes.Add(symbol):
                    lensTypeSymbols.Add(symbol);
                    break;
                case OptionalOfTypeContext { Symbol: { } symbol } when visitedOptionalTypes.Add(symbol):
                    optionalTypeSymbols.Add(symbol);
                    break;
            }
        }

        var typeModels = new Dictionary<INamedTypeSymbol, TypeGenerationModel>(
            SymbolEqualityComparer.Default
        );

        return new OfGenerationModel(
            LensTypes: GetTypeModels(lensTypeSymbols, typeModels),
            OptionalTypes: GetTypeModels(optionalTypeSymbols, typeModels)
        );

        #region Local Functions
        static ImmutableArray<TypeGenerationModel> GetTypeModels(
            ImmutableArray<INamedTypeSymbol>.Builder typeSymbols,
            Dictionary<INamedTypeSymbol, TypeGenerationModel> typeModels
        )
        {
            var builder = ImmutableArray.CreateBuilder<TypeGenerationModel>(typeSymbols.Count);

            foreach (var typeSymbol in typeSymbols)
            {
                if (!typeModels.TryGetValue(typeSymbol, out var typeModel))
                {
                    typeModel = CreateTypeGenerationModel(typeSymbol);
                    typeModels.Add(typeSymbol, typeModel);
                }

                if (!typeModel.Members.IsEmpty)
                {
                    builder.Add(typeModel);
                }
            }

            builder.Sort(static (x, y) => string.CompareOrdinal(x.TypeName, y.TypeName));

            return builder.ToImmutable();
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

    public static void AddSource(
        SourceProductionContext sourceProductionContext,
        string lensOfTypeName,
        ImmutableArray<TypeGenerationModel> typeModels,
        OpticsKind kind
    )
    {
        if (typeModels.IsDefaultOrEmpty)
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
        stringBuilder.AppendLine($"{typeIndent}internal static class {lensOfTypeName}Extensions");
        stringBuilder.AppendLine($"{typeIndent}{{");

        for (int i = 0; i < typeModels.Length; ++i)
        {
            var typeModel = typeModels[i];
            var typeName = typeModel.TypeName;

            for (int j = 0; j < typeModel.Members.Length; ++j)
            {
                var member = typeModel.Members[j];
                var opticType = GetOpticType(typeModel, member, kind);

                stringBuilder.AppendLine($"{memberIndent}public static {opticType} {member.Name}(");
                stringBuilder.AppendLine($"{bodyIndent}this {lensOfTypeName}<{typeName}> {char.ToLower(lensOfTypeName[0])}{lensOfTypeName[1..]}");
                stringBuilder.AppendLine($"{memberIndent})");
                stringBuilder.AppendLine($"{memberIndent}{{");

                stringBuilder.Append($"{bodyIndent}return ");
                AppendFactoryCall(stringBuilder, typeModel, member, kind, bodyIndent);

                stringBuilder.AppendLine($"{memberIndent}}}");

                if (j < typeModel.Members.Length - 1)
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
        stringBuilder.AppendLine($"{typeIndent}}}");

        // end namespace
        stringBuilder.AppendLine($"}}");

        sourceProductionContext.AddSource(
            hintName: $"{lensOfTypeName}Extensions.g.cs",
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
            (OpticsKind.Lens, false) => $"{LensOfTypeString}<{typeModel.TypeName}, {member.TypeName}>",
            (OpticsKind.Lens, true) => $"{LensOfTypeString}<{typeModel.TypeName}, {MaybeTypeString}<{member.TypeName}>>",
            (OpticsKind.Optional, false) => $"{OptionalOfTypeString}<{MaybeTypeString}<{typeModel.TypeName}>, {member.TypeName}>",
            (OpticsKind.Optional, true) => $"{LensOfTypeString}<{MaybeTypeString}<{typeModel.TypeName}>, {MaybeTypeString}<{member.TypeName}>>",
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
            stringBuilder.AppendLine($"{indentation}{Indent}: {MaybeTypeString}.Nothing<{typeModel.TypeName}>()");
        }
        #endregion
    }

    private static TypeGenerationModel CreateTypeGenerationModel(INamedTypeSymbol typeSymbol)
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
