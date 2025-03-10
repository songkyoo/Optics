using Macaron.Functional;

namespace Macaron.Optics;

public static class IsoExtensions
{
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

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Func<TValue1, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(source =>
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
        return Getter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = iso.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        Func<TValue1, Maybe<TValue2>> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(source =>
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
        return OptionalGetter<T, TValue2>.Of(source =>
        {
            var value0 = source;
            var value1 = iso.Get(value0);
            var value2 = optionalGetter.Get(value1);

            return value2;
        });
    }

    public static Constructor<T, TValue> ToConstructor<T, TValue>(
        this Iso<T, TValue> iso
    )
    {
        return Constructor<T, TValue>.Of(iso.Construct);
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Iso<T, TValue> iso
    )
    {
        return Getter<T, TValue>.Of(iso.Get);
    }

    public static OptionalGetter<T, TValue> ToOptionalGetter<T, TValue>(
        this Iso<T, TValue> iso
    )
    {
        return OptionalGetter<T, TValue>.Of(iso.Get);
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
