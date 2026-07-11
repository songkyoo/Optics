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
                .Where(static result => result is AnalysisSuccess<AttributeContext>)
                .Select(static (result, _) => ((AnalysisSuccess<AttributeContext>)result).Context);
            var generationModelProvider = attributeContextProvider
                .Select(static (attributeContext, _) => CreateAttributeGenerationModel(attributeContext))
                .WithComparer(AttributeGenerationModelComparer.Instance);
            var diagnosticProvider = analysisResultProvider
                .Where(static result => result is AnalysisFailure<AttributeContext>)
                .Select(static (result, _) => ((AnalysisFailure<AttributeContext>)result).Diagnostic);

            context.RegisterSourceOutput(
                source: diagnosticProvider,
                action: static (sourceProductionContext, diagnostic) => sourceProductionContext.ReportDiagnostic(
                    diagnostic
                )
            );
            context.RegisterSourceOutput(
                source: generationModelProvider,
                action: static (sourceProductionContext, generationModel) => AddSource(
                    sourceProductionContext,
                    generationModel
                )
            );
        }
        #endregion
    }
    #endregion
}
