using System.Collections.Immutable;
using static System.StringComparison;

namespace Macaron.Optics.Generator;

internal static class GenerationModelEquality
{
    public static bool TypeSequenceEquals(
        ImmutableArray<TypeGenerationModel> x,
        ImmutableArray<TypeGenerationModel> y
    )
    {
        if (x.Length != y.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Length; ++i)
        {
            if (!TypeEquals(x[i], y[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TypeEquals(TypeGenerationModel x, TypeGenerationModel y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (!string.Equals(x.FullyQualifiedName, y.FullyQualifiedName, comparisonType: Ordinal)
            || x.Members.Length != y.Members.Length
        )
        {
            return false;
        }

        for (var i = 0; i < x.Members.Length; ++i)
        {
            if (x.Members[i] != y.Members[i])
            {
                return false;
            }
        }

        return true;
    }

    public static bool StringSequenceEquals(ImmutableArray<string> x, ImmutableArray<string> y)
    {
        if (x.Length != y.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Length; ++i)
        {
            if (!string.Equals(x[i], y[i], Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static int AddTypeSequenceHashCode(int hash, ImmutableArray<TypeGenerationModel> types)
    {
        foreach (var type in types)
        {
            hash = AddTypeHashCode(hash, type);
        }

        return hash;
    }

    public static int AddTypeHashCode(int hash, TypeGenerationModel type)
    {
        hash = AddHashCode(hash, type.FullyQualifiedName);

        foreach (var member in type.Members)
        {
            hash = AddHashCode(hash, member.Name);
            hash = AddHashCode(hash, member.TypeName);
            hash = AddHashCode(hash, member.IsNullable ? 1 : 0);
        }

        return hash;
    }

    public static int AddHashCode(int hash, string? value)
    {
        return AddHashCode(hash, value is null ? 0 : StringComparer.Ordinal.GetHashCode(value));
    }

    public static int AddHashCode(int hash, int value)
    {
        return unchecked(hash * 31 + value);
    }
}
