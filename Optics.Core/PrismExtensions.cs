using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class PrismExtensions
{
    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Func<Maybe<TValue1>, Maybe<TValue2>> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = prism.Get(value0);
            var value2 = optionalGetter.Invoke(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        OptionalGetter<Maybe<TValue1>, TValue2> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = prism.Get(value0);
            var value2 = optionalGetter.Get(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Func<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = prism.Get(value0);
            var value2 = getter.Invoke(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Getter<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = prism.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static Constructor<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Func<TValue2, TValue1> constructor
    )
    {
        return Constructor<T, TValue2>.Of(value =>
        {
            var newValue1 = constructor.Invoke(value);
            var newValue0 = prism.Construct(newValue1);

            return newValue0;
        });
    }

    public static Constructor<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Constructor<TValue1, TValue2> constructor
    )
    {
        return Constructor<T, TValue2>.Of(value =>
        {
            var newValue1 = constructor.Construct(value);
            var newValue0 = prism.Construct(newValue1);

            return newValue0;
        });
    }

    public static Prism<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism1,
        Prism<TValue1, TValue2> prism2
    )
    {
        return Prism<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value0 = source;
                var value1 = prism1.Get(value0);
                var value2 = value1 is { IsJust: true } ? prism2.Get(value1.Value) : Nothing();

                return value2;
            },
            constructor: value =>
            {
                var value0 = value;
                var value1 = prism2.Construct(value0);
                var value2 = prism1.Construct(value1);

                return value2;
            }
        );
    }

    public static Iso<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Iso<TValue1, TValue2> iso,
        Func<T, TValue1> getDefaultValue
    )
    {
        return Iso<T, TValue2>.Of(
            source =>
            {
                var value0 = source;
                var value1 = prism.Get(value0) is { IsJust: true } just ? just.Value : getDefaultValue(value0);
                var value2 = iso.Get(value1);

                return value2;
            },
            value =>
            {
                var value0 = value;
                var value1 = iso.Construct(value0);
                var value2 = prism.Construct(value1);

                return value2;
            }
        );
    }

    public static Iso<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Iso<TValue1, TValue2> iso,
        Func<TValue1> getDefaultValue
    )
    {
        return Iso<T, TValue2>.Of(
            source =>
            {
                var value0 = source;
                var value1 = prism.Get(value0) is { IsJust: true } just ? just.Value : getDefaultValue();
                var value2 = iso.Get(value1);

                return value2;
            },
            value =>
            {
                var value0 = value;
                var value1 = iso.Construct(value0);
                var value2 = prism.Construct(value1);

                return value2;
            }
        );
    }

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

    public static OptionalGetter<T, TValue> ToOptionalGetter<T, TValue>(
        this Prism<T, TValue> prism
    )
    {
        return OptionalGetter<T, TValue>.Of(prism.Get);
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

    public static Constructor<T, TValue> ToConstructor<T, TValue>(
        this Prism<T, TValue> prism
    )
    {
        return Constructor<T, TValue>.Of(prism.Construct);
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
