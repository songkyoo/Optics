using static System.StringComparison;
using static Macaron.Optics.Generator.GenerationModelEquality;

namespace Macaron.Optics.Generator;

public sealed class AttributeGenerationModelComparer : IEqualityComparer<AttributeGenerationModel>
{
    #region Static
    public static readonly AttributeGenerationModelComparer Instance = new();
    #endregion

    #region Constructors
    private AttributeGenerationModelComparer()
    {
    }
    #endregion

    #region IEqualityComparer<AttributeGenerationModel> Interface
    public bool Equals(AttributeGenerationModel? x, AttributeGenerationModel? y)
    {
        return ReferenceEquals(x, y)
            || x is not null
            && y is not null
            && x.Kind == y.Kind
            && string.Equals(x.NamespaceName, y.NamespaceName, comparisonType: Ordinal)
            && string.Equals(x.HintName, y.HintName, comparisonType: Ordinal)
            && StringSequenceEquals(x.TypeDeclarations, y.TypeDeclarations)
            && TypeEquals(x.TargetType, y.TargetType);
    }

    public int GetHashCode(AttributeGenerationModel obj)
    {
        var hash = 17;

        hash = AddHashCode(hash, (int)obj.Kind);
        hash = AddHashCode(hash, obj.NamespaceName);
        hash = AddHashCode(hash, obj.HintName);

        foreach (var declaration in obj.TypeDeclarations)
        {
            hash = AddHashCode(hash, declaration);
        }

        return AddTypeHashCode(hash, obj.TargetType);
    }
    #endregion
}
