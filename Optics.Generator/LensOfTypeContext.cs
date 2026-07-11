using Microsoft.CodeAnalysis;

namespace Macaron.Optics.Generator;

internal sealed record LensOfTypeContext(
    INamedTypeSymbol Symbol
) : TypeContext;
