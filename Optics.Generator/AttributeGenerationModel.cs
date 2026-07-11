using System.Collections.Immutable;

namespace Macaron.Optics.Generator;

public sealed record AttributeGenerationModel(
    OpticsKind Kind,
    string? NamespaceName,
    ImmutableArray<string> TypeDeclarations,
    string HintName,
    TypeGenerationModel TargetType
);
