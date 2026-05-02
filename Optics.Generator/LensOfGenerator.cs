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
        var visitedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        IncrementalValuesProvider<AttributeContext> valuesProvider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: (generatorSyntaxContext, _) => GetAttributeContext(generatorSyntaxContext, visitedTypes)
            );

        context.RegisterSourceOutput(
            source: valuesProvider,
            action: (sourceProductionContext, attributeContext) =>
            {
                foreach (var diagnostic in attributeContext.Diagnostics)
                {
                    sourceProductionContext.ReportDiagnostic(diagnostic);
                }

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
            });
    }
    #endregion
}
