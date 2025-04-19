using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
                transform: static (context, _) => GetClassWithLensOfAttribute(context)
            )
            .Where(static classDeclaration => classDeclaration is not null);

        context.RegisterSourceOutput(classDeclarations, Generate!);
    }

    private sealed record LensOfContext(INamedTypeSymbol ContainingTypeSymbol, INamedTypeSymbol TargetTypeSymbol);

    private static LensOfContext? GetClassWithLensOfAttribute(GeneratorSyntaxContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!typeSymbol.IsStatic || typeSymbol.IsGenericType)
        {
            return null;
        }

        var lensOfAttribute = typeSymbol
            .GetAttributes()
            .FirstOrDefault(attributeData => ToFullyQualifiedName(attributeData.AttributeClass) == LensOfAttributeName);
        if (lensOfAttribute is null)
        {
            return null;
        }

        var targetTypeSymbol = lensOfAttribute.ConstructorArguments.Length == 1
            ? lensOfAttribute.ConstructorArguments[0].Value as INamedTypeSymbol
            : typeSymbol.ContainingType;

        return targetTypeSymbol == null ? null : new LensOfContext(
            ContainingTypeSymbol: typeSymbol,
            TargetTypeSymbol: targetTypeSymbol
        );
    }

    private static void Generate(
        SourceProductionContext sourceProductionContext,
        LensOfContext lensOfContext
    )
    {
        var (containingTypeSymbol, targetTypeSymbol) = lensOfContext;
        var stringBuilder = CreateStringBuilderWithFileHeader();

        // namespace
        stringBuilder.AppendLine($"namespace {containingTypeSymbol.ContainingNamespace.ToDisplayString()};");
        stringBuilder.AppendLine($"");

        // get nestedTypes
        var nestedTypes = new List<INamedTypeSymbol>();
        var parentType = containingTypeSymbol.ContainingType;
        while (parentType != null)
        {
            nestedTypes.Add(parentType);
            parentType = parentType.ContainingType;
        }

        var depthSpacerText = "";

        // begin nestedTypes
        for (var i = nestedTypes.Count - 1; i >= 0; --i)
        {
            var nestedType = nestedTypes[i];

            stringBuilder.AppendLine($"{depthSpacerText}{GetPartialTypeDeclarationString(nestedType)}");
            stringBuilder.AppendLine($"{depthSpacerText}{{");

            depthSpacerText += "    ";
        }

        // begin containingType
        stringBuilder.AppendLine($"{depthSpacerText}{GetPartialTypeDeclarationString(containingTypeSymbol)}");
        stringBuilder.AppendLine($"{depthSpacerText}{{");

        // generate targetType members
        depthSpacerText += "    ";

        var members = GenerateLensOfMembers(targetTypeSymbol);
        for (var i = 0; i < members.Length; ++i)
        {
            var (memberDeclaration, lines) = members[i];

            stringBuilder.AppendLine($"{depthSpacerText}public static readonly {memberDeclaration} = {lines[0]}");
            foreach (var line in lines.Skip(1))
            {
                stringBuilder.AppendLine($"{depthSpacerText}{line}");
            }

            if (i < members.Length - 1)
            {
                stringBuilder.AppendLine();
            }
        }

        depthSpacerText = depthSpacerText[..^4];

        // end containedType
        stringBuilder.AppendLine($"{depthSpacerText}}}");

        // end nestedTypes
        for (var i = 0; i < nestedTypes.Count; ++i)
        {
            depthSpacerText = depthSpacerText[..^4];

            stringBuilder.AppendLine($"{depthSpacerText}}}");
        }

        sourceProductionContext.AddSource(
            hintName: GetHintName(targetTypeSymbol),
            sourceText: SourceText.From(stringBuilder.ToString(), Encoding.UTF8)
        );

        #region Local Functions
        static string GetPartialTypeDeclarationString(INamedTypeSymbol typeSymbol)
        {
            var typeKindString = GetTypeKindString(typeSymbol);
            var typeNameString = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            return $"partial {typeKindString} {typeNameString}";
        }

        static string GetTypeKindString(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol.IsRecord)
            {
                return "record";
            }

            return typeSymbol.TypeKind switch
            {
                TypeKind.Class => "class",
                TypeKind.Struct => "struct",
                TypeKind.Interface => "interface",
                _ => throw new InvalidOperationException($"Invalid type kind: {typeSymbol.TypeKind}")
            };
        }

        static string GetHintName(INamedTypeSymbol typeSymbol)
        {
            var qualifiedName = ToFullyQualifiedName(typeSymbol)!;

            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(qualifiedName);
            var hash = sha.ComputeHash(bytes);
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()[..8];

            return $"{hashString}_{typeSymbol.Name}_{typeSymbol.Arity}.g.cs";
        }
        #endregion
    }
    #endregion
}
