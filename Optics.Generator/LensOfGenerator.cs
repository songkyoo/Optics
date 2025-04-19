using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Macaron.Optics.Generator.Helper;

namespace Macaron.Optics.Generator;

[Generator]
public class LensOfGenerator : IIncrementalGenerator
{
    #region Constants
    private const string LensOfAttributeName = "global::Macaron.Optics.LensOfAttribute";
    #endregion

    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: static (context, _) => GetClassWithLensOfAttribute(context, LensOfAttributeName)
            )
            .Where(static classDeclaration => classDeclaration is not null);

        context.RegisterSourceOutput(
            source: classDeclarations,
            action: (sourceProductionContext, lensOfContext) => AddSource(
                sourceProductionContext: sourceProductionContext,
                lensOfContext: lensOfContext!,
                generateLensOfMembers: GenerateLensOfMembers
            )
        );
    }
    #endregion
}
