using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Macaron.Optics.Generator;

internal static class Helper
{
    public const string MaybeTypeName = "global::Macaron.Functional.Maybe";

    public static string? ToFullyQualifiedName(ISymbol? symbol)
    {
        return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public static INamedTypeSymbol? GetLensOfType(GeneratorSyntaxContext generatorSyntaxContext, string containingType)
    {
        var genericNameSyntax = GetGenericNameFromInvocation((InvocationExpressionSyntax)generatorSyntaxContext.Node);
        if (genericNameSyntax is null)
        {
            return null;
        }

        var semanticModel = generatorSyntaxContext.SemanticModel;
        var methodSymbol = semanticModel.GetSymbolInfo(genericNameSyntax).Symbol as IMethodSymbol;
        if (methodSymbol?.IsStatic is not true ||
            methodSymbol.Name != "Of" ||
            ToFullyQualifiedName(methodSymbol.ContainingType) != containingType
        )
        {
            return null;
        }

        var typeArgumentList = genericNameSyntax.TypeArgumentList;
        if (typeArgumentList.Arguments.Count != 1)
        {
            return null;
        }

        var typeArgument = genericNameSyntax.TypeArgumentList.Arguments[0];
        var symbolInfo = semanticModel.GetSymbolInfo(typeArgument);
        var namedTypeSymbol = symbolInfo.Symbol as INamedTypeSymbol;
        if (namedTypeSymbol is null)
        {
            return null;
        }

        // Nullable한 형식은 지원하지 않는다.
        if ((namedTypeSymbol.IsValueType && namedTypeSymbol.ToString().EndsWith("?")) ||
            typeArgument.ToString().EndsWith("?")
        )
        {
            return null;
        }

        // with 문을 지원하는 형식만 사용한다.
        return namedTypeSymbol.IsRecord is true || namedTypeSymbol.TypeKind == TypeKind.Struct
            ? namedTypeSymbol
            : null;
    }

    public static GenericNameSyntax? GetGenericNameFromInvocation(
        InvocationExpressionSyntax invocationExpressionSyntax
    )
    {
        return invocationExpressionSyntax.Expression switch
        {
            MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } => genericName,
            GenericNameSyntax genericName => genericName,
            _ => null
        };
    }

    public static bool IsValidProperty(IPropertySymbol propertySymbol)
    {
        if (propertySymbol.NullableAnnotation == NullableAnnotation.Annotated ||
            propertySymbol.GetMethod is null ||
            propertySymbol.IsIndexer
        )
        {
            return false;
        }

        return propertySymbol.SetMethod is not null or { IsReadOnly: true };
    }

    public static bool IsValidField(IFieldSymbol fieldSymbol)
    {
        return  !fieldSymbol.IsConst &&
            !fieldSymbol.IsStatic &&
            !fieldSymbol.IsReadOnly &&
            fieldSymbol.NullableAnnotation != NullableAnnotation.Annotated;
    }
}
