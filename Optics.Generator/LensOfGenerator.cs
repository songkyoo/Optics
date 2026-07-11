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
        var lensOfAnalysisResults = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: LensOfAttributeName,
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: static (generatorAttributeSyntaxContext, cancellationToken) => GetAttributeContext(
                    generatorAttributeSyntaxContext,
                    cancellationToken
                )
            )
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);
        var optionalOfAnalysisResults = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: OptionalOfAttributeName,
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: static (generatorAttributeSyntaxContext, cancellationToken) => GetAttributeContext(
                    generatorAttributeSyntaxContext,
                    cancellationToken
                )
            )
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);

        RegisterAnalysisResultOutputs(context, lensOfAnalysisResults);
        RegisterAnalysisResultOutputs(context, optionalOfAnalysisResults);

        #region Local Functions
        static void RegisterAnalysisResultOutputs(
            IncrementalGeneratorInitializationContext context,
            IncrementalValuesProvider<AnalysisResult<AttributeContext>> analysisResultProvider
        )
        {
            var attributeContextProvider = analysisResultProvider
                .Where(static result => result is AnalysisResult<AttributeContext>.Success)
                .Select(static (result, _) => ((AnalysisResult<AttributeContext>.Success)result).Context);
            var diagnosticProvider = analysisResultProvider
                .Where(static result => result is AnalysisResult<AttributeContext>.Failure)
                .Select(static (result, _) => ((AnalysisResult<AttributeContext>.Failure)result).Diagnostic);

            context.RegisterSourceOutput(
                source: diagnosticProvider,
                action: static (sourceProductionContext, diagnostic) =>
                    sourceProductionContext.ReportDiagnostic(diagnostic)
            );
            context.RegisterSourceOutput(
                source: attributeContextProvider,
                action: static (sourceProductionContext, attributeContext) => GenerateAttributeSource(
                    sourceProductionContext,
                    attributeContext
                )
            );
        }

        static void GenerateAttributeSource(
            SourceProductionContext sourceProductionContext,
            AttributeContext attributeContext
        )
        {
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
