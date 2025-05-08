using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Macaron.Optics.Generator.Helpers;

namespace Macaron.Optics.Generator;

[Generator]
public class LensOfGenerator : IIncrementalGenerator
{
    #region Constants
    private const string LensOfAttributeSource =
        """
        #nullable enable

        using System.Diagnostics;

        namespace Macaron.Optics;

        [Conditional("SOURCE_GENERATOR_ONLY")]
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        internal class LensOfAttribute : Attribute
        {
            public Type? TargetType { get; }

            public LensOfAttribute()
            {
                TargetType = null;
            }

            public LensOfAttribute(Type targetType)
            {
                TargetType = targetType;
            }
        }
        """;
    private const string LensOfAttributeName = "global::Macaron.Optics.LensOfAttribute";
    #endregion

    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("LensOfAttribute.g.cs", SourceText.From(LensOfAttributeSource, Encoding.UTF8));
        });

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
