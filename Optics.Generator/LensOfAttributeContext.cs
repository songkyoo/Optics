using Microsoft.CodeAnalysis;

namespace Macaron.Optics.Generator;

internal sealed record LensOfAttributeContext(
    INamedTypeSymbol ContainingTypeSymbol,
    INamedTypeSymbol TypeSymbol
) : AttributeContext;
