using System.Collections.Immutable;

namespace Macaron.Optics.Generator;

public sealed record OfGenerationModel(
    ImmutableArray<TypeGenerationModel> LensTypes,
    ImmutableArray<TypeGenerationModel> OptionalTypes
);
