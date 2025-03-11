using Macaron.Functional;

namespace Macaron.Optics;

public static class PrismExtensions
{
    public static Prism<T, TValue> Transform<T, TValue>(
        this Prism<T, TValue> prism,
        Func<T, TValue, Maybe<TValue>>? mapGet = null,
        Func<TValue, T, T>? mapConstruct = null
    )
    {
        return Prism<T, TValue>.Of(
            optionalGetter: source =>
            {
                var value = prism.Get(source);
                if (value.IsNothing)
                {
                    return value;
                }

                return mapGet != null ? mapGet.Invoke(source, value.Value) : value;
            },
            constructor: value =>
            {
                var source = prism.Construct(value);
                return mapConstruct != null ? mapConstruct.Invoke(value, source) : source;
            }
        );
    }

    public static Prism<T, TValue> Transform<T, TValue>(
        this Prism<T, TValue> prism,
        Func<TValue, Maybe<TValue>>? mapGet = null,
        Func<T, T>? mapConstruct = null
    )
    {
        return Prism<T, TValue>.Of(
            optionalGetter: source =>
            {
                var value = prism.Get(source);
                if (value.IsNothing)
                {
                    return value;
                }

                return mapGet != null ? mapGet.Invoke(value.Value) : value;
            },
            constructor: value =>
            {
                var source = prism.Construct(value);
                return mapConstruct != null ? mapConstruct.Invoke(source) : source;
            }
        );
    }

    public static Constructor<T, TValue> ToConstructor<T, TValue>(
        this Prism<T, TValue> prism
    )
    {
        return Constructor<T, TValue>.Of(prism.Construct);
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Prism<T, TValue> prism,
        Func<T, TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            source => prism.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(source)
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Prism<T, TValue> prism,
        Func<TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            source => prism.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue()
        );
    }

    public static OptionalGetter<T, TValue> ToOptionalGetter<T, TValue>(
        this Prism<T, TValue> prism
    )
    {
        return OptionalGetter<T, TValue>.Of(prism.Get);
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Prism<T, TValue> prism,
        Func<T, TValue> getDefaultValue
    )
    {
        return Iso<T, TValue>.Of(
            source => prism.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(source),
            prism.Construct
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Prism<T, TValue> prism,
        Func<TValue> getDefaultValue
    )
    {
        return Iso<T, TValue>.Of(
            source => prism.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(),
            prism.Construct
        );
    }
}
