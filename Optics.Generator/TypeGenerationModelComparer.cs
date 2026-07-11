using static Macaron.Optics.Generator.GenerationModelEquality;

namespace Macaron.Optics.Generator;

public sealed class TypeGenerationModelComparer : IEqualityComparer<TypeGenerationModel>
{
    #region Static
    public static readonly TypeGenerationModelComparer Instance = new();
    #endregion

    #region Constructors
    private TypeGenerationModelComparer()
    {
    }
    #endregion

    #region IEqualityComparer<TypeGenerationModel> Interface
    public bool Equals(TypeGenerationModel? x, TypeGenerationModel? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        return x is not null
            && y is not null
            && TypeEquals(x, y);
    }

    public int GetHashCode(TypeGenerationModel obj)
    {
        return AddTypeHashCode(17, obj);
    }
    #endregion
}
