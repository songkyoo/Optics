using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Macaron.Optics.Generator.Helpers;

namespace Macaron.Optics.Generator;

[Generator]
public class LensGenerator : IIncrementalGenerator
{
    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var valueProvider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is InvocationExpressionSyntax,
                transform: static (generatorSyntaxContext, _) => GetTypeContext(generatorSyntaxContext)
            )
            .Collect();

        context.RegisterSourceOutput(
            source: valueProvider,
            action: (sourceProductionContext, typeContexts) =>
            {
                foreach (var diagnostic in typeContexts.SelectMany(static context => context.Diagnostics))
                {
                    sourceProductionContext.ReportDiagnostic(diagnostic);
                }

                var lensOfTypeSymbolsBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
                var optionalOfTypeSymbolsBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

                foreach (var typeContext in typeContexts)
                {
                    switch (typeContext)
                    {
                        case LensOfTypeContext { Symbol: { } symbol }:
                            lensOfTypeSymbolsBuilder.Add(symbol);
                            break;
                        case OptionalOfTypeContext { Symbol: { } symbol }:
                            optionalOfTypeSymbolsBuilder.Add(symbol);
                            break;
                    }
                }

                AddSource(
                    sourceProductionContext: sourceProductionContext,
                    lensOfTypeName: "LensOf",
                    typeSymbols: lensOfTypeSymbolsBuilder.ToImmutable(),
                    generateMembers: GenerateLensOfMembers
                );
                AddSource(
                    sourceProductionContext: sourceProductionContext,
                    lensOfTypeName: "OptionalOf",
                    typeSymbols: optionalOfTypeSymbolsBuilder.ToImmutable(),
                    generateMembers: GenerateOptionalOfMembers
                );
            });
    }
    #endregion
}
