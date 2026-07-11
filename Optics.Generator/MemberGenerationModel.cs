namespace Macaron.Optics.Generator;

public readonly record struct MemberGenerationModel(
    string Name,
    string TypeName,
    bool IsNullable
);
