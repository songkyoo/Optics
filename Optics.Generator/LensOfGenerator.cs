using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Macaron.Optics.Generator.SourceGenerationHelper;
using static Macaron.Optics.Generator.OpticsKind;

namespace Macaron.Optics.Generator;

[Generator(LanguageNames.CSharp)]
public class LensOfGenerator : IIncrementalGenerator
{
    #region Constants
    private const string LensOfAttributeMetadataName = "Macaron.Optics.LensOfAttribute";
    private const string OptionalOfAttributeMetadataName = "Macaron.Optics.OptionalOfAttribute";
    #endregion

    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var lensOfAnalysisResults = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: LensOfAttributeMetadataName,
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: static (generatorAttributeSyntaxContext, cancellationToken) => GetAttributeContext(
                    generatorAttributeSyntaxContext,
                    kind: Lens,
                    cancellationToken
                )
            )
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);
        var optionalOfAnalysisResults = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: OptionalOfAttributeMetadataName,
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: static (generatorAttributeSyntaxContext, cancellationToken) => GetAttributeContext(
                    generatorAttributeSyntaxContext,
                    kind: Optional,
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
            var generationModelProvider = attributeContextProvider
                .Select(static (attributeContext, cancellationToken) => CreateAttributeGenerationModel(
                    attributeContext,
                    cancellationToken
                ))
                .WithComparer(AttributeGenerationModelComparer.Instance);
            var diagnosticProvider = analysisResultProvider
                .Where(static result => result is AnalysisResult<AttributeContext>.Failure)
                .Select(static (result, _) => ((AnalysisResult<AttributeContext>.Failure)result).Diagnostic);

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
