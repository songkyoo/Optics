using Macaron.Functional;

namespace Macaron.Optics;

public static class IsoExtensions
{
    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Func<TValue1, Maybe<TValue2>> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = iso.Get(value0);
            var value2 = optionalGetter.Invoke(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        OptionalGetter<TValue1, TValue2> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = iso.Get(value0);
            var value2 = optionalGetter.Get(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Func<TValue1, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = iso.Get(value0);
            var value2 = getter.Invoke(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Getter<TValue1, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = iso.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static Constructor<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Func<TValue2, TValue1> constructor
    )
    {
        return Constructor<T, TValue2>.Of(constructor: value =>
        {
            var newValue1 = constructor.Invoke(value);
            var newValue0 = iso.Construct(newValue1);

            return newValue0;
        });
    }

    public static Constructor<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Constructor<TValue1, TValue2> constructor
    )
    {
        return Constructor<T, TValue2>.Of(constructor: value =>
        {
            var newValue1 = constructor.Construct(value);
            var newValue0 = iso.Construct(newValue1);

            return newValue0;
        });
    }

    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Optional<TValue1, TValue2> optional
    )
    {
        return Optional<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value0 = source;
                var value1 = iso.Get(value0);
                var value2 = optional.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = iso.Get(value0);

                var newValue1 = optional.Set(value1, value);
                var newValue0 = iso.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Lens<TValue1, TValue2> lens
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = iso.Get(value0);
                var value2 = lens.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = iso.Get(value0);

                var newValue1 = lens.Set(value1, value);
                var newValue0 = iso.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Prism<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Prism<TValue1, TValue2> prism
    )
    {
        return Prism<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value0 = source;
                var value1 = iso.Get(value0);
                var value2 = prism.Get(value1);

                return value2;
            },
            constructor: value =>
            {
                var newValue1 = prism.Construct(value);
                var newValue0 = iso.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Iso<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso1,
        Iso<TValue1, TValue2> iso2
    )
    {
        return Iso<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = iso1.Get(value0);
                var value2 = iso2.Get(value1);

                return value2;
            },
            constructor: value =>
            {
                var newValue1 = iso2.Construct(value);
                var newValue0 = iso1.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Iso<T, TValue> Transform<T, TValue>(
        this Iso<T, TValue> iso,
        Func<T, TValue, TValue>? mapGet = null,
        Func<TValue, T, T>? mapConstruct = null
    )
    {
        return Iso<T, TValue>.Of(
            getter: source =>
            {
                var value = iso.Get(source);
                return mapGet != null ? mapGet.Invoke(source, value) : value;
            },
            constructor: value =>
            {
                var source = iso.Construct(value);
                return mapConstruct != null ? mapConstruct.Invoke(value, source) : source;
            }
        );
    }

    public static Iso<T, TValue> Transform<T, TValue>(
        this Iso<T, TValue> iso,
        Func<TValue, TValue>? mapGet = null,
        Func<T, T>? mapConstruct = null
    )
    {
        return Iso<T, TValue>.Of(
            getter: source =>
            {
                var value = iso.Get(source);
                return mapGet != null ? mapGet.Invoke(value) : value;
            },
            constructor: value =>
            {
                var source = iso.Construct(value);
                return mapConstruct != null ? mapConstruct.Invoke(source) : source;
            }
        );
    }

    public static OptionalGetter<T, TValue> ToOptionalGetter<T, TValue>(
        this Iso<T, TValue> iso
    )
    {
        return OptionalGetter<T, TValue>.Of(
            getter: iso.Get
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Iso<T, TValue> iso
    )
    {
        return Getter<T, TValue>.Of(
            getter: iso.Get
        );
    }

    public static Constructor<T, TValue> ToConstructor<T, TValue>(
        this Iso<T, TValue> iso
    )
    {
        return Constructor<T, TValue>.Of(
            constructor: iso.Construct
        );
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Iso<T, TValue> iso
    )
    {
        return Prism<T, TValue>.Of(
            getter: iso.Get,
            constructor: iso.Construct
        );
    }
}
