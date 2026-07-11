using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

using static Macaron.Optics.Generator.Helpers;

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
            .Select(static (result, _) => ((AnalysisResult<TypeContext>.Success)result).Context)
            .Collect();
        var diagnosticProvider = analysisResultProvider
            .Where(static result => result is AnalysisResult<TypeContext>.Failure)
            .Select(static (result, _) => ((AnalysisResult<TypeContext>.Failure)result).Diagnostic);

        context.RegisterSourceOutput(
            source: diagnosticProvider,
            action: static (sourceProductionContext, diagnostic) =>
                sourceProductionContext.ReportDiagnostic(diagnostic)
        );

        context.RegisterSourceOutput(
            source: typeContextProvider,
            action: (sourceProductionContext, typeContexts) =>
            {
                var lensOfTypeSymbolsBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
                var optionalOfTypeSymbolsBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
                var lensOfTypeSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var optionalOfTypeSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

                foreach (var typeContext in typeContexts)
                {
                    switch (typeContext)
                    {
                        case LensOfTypeContext { Symbol: { } symbol } when lensOfTypeSymbols.Add(symbol):
                        {
                            lensOfTypeSymbolsBuilder.Add(symbol);

                            break;
                        }
                        case OptionalOfTypeContext { Symbol: { } symbol } when optionalOfTypeSymbols.Add(symbol):
                        {
                            optionalOfTypeSymbolsBuilder.Add(symbol);

                            break;
                        }
                    }
                }

                var typeModels = new Dictionary<INamedTypeSymbol, TypeGenerationModel>(SymbolEqualityComparer.Default);
                var lensOfTypeModels = GetTypeModels(lensOfTypeSymbolsBuilder, typeModels);
                var optionalOfTypeModels = GetTypeModels(optionalOfTypeSymbolsBuilder, typeModels);

                AddSource(
                    sourceProductionContext: sourceProductionContext,
                    lensOfTypeName: "LensOf",
                    typeModels: lensOfTypeModels,
                    generateMembers: GenerateLensOfMembers
                );
                AddSource(
                    sourceProductionContext: sourceProductionContext,
                    lensOfTypeName: "OptionalOf",
                    typeModels: optionalOfTypeModels,
                    generateMembers: GenerateOptionalOfMembers
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

                    return builder.ToImmutable();
                }
                #endregion
            });
    }
    #endregion
}
