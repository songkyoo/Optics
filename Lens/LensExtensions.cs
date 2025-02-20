using System.Collections.Immutable;

namespace Macaron.Optics;

public static class LensExtensions
{
    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
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

    public static OptionLens<T, TValue2> Compose<T, TValue1, TValue2>(
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

    public static OptionLens<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionLens<T, TValue1> lens1,
        OptionLens<TValue1, TValue2> lens2
    )
    {
        return OptionLens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = value1.IsSome ? lens2.Get(value1.Value) : Option.None<TValue2>();

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);

                var newValue1 = value1.IsSome ? Option.Some(lens2.Set(value1.Value, value)) : Option.None<TValue1>();
                var newValue0 = newValue1.IsSome ? lens1.Set(source, newValue1.Value) : source;

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
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
                var value2 = lens2.Get(value1.IsSome ? value1.Value : getDefaultValue(value0));

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);

                var newValue1 = lens2.Set(value1.IsSome ? value1.Value : getDefaultValue(value0), value);
                var newValue0 = lens1.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    public static OptionLens<T, TValue> Key<T, TKey, TValue>(
        this Lens<T, ImmutableDictionary<TKey, TValue>> lens,
        TKey key,
        Func<ImmutableDictionary<TKey, TValue>, TKey, TValue, ImmutableDictionary<TKey, TValue>>? setItem = null
    ) where TKey : notnull
    {
        return OptionLens<T, TValue>.Of(
            getter: source => lens.Get(source).TryGetValue(key, out var value)
                ? Option.Some(value)
                : Option.None<TValue>(),
            setter: (source, value) => lens.Set(source, (setItem ?? SetItem)(lens.Get(source), key, value))
        );

        #region Local Functions
        static ImmutableDictionary<TKey, TValue> SetItem(ImmutableDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            return dict.ContainsKey(key)
                ? dict.SetItem(key, value)
                : throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");
        }
        #endregion
    }

    public static OptionLens<T, TValue> Index<T, TValue>(
        this Lens<T, ImmutableList<TValue>> lens,
        int index
    )
    {
        return OptionLens<T, TValue>.Of(
            getter: source => index >= 0 && index < lens.Get(source).Count
                ? Option.Some(lens.Get(source)[index])
                : Option.None<TValue>(),
            setter: (source, value) => index >= 0 && index < lens.Get(source).Count
                ? lens.Set(source, lens.Get(source).SetItem(index, value))
                : source
        );
    }

    public static Lens<T, TValue> OrElse<T, TValue>(
        this OptionLens<T, TValue> lens, TValue defaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => lens.Get(source) is var option && option.IsSome
                ? option.Value
                : defaultValue,
            setter: (source, value) => lens.Set(source, value)
        );
    }

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
}
