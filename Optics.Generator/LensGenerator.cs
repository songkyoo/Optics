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
                transform: static (generatorSyntaxContext, cancellationToken) => GetTypeContext(
                    generatorSyntaxContext,
                    cancellationToken
                )
            )
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);
        var typeContextProvider = analysisResultProvider
            .Where(static result => result is AnalysisSuccess<TypeContext>)
            .Select(static (result, _) => ((AnalysisSuccess<TypeContext>)result).Context);
        var generationModelProvider = typeContextProvider
            .Collect()
            .Select(static (typeContexts, cancellationToken) => CreateOfGenerationModel(
                typeContexts,
                cancellationToken
            ))
            .WithComparer(OfGenerationModelComparer.Instance);
        var lensTypeProvider = generationModelProvider
            .SelectMany(static (generationModel, _) => generationModel.LensTypes)
            .WithComparer(TypeGenerationModelComparer.Instance)
            .WithTrackingName("LensTypeGenerationModels");
        var optionalTypeProvider = generationModelProvider
            .SelectMany(static (generationModel, _) => generationModel.OptionalTypes)
            .WithComparer(TypeGenerationModelComparer.Instance)
            .WithTrackingName("OptionalTypeGenerationModels");
        var diagnosticProvider = analysisResultProvider
            .Where(static result => result is AnalysisFailure<TypeContext>)
            .Select(static (result, _) => ((AnalysisFailure<TypeContext>)result).Diagnostic);

        context.RegisterSourceOutput(
            source: diagnosticProvider,
            action: static (sourceProductionContext, diagnostic) =>
                sourceProductionContext.ReportDiagnostic(diagnostic)
        );
        context.RegisterSourceOutput(
            source: lensTypeProvider,
            action: static (sourceProductionContext, typeModel) => AddSource(
                sourceProductionContext,
                lensOfTypeName: "LensOf",
                typeModel,
                kind: Lens
            )
        );
        context.RegisterSourceOutput(
            source: optionalTypeProvider,
            action: static (sourceProductionContext, typeModel) => AddSource(
                sourceProductionContext,
                lensOfTypeName: "OptionalOf",
                typeModel,
                kind: Optional
            )
        );
    }
    #endregion
}
