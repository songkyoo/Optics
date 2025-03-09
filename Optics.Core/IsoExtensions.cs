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
        Getter<TValue1, TValue2> getter
    )
    {
        return Getter.Of<T, TValue2>(source =>
        {
            var value0 = source;
            var value1 = iso.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Iso<T, TValue1> iso,
        OptionalGetter<TValue1, TValue2> optionalGetter
    )
    {
        return OptionalGetter.Of<T, TValue2>(source =>
        {
            var value0 = source;
            var value1 = iso.Get(value0);
            var value2 = optionalGetter.Get(value1);

            return value2;
        });
    }
}
