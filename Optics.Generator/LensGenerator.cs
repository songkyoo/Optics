using Microsoft.CodeAnalysis;

using static Macaron.Optics.Generator.SourceGenerationHelper;
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
                transform: static (generatorSyntaxContext, cancellationToken) => GetTypeAnalysisContext(
                    generatorSyntaxContext,
                    cancellationToken
                )
            )
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);

        // 진단
        var diagnosticProvider = analysisResultProvider
            .Where(static result => result is AnalysisResult<TypeContext>.Failure)
            .Select(static (result, _) => ((AnalysisResult<TypeContext>.Failure)result).Diagnostic);

        context.RegisterSourceOutput(
            source: diagnosticProvider,
            action: static (sourceProductionContext, diagnostic) => sourceProductionContext.ReportDiagnostic(diagnostic)
        );

        // 코드 생성
        var typeContextProvider = analysisResultProvider
            .Where(static result => result is AnalysisResult<TypeContext>.Success)
            .Select(static (result, _) => ((AnalysisResult<TypeContext>.Success)result).Context)
            .Collect()
            .Select(static (typeAnalysisContexts, cancellationToken) => CreateOfGenerationModel(
                typeAnalysisContexts,
                cancellationToken
            ))
            .WithComparer(OfGenerationModelComparer.Instance);

        // Lens 타입
        var lensTypeProvider = typeContextProvider
            .SelectMany(static (generationModel, _) => generationModel.LensTypes)
            .WithComparer(TypeGenerationModelComparer.Instance)
            .WithTrackingName("LensTypeGenerationModel");

        context.RegisterSourceOutput(
            source: lensTypeProvider,
            action: static (sourceProductionContext, typeModel) => AddSource(
                sourceProductionContext,
                lensOfTypeName: "LensOf",
                typeModel,
                kind: Lens
            )
        );

        // Optional 타입
        var optionalTypeProvider = typeContextProvider
            .SelectMany(static (generationModel, _) => generationModel.OptionalTypes)
            .WithComparer(TypeGenerationModelComparer.Instance)
            .WithTrackingName("OptionalTypeGenerationModel");

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
