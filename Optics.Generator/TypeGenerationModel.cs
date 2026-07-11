using System.Collections.Immutable;

namespace Macaron.Optics.Generator;

public sealed record TypeGenerationModel(
    string TypeName,
    ImmutableArray<MemberGenerationModel> Members
);
