using Microsoft.CodeAnalysis;

namespace Macaron.Optics.Generator;

internal sealed record OptionalOfAttributeContext(
    INamedTypeSymbol ContainingTypeSymbol,
    INamedTypeSymbol TypeSymbol
) : AttributeContext;
