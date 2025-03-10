using Macaron.Functional;

namespace Macaron.Optics;

public static class OptionalGetterExtensions
{
    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter,
        Func<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = optionalGetter.Get(value0);
            var value2 = getter.Invoke(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter,
        Getter<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = optionalGetter.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter1,
        Func<Maybe<TValue1>, Maybe<TValue2>> optionalGetter2
    )
    {
        return OptionalGetter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = optionalGetter1.Get(value0);
            var value2 = optionalGetter2.Invoke(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter1,
        OptionalGetter<Maybe<TValue1>, TValue2> optionalGetter2
    )
    {
        return OptionalGetter.Of<T, TValue2>(source =>
        {
            var value0 = source;
            var value1 = optionalGetter1.Get(value0);
            var value2 = optionalGetter2.Get(value1);

            return value2;
        });
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this OptionalGetter<T, TValue> optional,
        Func<T, TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            source => optional.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(source)
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this OptionalGetter<T, TValue> optional,
        Func<TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            source => optional.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue()
        );
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<T, TValue, T> setter
    )
    {
        return Optional<T, TValue>.Of(optionalGetter.Get, setter);
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Setter<T, TValue> setter
    )
    {
        return Optional<T, TValue>.Of(optionalGetter, setter);
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<TValue, T> constructor
    )
    {
        return Prism<T, TValue>.Of(optionalGetter.Get, constructor);
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Constructor<T, TValue> constructor
    )
    {
        return Prism<T, TValue>.Of(optionalGetter, constructor);
    }
}
