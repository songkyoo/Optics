﻿using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Macaron.Optics.Generator;

[Generator]
public class LensGenerator : IIncrementalGenerator
{
    #region Static
    private static string? ToFullyQualifiedName(ISymbol? symbol)
    {
        return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static INamedTypeSymbol? GetLensOfType(GeneratorSyntaxContext generatorSyntaxContext)
    {
        var invocationExpressionSyntax = (InvocationExpressionSyntax)generatorSyntaxContext.Node;

        if (invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax
                {
                    Identifier.Text: "Of",
                    TypeArgumentList.Arguments.Count: 1,
                } genericName
            }
        )
        {
            var symbol = generatorSyntaxContext.SemanticModel.GetSymbolInfo(genericName).Symbol as IMethodSymbol;
            if (symbol?.IsStatic is true &&
                ToFullyQualifiedName(symbol.ContainingType) == "global::Macaron.Optics.Lens"
            )
            {
                return generatorSyntaxContext
                    .SemanticModel
                    .GetTypeInfo(genericName.TypeArgumentList.Arguments[0])
                    .Type as INamedTypeSymbol;
            }
        }

        return null;
    }

    private static bool IsValidProperty(IPropertySymbol propertySymbol)
    {
        if (propertySymbol.GetMethod is null)
        {
            return false;
        }

        if (propertySymbol.IsIndexer)
        {
            return false;
        }

        return propertySymbol.SetMethod is not null or { IsReadOnly: true };
    }

    private static bool IsValidField(IFieldSymbol fieldSymbol)
    {
        return !fieldSymbol.IsReadOnly;
    }

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

        foreach (var typeSymbol in uniqueTypeSymbols)
        {
            var stringBuilder = new StringBuilder();

            var typeName = ToFullyQualifiedName(typeSymbol)!;
            var className = $"global_Macaron_Optics_LensOf_{typeName.Replace('.', '_').Replace("::", "_")}_Extensions";

            stringBuilder.AppendLine("// <auto-generated />");
            stringBuilder.AppendLine($"namespace Macaron.Optics");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine($"    public static class {className}");
            stringBuilder.AppendLine("    {");

            var members = typeSymbol
                .GetMembers()
                .Where(symbol => symbol.DeclaredAccessibility == Accessibility.Public)
                .Where(symbol =>
                    (symbol is IPropertySymbol prop && IsValidProperty(prop)) ||
                    (symbol is IFieldSymbol field && IsValidField(field))
                )
                .ToArray();
            foreach (var member in members)
            {
                var memberTypeName = ToFullyQualifiedName(member is IPropertySymbol prop
                    ? prop.Type
                    : ((IFieldSymbol)member).Type
                );

                stringBuilder.AppendLine($"        public static global::Macaron.Optics.Lens<{typeName}, {memberTypeName}> {member.Name}(");
                stringBuilder.AppendLine($"            this global::Macaron.Optics.LensOf<{typeName}> lensOf");
                stringBuilder.AppendLine("        )");
                stringBuilder.AppendLine("        {");
                stringBuilder.AppendLine($"            return global::Macaron.Optics.Lens<{typeName}, {memberTypeName}>.Of(");
                stringBuilder.AppendLine($"                getter: static source => source.{member.Name},");
                stringBuilder.AppendLine($"                setter: static (source, value) => source with");
                stringBuilder.AppendLine("                {");
                stringBuilder.AppendLine($"                    {member.Name} = value,");
                stringBuilder.AppendLine("                }");
                stringBuilder.AppendLine("            );");
                stringBuilder.AppendLine("        }");
            }

            stringBuilder.AppendLine("    }");
            stringBuilder.AppendLine("}");

            sourceProductionContext.AddSource(
                hintName: $"{className}.g.cs",
                sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
            );
        }
    }
    #endregion

    #region IIncrementalGenerator Interface
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var lensOfCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is InvocationExpressionSyntax,
                transform: static (generatorSyntaxContext, _) => GetLensOfType(generatorSyntaxContext)
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
