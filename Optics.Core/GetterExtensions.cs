using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static partial class GetterExtensions
{
    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter1,
        Getter<TValue1, TValue2> getter2
    )
    {
        return Getter.Of<T, TValue2>(source =>
        {
            var value0 = source;
            var value1 = getter1.Get(value0);
            var value2 = getter2.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Getter<T, TValue1> getter,
        OptionalGetter<TValue1, TValue2> optionalGetter
    )
    {
        return OptionalGetter.Of<T, TValue2>(source =>
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
        return OptionalGetter.Of<T, TValue2>(source =>
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
        return OptionalGetter.Of<T, TValue2>(source =>
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
        return Getter.Of<T, TValue2>(source =>
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
        return Getter.Of<T, TValue2>(source =>
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
        return OptionalGetter.Of<T, TValue>(source =>
        {
            var value0 = source;
            var value1 = Just(getter.Get(value0));

            return value1;
        });
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this Getter<T, TValue> getter,
        Func<T, TValue, T> setter
    )
    {
        return Optional<T, TValue>.Of(
            getter: getter,
            setter: Setter.Of(setter)
        );
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this Getter<T, TValue> getter,
        Setter<T, TValue> setter
    )
    {
        return Optional<T, TValue>.Of(
            optionalGetter: OptionalGetter.Of(getter),
            setter: setter
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
