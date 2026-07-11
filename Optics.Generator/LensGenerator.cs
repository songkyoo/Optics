using Microsoft.CodeAnalysis;

using static Macaron.Optics.Generator.Helpers;
using static Macaron.Optics.Generator.OpticsKind;

namespace Macaron.Optics.Generator;

[Generator(LanguageNames.CSharp)]
public class LensGenerator : IIncrementalGenerator
{
    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var analysisResultProvider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => IsOfInvocationCandidate(syntaxNode),
                transform: static (generatorSyntaxContext, _) => GetTypeContext(generatorSyntaxContext)
            )
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);
        var typeContextProvider = analysisResultProvider
            .Where(static result => result is AnalysisResult<TypeContext>.Success)
            .Select(static (result, _) => ((AnalysisResult<TypeContext>.Success)result).Context);
        var generationModelProvider = typeContextProvider
            .Collect()
            .Select(static (typeContexts, _) => CreateOfGenerationModel(typeContexts))
            .WithComparer(OfGenerationModelComparer.Instance);
        var diagnosticProvider = analysisResultProvider
            .Where(static result => result is AnalysisResult<TypeContext>.Failure)
            .Select(static (result, _) => ((AnalysisResult<TypeContext>.Failure)result).Diagnostic);

        context.RegisterSourceOutput(
            source: diagnosticProvider,
            action: static (sourceProductionContext, diagnostic) =>
                sourceProductionContext.ReportDiagnostic(diagnostic)
        );
        context.RegisterSourceOutput(
            source: generationModelProvider,
            action: static (sourceProductionContext, generationModel) =>
            {
                AddSource(
                    sourceProductionContext: sourceProductionContext,
                    lensOfTypeName: "LensOf",
                    typeModels: generationModel.LensTypes,
                    kind: Lens
                );
                AddSource(
                    sourceProductionContext: sourceProductionContext,
                    lensOfTypeName: "OptionalOf",
                    typeModels: generationModel.OptionalTypes,
                    kind: Optional
                );
            }
        );
    }
    #endregion
}
