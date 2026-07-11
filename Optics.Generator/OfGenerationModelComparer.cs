using System.Collections.Immutable;
using static Macaron.Optics.Generator.GenerationModelEquality;

namespace Macaron.Optics.Generator;

public sealed class OfGenerationModelComparer : IEqualityComparer<OfGenerationModel>
{
    #region Static
    public static readonly OfGenerationModelComparer Instance = new();
    #endregion

    #region Constructors
    private OfGenerationModelComparer()
    {
    }
    #endregion

    #region IEqualityComparer<OfGenerationModel> Interface
    public bool Equals(OfGenerationModel? x, OfGenerationModel? y)
    {
        return ReferenceEquals(x, y)
            || x is not null
            && y is not null
            && TypeSequenceEquals(x.LensTypes, y.LensTypes)
            && TypeSequenceEquals(x.OptionalTypes, y.OptionalTypes);
    }

    public int GetHashCode(OfGenerationModel obj)
    {
        var hash = 17;

        hash = AddTypeSequenceHashCode(hash, obj.LensTypes);
        hash = AddTypeSequenceHashCode(hash, obj.OptionalTypes);

        return hash;
    }
    #endregion
}
