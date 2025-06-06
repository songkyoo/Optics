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
                transform: static (generatorSyntaxContext, _) => GetLensOfTypeContext(
                    generatorSyntaxContext,
                    LensTypeName
                )
            )
            .Collect();

        context.RegisterSourceOutput(
            source: valueProvider,
            action: (sourceProductionContext, lensOfTypeContextx) => AddSource(
                sourceProductionContext: sourceProductionContext,
                lensOfTypeName: "LensOf",
                lensOfTypeContexts: lensOfTypeContextx,
                generateLensOfMembers: GenerateLensOfMembers
            )
        );
    }
    #endregion
}
