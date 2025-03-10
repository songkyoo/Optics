using Macaron.Functional;

namespace Macaron.Optics;

public static class GetterExtensions
{
    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter1,
        Func<TValue1, TValue2> getter2
    )
    {
        return Getter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = getter1.Get(value0);
            var value2 = getter2.Invoke(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter1,
        Getter<TValue1, TValue2> getter2
    )
    {
        return Getter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = getter1.Get(value0);
            var value2 = getter2.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter,
        Func<TValue1, Maybe<TValue2>> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = getter.Get(value0);
            var value2 = optionalGetter.Invoke(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter,
        OptionalGetter<TValue1, TValue2> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = getter.Get(value0);
            var value2 = optionalGetter.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter,
        Optional<TValue1, TValue2> optional
    )
    {
        return OptionalGetter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = getter.Get(value0);
            var value2 = optional.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter,
        Prism<TValue1, TValue2> prism
    )
    {
        return OptionalGetter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = getter.Get(value0);
            var value2 = prism.Get(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter,
        Lens<TValue1, TValue2> lens
    )
    {
        return Getter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = getter.Get(value0);
            var value2 = lens.Get(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter,
        Iso<TValue1, TValue2> iso
    )
    {
        return Getter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = getter.Get(value0);
            var value2 = iso.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue> ToOptionalGetter<T, TValue>(
        this Getter<T, TValue> getter
    )
    {
        return OptionalGetter<T, TValue>.Of(getter);
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this Getter<T, TValue> getter,
        Func<T, TValue, T> setter
    )
    {
        return Optional<T, TValue>.Of(
            getter: getter.Get,
            setter: setter
        );
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this Getter<T, TValue> getter,
        Setter<T, TValue> setter
    )
    {
        return Optional<T, TValue>.Of(
            getter: getter,
            setter: setter
        );
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Getter<T, TValue> getter,
        Func<TValue, T> constructor
    )
    {
        return Prism<T, TValue>.Of(
            getter: getter.Get,
            constructor: constructor
        );
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this Getter<T, TValue> getter,
        Constructor<T, TValue> constructor
    )
    {
        return Prism<T, TValue>.Of(
            getter: getter,
            constructor: constructor
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this Getter<T, TValue> getter,
        Func<T, TValue, T> setter
    )
    {
        return Lens<T, TValue>.Of(
            getter: getter.Get,
            setter: setter
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this Getter<T, TValue> getter,
        Setter<T, TValue> setter
    )
    {
        return Lens<T, TValue>.Of(
            getter: getter,
            setter: setter
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Getter<T, TValue> getter,
        Func<TValue, T> constructor
    )
    {
        return Iso<T, TValue>.Of(
            getter: getter.Get,
            constructor: constructor
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Getter<T, TValue> getter,
        Constructor<T, TValue> constructor
    )
    {
        return Iso<T, TValue>.Of(
            getter: getter,
            constructor: constructor
        );
    }
}
