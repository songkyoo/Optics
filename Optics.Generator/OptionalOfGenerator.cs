using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Macaron.Optics.Generator.Helpers;

namespace Macaron.Optics.Generator;

[Generator]
public class OptionalOfGenerator : IIncrementalGenerator
{
    #region Constants
    private const string OptionalOfAttributeSource =
        """
        #nullable enable

        using System.Diagnostics;

        namespace Macaron.Optics;

        [Conditional("SOURCE_GENERATOR_ONLY")]
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        internal class OptionalOfAttribute : Attribute
        {
            public Type? TargetType { get; }

            public OptionalOfAttribute()
            {
                TargetType = null;
            }

            public OptionalOfAttribute(Type targetType)
            {
                TargetType = targetType;
            }
        }
        """;
    private const string OptionalOfAttributeName = "global::Macaron.Optics.OptionalOfAttribute";
    #endregion

    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("OptionalOfAttribute.g.cs", SourceText.From(OptionalOfAttributeSource, Encoding.UTF8));
        });

        var classDeclarations = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: static (context, _) => GetClassWithLensOfAttribute(context, OptionalOfAttributeName)
            )
            .Where(static classDeclaration => classDeclaration is not null);

        context.RegisterSourceOutput(
            source: classDeclarations,
            action: (sourceProductionContext, lensOfContext) => AddSource(
                sourceProductionContext: sourceProductionContext,
                lensOfContext: lensOfContext!,
                generateLensOfMembers: GenerateOptionalOfMembers
            )
        );
    }
    #endregion
}
