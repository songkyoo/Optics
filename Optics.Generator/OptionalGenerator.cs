using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Macaron.Optics.Generator.Helper;

namespace Macaron.Optics.Generator;

[Generator]
public class OptionalGenerator : IIncrementalGenerator
{
    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionalOfCalls = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is InvocationExpressionSyntax,
                transform: static (generatorSyntaxContext, _) => GetLensOfType(generatorSyntaxContext, OptionalTypeName)
            )
            .Where(static typeName => typeName is not null)
            .Select(static (typeName, _) => typeName!)
            .Collect();

        context.RegisterSourceOutput(
            source: optionalOfCalls,
            action: (sourceProductionContext, lensTypeSymbols) => AddSource(
                sourceProductionContext: sourceProductionContext,
                lensOfTypeName: "OptionalOf",
                lensTypeSymbols: lensTypeSymbols,
                generateLensOfMembers: GenerateOptionalOfMembers
            )
        );
    }
    #endregion
}
