using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Macaron.Optics.Generator.Helpers;

namespace Macaron.Optics.Generator;

[Generator]
public class LensOfGenerator : IIncrementalGenerator
{
    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var valuesProvider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: static (generatorSyntaxContext, _) => GetAttributeContext(generatorSyntaxContext)
            );

        context.RegisterSourceOutput(
            source: valuesProvider.Collect(),
            action: (sourceProductionContext, attributeContexts) =>
            {
                foreach (var diagnostic in attributeContexts.SelectMany(static context => context.Diagnostics))
                {
                    sourceProductionContext.ReportDiagnostic(diagnostic);
                }

                foreach (var attributeContext in GetUniqueAttributeContexts(attributeContexts))
                {
                    switch (attributeContext)
                    {
                        case LensOfAttributeContext
                        {
                            ContainingTypeSymbol: { } containingTypeSymbol,
                            TypeSymbol: { } typeSymbol
                        }:
                        {
                            AddSource(
                                sourceProductionContext: sourceProductionContext,
                                attributeContext: (containingTypeSymbol, typeSymbol),
                                generateMembers: GenerateLensOfMembers
                            );
                            break;
                        }
                        case OptionalOfAttributeContext
                        {
                            ContainingTypeSymbol: { } containingTypeSymbol,
                            TypeSymbol: { } typeSymbol
                        }:
                        {
                            AddSource(
                                sourceProductionContext: sourceProductionContext,
                                attributeContext: (containingTypeSymbol, typeSymbol),
                                generateMembers: GenerateOptionalOfMembers
                            );
                            break;
                        }
                    }
                }
            });

        static IEnumerable<AttributeContext> GetUniqueAttributeContexts(
            IEnumerable<AttributeContext> attributeContexts
        )
        {
            var visitedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var attributeContext in attributeContexts)
            {
                var containingTypeSymbol = attributeContext switch
                {
                    LensOfAttributeContext context => context.ContainingTypeSymbol,
                    OptionalOfAttributeContext context => context.ContainingTypeSymbol,
                    _ => null
                };
                if (containingTypeSymbol is null || visitedTypes.Add(containingTypeSymbol))
                {
                    yield return attributeContext;
                }
            }
        }
    }
    #endregion
}
