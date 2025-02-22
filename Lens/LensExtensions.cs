using System.Collections.Immutable;

using static Macaron.Optics.Option;

namespace Macaron.Optics;

file static class FileScopeExtensions
{
    public static Option<TValue> GetItem<TKey, TValue>(this ImmutableDictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? Some(value) : None<TValue>();
    }

    public static Option<TValue> GetItem<TKey, TValue>(this ImmutableSortedDictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? Some(value) : None<TValue>();
    }

    public static Option<T> GetItem<T>(this ImmutableList<T> list, int index)
    {
        return index.IsWithinBoundsOf(list) ? Some(list[index]) : None<T>();
    }

    public static Option<T> GetItem<T>(this ImmutableArray<T> array, int index)
    {
        return index.IsWithinBoundsOf(array) ? Some(array[index]) : None<T>();
    }

    public static bool IsWithinBoundsOf<T>(this int value, IReadOnlyList<T> list)
    {
        return value >= 0 && value < list.Count;
    }
}

public static class LensExtensions
{
    public static T Update<T, TValue>(
        this Lens<T, TValue> lens,
        T source,
        Func<TValue, TValue> update
    )
    {
        var value = lens.Get(source);
        var newValue = update(value);
        var newSource = lens.Set(source, newValue);

        return newSource;
    }

    public static T Update<T, TValue>(
        this OptionLens<T, TValue> lens,
        T source,
        Func<Option<TValue>, Option<TValue>> update
    )
    {
        var value = lens.Get(source);
        var newValue = update(value);
        var newSource = newValue.IsSome ? lens.Set(source, newValue.Value) : source;

        return newSource;
    }

    public static Lens<T, TValue2> Chain<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens1,
        Lens<TValue1, TValue2> lens2
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);

                var newValue1 = lens2.Set(value1, value);
                var newValue0 = lens1.Set(value0, newValue1);

                return newValue0;
            }
        );
    }

    public static OptionLens<T, TValue2> Chain<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens1,
        OptionLens<TValue1, TValue2> lens2
    )
    {
        return OptionLens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);

                var newValue1 = lens2.Set(value1, value);
                var newValue0 = lens1.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    public static OptionLens<T, TValue2> Chain<T, TValue1, TValue2>(
        this OptionLens<T, TValue1> lens1,
        OptionLens<TValue1, TValue2> lens2
    )
    {
        return OptionLens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = value1.IsSome ? lens2.Get(value1.Value) : None<TValue2>();

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);

                var newValue1 = value1.IsSome ? Some(lens2.Set(value1.Value, value)) : None<TValue1>();
                var newValue0 = newValue1.IsSome ? lens1.Set(source, newValue1.Value) : source;

                return newValue0;
            }
        );
    }

    public static OptionLens<T, TValue2> Chain<T, TValue1, TValue2>(
        this OptionLens<T, TValue1> lens1,
        OptionLens<Option<TValue1>, TValue2> lens2
    )
    {
        return OptionLens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);

                var newValue1 = lens2.Set(value1, value);
                var newValue0 = newValue1.IsSome ? lens1.Set(source, newValue1.Value): source;

                return newValue0;
            }
        );
    }

    public static OptionLens<Option<T>, TValue2> Chain<T, TValue1, TValue2>(
        this OptionLens<Option<T>, TValue1> lens1,
        OptionLens<Option<TValue1>, TValue2> lens2
    )
    {
        return OptionLens<Option<T>, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);

                var newValue1 = lens2.Set(value1, value);
                var newValue0 = newValue1.IsSome ? lens1.Set(source, newValue1.Value): source;

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Chain<T, TValue1, TValue2>(
        this OptionLens<T, TValue1> lens1,
        Lens<TValue1, TValue2> lens2,
        Func<T, TValue1> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1.GetOrElse(getDefaultValue(value0)));

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);

                var newValue1 = lens2.Set(value1.GetOrElse(getDefaultValue(value0)), value);
                var newValue0 = lens1.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    public static OptionLens<T, TValue> ToOptionLens<T, TValue>(this Lens<T, TValue> lens)
    {
        return OptionLens<T, TValue>.Of(
            getter: source => Some(lens.Get(source)),
            setter: (source, value) => lens.Set(source, value)
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(this OptionLens<T, TValue> lens, Func<T, TValue> getDefaultValue)
    {
        return Lens<T, TValue>.Of(
            getter: source => lens.Get(source).GetOrElse(getDefaultValue(source)),
            setter: (source, value) => lens.Get(source) is var val && val.IsSome ? lens.Set(source, val.Value) : source
        );
    }

    public static OptionLens<T, TValue> Key<T, TKey, TValue>(
        this Lens<T, ImmutableDictionary<TKey, TValue>> lens,
        TKey key
    ) where TKey : notnull
    {
        return lens.AtKey(
            getter: static (dict, key) => dict.GetItem(key),
            setter: static (dict, key, value) => dict.SetItem(key, value),
            key: key
        );
    }

    public static OptionLens<T, TValue> Key<T, TKey, TValue>(
        this Lens<T, ImmutableSortedDictionary<TKey, TValue>> lens,
        TKey key
    ) where TKey : notnull
    {
        return lens.AtKey(
            getter: static (dict, key) => dict.GetItem(key),
            setter: static (dict, key, value) => dict.SetItem(key, value),
            key: key
        );
    }

    public static OptionLens<T, TValue> Index<T, TValue>(
        this Lens<T, ImmutableList<TValue>> lens,
        int index
    )
    {
        return lens.AtKey(
            getter: static (list, index) => list.GetItem(index),
            setter: static (list, index, value) => list.SetItem(index, value),
            key: index
        );
    }

    public static OptionLens<T, TValue> Index<T, TValue>(
        this Lens<T, ImmutableArray<TValue>> lens,
        int index
    )
    {
        return lens.AtKey(
            getter: static (array, index) => array.GetItem(index),
            setter: static (array, index, value) => array.SetItem(index, value),
            key: index
        );
    }

    public static OptionLens<T, TKeyedValue> AtKey<T, TValue, TKey, TKeyedValue>(
        this Lens<T, TValue> lens,
        Func<TValue, TKey, Option<TKeyedValue>> getter,
        Func<TValue, TKey, TKeyedValue, TValue> setter,
        TKey key
    ) where TKey : notnull
    {
        return OptionLens<T, TKeyedValue>.Of(
            getter: source => getter(lens.Get(source), key),
            setter: (source, value) => lens.Set(source, setter(lens.Get(source), key, value))
        );
    }

    public static Lens<T, TValue> OrElse<T, TValue>(
        this OptionLens<T, TValue> lens, TValue defaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => lens.Get(source) is var option && option.IsSome ? option.Value : defaultValue,
            setter: (source, value) => lens.Set(source, value)
        );
    }
}
