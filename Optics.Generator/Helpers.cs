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

        // begin namespace
        stringBuilder.AppendLine($"namespace Macaron.Optics");
        stringBuilder.AppendLine($"{{");

        // begin extension methods
        stringBuilder.AppendLine($"    internal static class {lensOfTypeName}Extensions");
        stringBuilder.AppendLine($"    {{");

        for (int i = 0; i < typeModels.Length; ++i)
        {
            var typeModel = typeModels[i];
            var typeName = typeModel.TypeName;

            for (int j = 0; j < typeModel.Members.Length; ++j)
            {
                var member = typeModel.Members[j];

                stringBuilder.Append("        public static ");
                AppendOpticType(stringBuilder, typeModel, member, kind);
                stringBuilder.Append(' ').Append(member.Name).AppendLine("(");
                stringBuilder.AppendLine($"            this {lensOfTypeName}<{typeName}> {char.ToLower(lensOfTypeName[0])}{lensOfTypeName[1..]}");
                stringBuilder.AppendLine($"        )");
                stringBuilder.AppendLine($"        {{");

                stringBuilder.Append("            return ");
                AppendFactoryCall(stringBuilder, typeModel, member, kind, "            ");

                stringBuilder.AppendLine($"        }}");

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

        var depthSpacerText = hasNamespace ? "    " : "";

        // begin containing types
        foreach (var typeDeclaration in generationModel.TypeDeclarations)
        {
            stringBuilder.AppendLine($"{depthSpacerText}{typeDeclaration}");
            stringBuilder.AppendLine($"{depthSpacerText}{{");

            depthSpacerText += "    ";
        }

        // write members
        for (var i = 0; i < typeModel.Members.Length; ++i)
        {
            var member = typeModel.Members[i];

            stringBuilder.Append(depthSpacerText).Append("public static readonly ");
            AppendOpticType(stringBuilder, typeModel, member, generationModel.Kind);
            stringBuilder.Append(' ').Append(member.Name).Append(" = ");
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
            depthSpacerText = depthSpacerText[..^4];
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

    private static void AppendOpticType(
        StringBuilder stringBuilder,
        TypeGenerationModel typeModel,
        MemberGenerationModel member,
        OpticsKind kind
    )
    {
        switch (kind)
        {
            case OpticsKind.Lens:
            {
                stringBuilder.Append(LensOfTypeString).Append('<').Append(typeModel.TypeName).Append(", ");

                if (member.IsNullable)
                {
                    stringBuilder.Append(MaybeTypeString).Append('<').Append(member.TypeName).Append('>');
                }
                else
                {
                    stringBuilder.Append(member.TypeName);
                }

                stringBuilder.Append('>');

                break;
            }
            case OpticsKind.Optional:
            {
                if (member.IsNullable)
                {
                    stringBuilder
                        .Append(LensOfTypeString)
                        .Append('<')
                        .Append(MaybeTypeString)
                        .Append('<')
                        .Append(typeModel.TypeName)
                        .Append(">, ")
                        .Append(MaybeTypeString)
                        .Append('<')
                        .Append(member.TypeName)
                        .Append(">>");
                }
                else
                {
                    stringBuilder
                        .Append(OptionalOfTypeString)
                        .Append('<')
                        .Append(MaybeTypeString)
                        .Append('<')
                        .Append(typeModel.TypeName)
                        .Append(">, ")
                        .Append(member.TypeName)
                        .Append('>');
                }

                break;
            }
            default:
                throw new InvalidOperationException($"Invalid optics kind: {kind}");
        }
    }

    private static void AppendFactoryCall(
        StringBuilder stringBuilder,
        TypeGenerationModel typeModel,
        MemberGenerationModel member,
        OpticsKind kind,
        string indentation
    )
    {
        AppendOpticType(stringBuilder, typeModel, member, kind);
        stringBuilder.AppendLine(".Of(");

        switch (kind, member.IsNullable)
        {
            case (OpticsKind.Lens, false):
            {
                stringBuilder.Append(indentation).Append("    getter: static source => source.").Append(member.Name).AppendLine(",");
                stringBuilder.Append(indentation).AppendLine("    setter: static (source, value) => source with");
                stringBuilder.Append(indentation).AppendLine("    {");
                stringBuilder.Append(indentation).Append("        ").Append(member.Name).AppendLine(" = value,");
                stringBuilder.Append(indentation).AppendLine("    }");

                break;
            }
            case (OpticsKind.Lens, true):
            {
                stringBuilder.Append(indentation).Append("    getter: static source => source is { ").Append(member.Name).AppendLine(": { } value }");
                stringBuilder.Append(indentation).Append("        ? ").Append(MaybeTypeString).AppendLine(".Just(value)");
                stringBuilder.Append(indentation).Append("        : ").Append(MaybeTypeString).Append(".Nothing<").Append(member.TypeName).AppendLine(">(),");
                stringBuilder.Append(indentation).AppendLine("    setter: static (source, value) => source with");
                stringBuilder.Append(indentation).AppendLine("    {");
                stringBuilder.Append(indentation).Append("        ").Append(member.Name).AppendLine(" = value is { IsJust: true, Value: var value2 } ? value2 : null,");
                stringBuilder.Append(indentation).AppendLine("    }");

                break;
            }
            case (OpticsKind.Optional, false):
            {
                stringBuilder.Append(indentation).AppendLine("    optionalGetter: static source => source.IsJust");
                stringBuilder.Append(indentation).Append("        ? ").Append(MaybeTypeString).Append(".Just(source.Value.").Append(member.Name).AppendLine(")");
                stringBuilder.Append(indentation).Append("        : ").Append(MaybeTypeString).Append(".Nothing<").Append(member.TypeName).AppendLine(">(),");
                AppendOptionalSetter(stringBuilder, typeModel, member, indentation, isNullable: false);

                break;
            }
            case (OpticsKind.Optional, true):
            {
                stringBuilder.Append(indentation).AppendLine("    getter: static source => source is { IsJust: true, Value: { } value }");
                stringBuilder.Append(indentation).Append("        ? value.").Append(member.Name).AppendLine(" is { } value2");
                stringBuilder.Append(indentation).Append("            ? ").Append(MaybeTypeString).AppendLine(".Just(value2)");
                stringBuilder.Append(indentation).Append("            : ").Append(MaybeTypeString).Append(".Nothing<").Append(member.TypeName).AppendLine(">()");
                stringBuilder.Append(indentation).Append("        : ").Append(MaybeTypeString).Append(".Nothing<").Append(member.TypeName).AppendLine(">(),");
                AppendOptionalSetter(stringBuilder, typeModel, member, indentation, isNullable: true);

                break;
            }
            default:
                throw new InvalidOperationException($"Invalid optics kind: {kind}");
        }

        stringBuilder.Append(indentation).AppendLine(");");

        #region Local Functions
        static void AppendOptionalSetter(
            StringBuilder stringBuilder,
            TypeGenerationModel typeModel,
            MemberGenerationModel member,
            string indentation,
            bool isNullable
        )
        {
            stringBuilder.Append(indentation).AppendLine("    setter: static (source, value) => source.IsJust");
            stringBuilder.Append(indentation).Append("        ? ").Append(MaybeTypeString).AppendLine(".Just(source.Value with");
            stringBuilder.Append(indentation).AppendLine("        {");
            stringBuilder.Append(indentation).Append("            ").Append(member.Name).Append(" = value");

            if (isNullable)
            {
                stringBuilder.Append(" is { IsJust: true, Value: var value2 } ? value2 : null");
            }

            stringBuilder.AppendLine(",");
            stringBuilder.Append(indentation).AppendLine("        })");
            stringBuilder.Append(indentation).Append("        : ").Append(MaybeTypeString).Append(".Nothing<").Append(typeModel.TypeName).AppendLine(">()");
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
