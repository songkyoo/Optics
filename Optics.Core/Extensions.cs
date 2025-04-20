using System.Collections.Immutable;
using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class Extensions
{
    public static Optional<T, TValue> Key<T, TKey, TValue>(
        this Lens<T, ImmutableDictionary<TKey, TValue>> lens,
        TKey key
    ) where TKey : notnull
    {
        return lens.At(
            getter: static (dict, key) => dict.GetItem(key),
            setter: static (dict, key, value) => dict.SetItem(key, value),
            index: key
        );
    }

    public static Optional<T, TValue> Key<T, TKey, TValue>(
        this Lens<T, ImmutableSortedDictionary<TKey, TValue>> lens,
        TKey key
    ) where TKey : notnull
    {
        return lens.At(
            getter: static (dict, key) => dict.GetItem(key),
            setter: static (dict, key, value) => dict.SetItem(key, value),
            index: key
        );
    }

    public static Optional<T, TValue> Index<T, TValue>(
        this Lens<T, ImmutableList<TValue>> lens,
        int index
    )
    {
        return lens.At(
            getter: static (list, index) => list.GetItem(index),
            setter: static (list, index, value) => index.IsWithinBoundsOf(list) ? list.SetItem(index, value) : list,
            index: index
        );
    }

    public static Optional<T, TValue> Index<T, TValue>(
        this Lens<T, ImmutableArray<TValue>> lens,
        int index
    )
    {
        return lens.At(
            getter: static (array, index) => array.GetItem(index),
            setter: static (array, index, value) => index.IsWithinBoundsOf(array) ? array.SetItem(index, value) : array,
            index: index
        );
    }

    public static Optional<T, TIndexedValue> At<T, TValue, TIndex, TIndexedValue>(
        this Lens<T, TValue> lens,
        Func<TValue, TIndex, Maybe<TIndexedValue>> getter,
        Func<TValue, TIndex, TIndexedValue, TValue> setter,
        TIndex index
    ) where TIndex : notnull
    {
        return Optional<T, TIndexedValue>.Of(
            optionalGetter: source => getter(lens.Get(source), index),
            setter: (source, value) => lens.Set(source, setter(lens.Get(source), index, value))
        );
    }

    public static Lens<T, TValue> OrElse<T, TValue>(
        this Optional<T, TValue> optional,
        Func<T, TValue> getDefaultValue
    )
    {
        return optional.ToLens(getDefaultValue);
    }

    public static Lens<T, TValue> OrElse<T, TValue>(
        this Optional<T, TValue> optional,
        Func<TValue> getDefaultValue
    )
    {
        return optional.ToLens(getDefaultValue);
    }

    public static Lens<T, TValue> OrElse<T, TValue>(
        this Optional<T, TValue> optional,
        TValue defaultValue
    )
    {
        return optional.ToLens(defaultValue);
    }
}

file static class FileScopeExtensions
{
    public static Maybe<TValue> GetItem<TKey, TValue>(this ImmutableDictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? Just(value) : Nothing<TValue>();
    }

    public static Maybe<TValue> GetItem<TKey, TValue>(this ImmutableSortedDictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? Just(value) : Nothing<TValue>();
    }

    public static Maybe<T> GetItem<T>(this ImmutableList<T> list, int index)
    {
        return index.IsWithinBoundsOf(list) ? Just(list[index]) : Nothing<T>();
    }

    public static Maybe<T> GetItem<T>(this ImmutableArray<T> array, int index)
    {
        return index.IsWithinBoundsOf(array) ? Just(array[index]) : Nothing<T>();
    }

    public static bool IsWithinBoundsOf<T>(this int value, IReadOnlyList<T> list)
    {
        return value >= 0 && value < list.Count;
    }
}
