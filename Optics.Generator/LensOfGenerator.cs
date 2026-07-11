using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Macaron.Optics.Generator.Helpers;

namespace Macaron.Optics.Generator;

[Generator(LanguageNames.CSharp)]
public class LensOfGenerator : IIncrementalGenerator
{
    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var lensOfAttributeContexts = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: LensOfAttributeName,
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: static (generatorAttributeSyntaxContext, cancellationToken) => GetAttributeContext(
                    generatorAttributeSyntaxContext,
                    cancellationToken
                )
            )
            .Where(static attributeContext => !ReferenceEquals(attributeContext, AttributeContext.Empty));
        var optionalOfAttributeContexts = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: OptionalOfAttributeName,
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: static (generatorAttributeSyntaxContext, cancellationToken) => GetAttributeContext(
                    generatorAttributeSyntaxContext,
                    cancellationToken
                )
            )
            .Where(static attributeContext => !ReferenceEquals(attributeContext, AttributeContext.Empty));

        context.RegisterSourceOutput(
            source: lensOfAttributeContexts,
            action: static (sourceProductionContext, attributeContext) => GenerateAttributeSource(
                sourceProductionContext,
                attributeContext
            )
        );
        context.RegisterSourceOutput(
            source: optionalOfAttributeContexts,
            action: static (sourceProductionContext, attributeContext) => GenerateAttributeSource(
                sourceProductionContext,
                attributeContext
            )
        );

        #region Local Functions
        static void GenerateAttributeSource(
            SourceProductionContext sourceProductionContext,
            AttributeContext attributeContext
        )
        {
            foreach (var diagnostic in attributeContext.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            switch (attributeContext)
            {
                case LensOfAttributeContext
                {
                    ContainingTypeSymbol: { } containingTypeSymbol,
                    TypeSymbol: { } typeSymbol
                }:
                {
                    AddSource(
                        sourceProductionContext: sourceProductionContext,
                        attributeContext: (containingTypeSymbol, typeSymbol),
                        generateMembers: GenerateLensOfMembers
                    );

                    break;
                }
                case OptionalOfAttributeContext
                {
                    ContainingTypeSymbol: { } containingTypeSymbol,
                    TypeSymbol: { } typeSymbol
                }:
                {
                    AddSource(
                        sourceProductionContext: sourceProductionContext,
                        attributeContext: (containingTypeSymbol, typeSymbol),
                        generateMembers: GenerateOptionalOfMembers
                    );

                    break;
                }
            }
        }
        #endregion
    }
    #endregion
}
