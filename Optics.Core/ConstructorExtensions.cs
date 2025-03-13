using Macaron.Functional;

namespace Macaron.Optics;

public static class ConstructorExtensions
{
    public static Constructor<T, TValue2> Compose<T, TValue1, TValue2>(
        this Constructor<T, TValue1> constructor1,
        Func<TValue2, TValue1> constructor2
    )
    {
        return Constructor<T, TValue2>.Of(constructor: value =>
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
        return Constructor<T, TValue2>.Of(constructor: value =>
        {
            var newValue1 = constructor2.Construct(value);
            var newValue0 = constructor1.Construct(newValue1);

            return newValue0;
        });
    }

    public static Constructor<TResult, TValue> Transform<T, TValue, TResult>(
        this Constructor<T, TValue> constructor,
        Func<TValue, T, TResult> mapConstruct
    )
    {
        return Constructor<TResult, TValue>.Of(constructor: value =>
        {
            var newValue1 = constructor.Construct(value);
            var newValue0 = mapConstruct(value, newValue1);

            return newValue0;
        });
    }

    public static Constructor<TResult, TValue> Transform<T, TValue, TResult>(
        this Constructor<T, TValue> constructor,
        Func<T, TResult> mapConstruct
    )
    {
        return Constructor<TResult, TValue>.Of(constructor: value =>
        {
            var newValue1 = constructor.Construct(value);
            var newValue0 = mapConstruct(newValue1);

            return newValue0;
        });
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Constructor<T, TValue> constructor,
        Func<T, TValue> getter
    )
    {
        return Prism<T, TValue>.Of(
            getter: getter,
            constructor: constructor.Construct
        );
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Constructor<T, TValue> constructor,
        Getter<T, TValue> getter
    )
    {
        return Prism<T, TValue>.Of(
            getter: getter,
            constructor: constructor
        );
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Constructor<T, TValue> constructor,
        Func<T, Maybe<TValue>> optionalGetter
    )
    {
        return Prism<T, TValue>.Of(
            optionalGetter: optionalGetter,
            constructor: constructor.Construct
        );
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Constructor<T, TValue> constructor,
        OptionalGetter<T, TValue> optionalGetter
    )
    {
        return Prism<T, TValue>.Of(
            optionalGetter: optionalGetter,
            constructor: constructor
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Constructor<T, TValue> constructor,
        Func<T, TValue> getter
    )
    {
        return Iso<T, TValue>.Of(
            getter: getter,
            constructor: constructor.Construct
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Constructor<T, TValue> constructor,
        Getter<T, TValue> getter
    )
    {
        return Iso<T, TValue>.Of(
            getter: getter,
            constructor: constructor
        );
    }
}
