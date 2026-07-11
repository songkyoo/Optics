using System.Collections.Immutable;

namespace Macaron.Optics.Generator;

public sealed record TypeGenerationModel(
    string Name,
    int Arity,
    string FullyQualifiedName,
    ImmutableArray<MemberGenerationModel> Members
);
