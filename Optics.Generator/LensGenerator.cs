using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Macaron.Optics.Generator.Helper;

namespace Macaron.Optics.Generator;

[Generator]
public class LensGenerator : IIncrementalGenerator
{
    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var lensOfCalls = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is InvocationExpressionSyntax,
                transform: static (generatorSyntaxContext, _) => GetLensOfType(generatorSyntaxContext, LensTypeName)
            )
            .Where(static typeName => typeName is not null)
            .Select(static (typeName, _) => typeName!)
            .Collect();

        context.RegisterSourceOutput(
            source: lensOfCalls,
            action: (sourceProductionContext, lensTypeSymbols) => AddSource(
                sourceProductionContext: sourceProductionContext,
                lensOfTypeName: "LensOf",
                lensTypeSymbols: lensTypeSymbols,
                generateLensOfMembers: GenerateLensOfMembers
            )
        );
    }
    #endregion
}
