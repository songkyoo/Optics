using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Macaron.Optics.Generator.Helper;

namespace Macaron.Optics.Generator;

[Generator]
public class LensGenerator : IIncrementalGenerator
{
    #region Constants
    private const string LensOfTypeName = "global::Macaron.Optics.LensOf";
    #endregion

    #region Static
    private static void GenerateSource(
        SourceProductionContext sourceProductionContext,
        ImmutableArray<INamedTypeSymbol> lensTypeSymbols
    )
    {
        var uniqueTypeSymbols = lensTypeSymbols.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToArray();
        if (uniqueTypeSymbols.Length == 0)
        {
            return;
        }

        var stringBuilder = CreateStringBuilderWithFileHeader();

        // begin namespace
        stringBuilder.AppendLine("namespace Macaron.Optics");
        stringBuilder.AppendLine("{");

        // begin extension methods
        stringBuilder.AppendLine($"    internal static class LensOfExtensions");
        stringBuilder.AppendLine($"    {{");

        for (int i = 0; i < uniqueTypeSymbols.Length; ++i)
        {
            var typeSymbol = uniqueTypeSymbols[i];

            var members = GenerateLensOfMembers(typeSymbol);
            if (members.Length == 0)
            {
                continue;
            }

            var typeName = ToFullyQualifiedName(typeSymbol)!;
            for (var j = 0; j < members.Length; ++j)
            {
                var (memberDeclaration, lines) = members[j];

                stringBuilder.AppendLine($"        public static {memberDeclaration}(");
                stringBuilder.AppendLine($"            this {LensOfTypeName}<{typeName}> lensOf");
                stringBuilder.AppendLine($"        )");
                stringBuilder.AppendLine($"        {{");

                stringBuilder.AppendLine($"            return {lines[0]}");
                foreach (var line in lines.Skip(1))
                {
                    stringBuilder.AppendLine($"            {line}");
                }

                stringBuilder.AppendLine($"        }}");

                if (j < members.Length - 1)
                {
                    stringBuilder.AppendLine();
                }
            }

            if (i < uniqueTypeSymbols.Length - 1)
            {
                stringBuilder.AppendLine();
            }
        }

        // end extension methods
        stringBuilder.AppendLine($"    }}");

        // end namespace
        stringBuilder.AppendLine($"}}");

        sourceProductionContext.AddSource(
            hintName: "LensOfExtensions.g.cs",
            sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
        );
    }
    #endregion

    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var lensOfCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is InvocationExpressionSyntax,
                transform: static (generatorSyntaxContext, _) => GetLensOfType(generatorSyntaxContext, LensTypeName)
            )
            .Where(static typeName => typeName is not null)
            .Select(static (typeName, _) => typeName!)
            .Collect();

        context.RegisterSourceOutput(
            source: lensOfCalls,
            action: GenerateSource
        );
    }
    #endregion
}
