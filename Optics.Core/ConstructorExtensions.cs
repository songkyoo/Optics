using Macaron.Functional;

namespace Macaron.Optics;

public static class ConstructorExtensions
{
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
