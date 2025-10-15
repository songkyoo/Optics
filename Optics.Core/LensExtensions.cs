using Macaron.Functional;

namespace Macaron.Optics;

public static class LensExtensions
{
    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Func<TValue1, Maybe<TValue2>> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = lens.Get(value0);
            var value2 = optionalGetter(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        OptionalGetter<TValue1, TValue2> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = lens.Get(value0);
            var value2 = optionalGetter.Get(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Func<TValue1, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = lens.Get(value0);
            var value2 = getter(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Getter<TValue1, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = lens.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Optional<TValue1, TValue2> optional
    )
    {
        return Optional<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value0 = source;
                var value1 = lens.Get(value0);
                var value2 = optional.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens.Get(value0);

                var newValue1 = optional.Set(value1, value);
                var newValue0 = lens.Set(source, newValue1);

                return newValue0;
            }
        );
    }

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

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Iso<TValue1, TValue2> iso
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens.Get(value0);
                var value2 = iso.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var newValue1 = iso.Construct(value);
                var newValue0 = lens.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue> Transform<T, TValue>(
        this Lens<T, TValue> lens,
        Func<T, TValue, TValue>? mapGet = null,
        Func<T, TValue, TValue>? mapSet = null
    )
    {
        return Lens<T, TValue>.Of(
            getter: source =>
            {
                var value = lens.Get(source);
                return mapGet != null ? mapGet(source, value) : value;
            },
            setter: (source, value) =>
            {
                return lens.Set(source, mapSet != null ? mapSet(source, value) : value);
            }
        );
    }

    public static Lens<T, TValue> Transform<T, TValue>(
        this Lens<T, TValue> lens,
        Func<TValue, TValue>? mapGet = null,
        Func<TValue, TValue>? mapSet = null
    )
    {
        return Lens<T, TValue>.Of(
            getter: source =>
            {
                var value = lens.Get(source);
                return mapGet != null ? mapGet(value) : value;
            },
            setter: (source, value) =>
            {
                return lens.Set(source, mapSet != null ? mapSet(value) : value);
            }
        );
    }

    public static T Modify<T, TValue>(
        this Lens<T, TValue> lens,
        T source,
        Func<T, TValue, TValue> modifier
    )
    {
        var value = lens.Get(source);
        var newValue = modifier(source, value);
        var newSource = lens.Set(source, newValue);

        return newSource;
    }

    public static T Modify<T, TValue>(
        this Lens<T, TValue> lens,
        T source,
        Func<TValue, TValue> modifier
    )
    {
        var value = lens.Get(source);
        var newValue = modifier(value);
        var newSource = lens.Set(source, newValue);

        return newSource;
    }

    public static OptionalGetter<T, TValue> ToOptionalGetter<T, TValue>(this Lens<T, TValue> lens)
    {
        return OptionalGetter<T, TValue>.Of(
            getter: lens.Get
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(this Lens<T, TValue> lens)
    {
        return Getter<T, TValue>.Of(
            getter: lens.Get
        );
    }

    public static Setter<T, TValue> ToSetter<T, TValue>(this Lens<T, TValue> lens)
    {
        return Setter<T, TValue>.Of(
            setter: lens.Set
        );
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(this Lens<T, TValue> lens)
    {
        return Optional<T, TValue>.Of(
            getter: lens.Get,
            setter: lens.Set
        );
    }
}
