using Macaron.Functional;

namespace Macaron.Optics;

public static class SetterExtensions
{
    public static Setter<T, TValue> Transform<T, TValue>(
        this Setter<T, TValue> setter,
        Func<T, TValue, TValue> mapSet
    )
    {
        return Setter<T, TValue>.Of((source, value) =>
        {
            var newValue1 = mapSet(source, value);
            var newValue0 = setter.Set(source, newValue1);

            return newValue0;
        });
    }

    public static Setter<T, TValue> Transform<T, TValue>(
        this Setter<T, TValue> setter,
        Func<TValue, TValue> mapSet
    )
    {
        return Setter<T, TValue>.Of((source, value) =>
        {
            var newValue1 = mapSet(value);
            var newValue0 = setter.Set(source, newValue1);

            return newValue0;
        });
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this Setter<T, TValue> setter,
        Func<T, Maybe<TValue>> optionalGetter
    )
    {
        return Optional<T, TValue>.Of(optionalGetter, setter.Set);
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this Setter<T, TValue> setter,
        OptionalGetter<T, TValue> optionalGetter
    )
    {
        return Optional<T, TValue>.Of(optionalGetter.Get, setter.Set);
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this Setter<T, TValue> setter,
        Func<T, TValue> getter
    )
    {
        return Optional<T, TValue>.Of(getter, setter.Set);
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this Setter<T, TValue> setter,
        Getter<T, TValue> getter
    )
    {
        return Optional<T, TValue>.Of(getter.Get, setter.Set);
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this Setter<T, TValue> setter,
        Func<T, TValue> getter
    )
    {
        return Lens<T, TValue>.Of(getter, setter.Set);
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this Setter<T, TValue> setter,
        Getter<T, TValue> getter
    )
    {
        return Lens<T, TValue>.Of(getter.Get, setter.Set);
    }
}
