﻿using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Macaron.Optics.Generator.Helper;

namespace Macaron.Optics.Generator;

[Generator]
public class LensGenerator : IIncrementalGenerator
{
    #region Static
    private const string LensTypeName = "global::Macaron.Optics.Lens";
    private const string LensOfTypeName = "global::Macaron.Optics.LensOf";

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

        var lensOfStringBuilder = new StringBuilder();

        lensOfStringBuilder.AppendLine($"    public static class LensOfExtensions");
        lensOfStringBuilder.AppendLine("    {");

        for (int i = 0; i < uniqueTypeSymbols.Length; ++i)
        {
            var typeSymbol = uniqueTypeSymbols[i];
            var members = typeSymbol
                .GetMembers()
                .Where(symbol => symbol.DeclaredAccessibility == Accessibility.Public)
                .Where(symbol =>
                    (symbol is IPropertySymbol propertySymbol && IsValidProperty(propertySymbol)) ||
                    (symbol is IFieldSymbol fieldSymbol && IsValidField(fieldSymbol))
                )
                .ToArray();
            if (members.Length == 0)
            {
                continue;
            }

            var typeName = ToFullyQualifiedName(typeSymbol)!;

            for (int j = 0; j < members.Length; ++j)
            {
                var member = members[j];
                var memberTypeName = ToFullyQualifiedName(member is IPropertySymbol propertySymbol
                    ? propertySymbol.Type
                    : ((IFieldSymbol)member).Type
                );

                lensOfStringBuilder.AppendLine($"       public static {LensTypeName}<{typeName}, {memberTypeName}> {member.Name}(");
                lensOfStringBuilder.AppendLine($"           this {LensOfTypeName}<{typeName}> lensOf");
                lensOfStringBuilder.AppendLine("       )");
                lensOfStringBuilder.AppendLine("       {");
                lensOfStringBuilder.AppendLine($"           return {LensTypeName}<{typeName}, {memberTypeName}>.Of(");
                lensOfStringBuilder.AppendLine($"               getter: static source => source.{member.Name},");
                lensOfStringBuilder.AppendLine($"               setter: static (source, value) => source with");
                lensOfStringBuilder.AppendLine("               {");
                lensOfStringBuilder.AppendLine($"                   {member.Name} = value,");
                lensOfStringBuilder.AppendLine("               }");
                lensOfStringBuilder.AppendLine("           );");
                lensOfStringBuilder.AppendLine("       }");

                if (j < members.Length - 1)
                {
                    lensOfStringBuilder.AppendLine();
                }
            }

            if (i < uniqueTypeSymbols.Length - 1)
            {
                lensOfStringBuilder.AppendLine();
            }
        }

        lensOfStringBuilder.AppendLine("    }");

        AddSource(sourceProductionContext, "LensOfExtensions.g.cs", lensOfStringBuilder.ToString());

        #region Local Functions
        static void AddSource(SourceProductionContext sourceProductionContext, string hintName, string sourceText)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("// <auto-generated />");
            stringBuilder.AppendLine("#nullable enable");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace Macaron.Optics");
            stringBuilder.AppendLine("{");
            stringBuilder.Append(sourceText);
            stringBuilder.AppendLine("}");

            sourceProductionContext.AddSource(
                hintName: hintName,
                sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
            );
        }
        #endregion
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
