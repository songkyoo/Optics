using Macaron.Functional;

namespace Macaron.Optics;

public static class ConstructorExtensions
{
    public static Constructor<T, TValue2> Compose<T, TValue1, TValue2>(
        this Constructor<T, TValue1> constructor1,
        Func<TValue2, TValue1> constructor2
    )
    {
        return Constructor<T, TValue2>.Of(value =>
        {
            var newValue1 = constructor2.Invoke(value);
            var newValue0 = constructor1.Construct(newValue1);

            return newValue0;
        });
    }

    public static Constructor<T, TValue2> Compose<T, TValue1, TValue2>(
        this Constructor<T, TValue1> constructor1,
        Constructor<TValue1, TValue2> constructor2
    )
    {
        return Constructor<T, TValue2>.Of(value =>
        {
            var newValue1 = constructor2.Construct(value);
            var newValue0 = constructor1.Construct(newValue1);

            return newValue0;
        });
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Constructor<T, TValue> constructor,
        Func<T, TValue> getter
    )
    {
        return Prism<T, TValue>.Of(getter, constructor.Construct);
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Constructor<T, TValue> constructor,
        Getter<T, TValue> getter
    )
    {
        return Prism<T, TValue>.Of(getter, constructor);
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Constructor<T, TValue> constructor,
        Func<T, Maybe<TValue>> optionalGetter
    )
    {
        return Prism<T, TValue>.Of(optionalGetter, constructor.Construct);
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Constructor<T, TValue> constructor,
        OptionalGetter<T, TValue> optionalGetter
    )
    {
        return Prism<T, TValue>.Of(optionalGetter, constructor);
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Constructor<T, TValue> constructor,
        Func<T, TValue> getter
    )
    {
        return Iso<T, TValue>.Of(getter, constructor.Construct);
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Constructor<T, TValue> constructor,
        Getter<T, TValue> getter
    )
    {
        return Iso<T, TValue>.Of(getter, constructor);
    }
}
