using Microsoft.CodeAnalysis;

namespace Macaron.Optics.Generator;

internal sealed record TypeContext(
    OpticsKind Kind,
    INamedTypeSymbol Symbol
);
