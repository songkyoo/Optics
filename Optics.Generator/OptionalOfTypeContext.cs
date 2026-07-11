using Microsoft.CodeAnalysis;

namespace Macaron.Optics.Generator;

internal sealed record OptionalOfTypeContext(
    INamedTypeSymbol Symbol
) : TypeContext;
