using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class OptionalGetterExtensions
{
    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter1,
        Func<Maybe<TValue1>, Maybe<TValue2>> optionalGetter2
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = optionalGetter1.Get(value0);
            var value2 = optionalGetter2(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter1,
        OptionalGetter<Maybe<TValue1>, TValue2> optionalGetter2
    )
    {
        return OptionalGetter.Of<T, TValue2>(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = optionalGetter1.Get(value0);
            var value2 = optionalGetter2.Get(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter,
        Func<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = optionalGetter.Get(value0);
            var value2 = getter(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter,
        Getter<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = optionalGetter.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter,
        Optional<TValue1, TValue2> optional
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = optionalGetter.Get(value0);
            var value2 = value1 is { IsJust: true } ? optional.Get(value1.Value) : Nothing();

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter,
        Lens<TValue1, TValue2> lens
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = optionalGetter.Get(value0);
            var value2 = value1 is { IsJust: true } ? Just(lens.Get(value1.Value)) : Nothing();

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter,
        Prism<TValue1, TValue2> prism
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = optionalGetter.Get(value0);
            var value2 = value1 is { IsJust: true } ? prism.Get(value1.Value) : Nothing();

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter,
        Iso<TValue1, TValue2> iso
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = optionalGetter.Get(value0);
            var value2 = value1 is { IsJust: true } ? Just(iso.Get(value1.Value)) : Nothing();

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Transform<T, TValue, TValue2>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<T, TValue, TValue2> mapGet
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value1 = optionalGetter.Get(source);
            var value2 = value1.IsJust ? Just(mapGet(source, value1.Value)) : Nothing();

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Transform<T, TValue, TValue2>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<TValue, TValue2> mapGet
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value1 = optionalGetter.Get(source);
            var value2 = value1.IsJust ? Just(mapGet(value1.Value)) : Nothing();

            return value2;
        });
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this OptionalGetter<T, TValue> optional,
        Func<T, TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            getter: source => optional.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(source)
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this OptionalGetter<T, TValue> optional,
        Func<TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            getter: source => optional.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue()
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this OptionalGetter<T, TValue> optional,
        TValue defaultValue
    )
    {
        return Getter<T, TValue>.Of(
            getter: source => optional.Get(source) is { IsJust: true } just ? just.Value : defaultValue
        );
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<T, TValue, T> setter
    )
    {
        return Optional<T, TValue>.Of(
            optionalGetter: optionalGetter.Get,
            setter: setter
        );
    }

    public static Optional<T, TValue> ToOptional<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Setter<T, TValue> setter
    )
    {
        return Optional<T, TValue>.Of(
            optionalGetter: optionalGetter,
            setter: setter
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<T, TValue, T> setter,
        Func<T, TValue> getDefaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : getDefaultValue(source),
            setter: setter
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<T, TValue, T> setter,
        Func<TValue> getDefaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : getDefaultValue(),
            setter: setter
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<T, TValue, T> setter,
        TValue defaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : defaultValue,
            setter: setter
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Setter<T, TValue> setter,
        Func<T, TValue> getDefaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : getDefaultValue(source),
            setter: setter.Set
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Setter<T, TValue> setter,
        Func<TValue> getDefaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : getDefaultValue(),
            setter: setter.Set
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Setter<T, TValue> setter,
        TValue defaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : defaultValue,
            setter: setter.Set
        );
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<TValue, T> constructor
    )
    {
        return Prism<T, TValue>.Of(
            optionalGetter: optionalGetter.Get,
            constructor: constructor
        );
    }

    public static Prism<T, TValue> ToPrism<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Constructor<T, TValue> constructor
    )
    {
        return Prism<T, TValue>.Of(
            optionalGetter: optionalGetter,
            constructor: constructor
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<TValue, T> constructor,
        Func<T, TValue> getDefaultValue
    )
    {
        return Iso<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : getDefaultValue(source),
            constructor: constructor
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<TValue, T> constructor,
        Func<TValue> getDefaultValue
    )
    {
        return Iso<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : getDefaultValue(),
            constructor: constructor
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Func<TValue, T> constructor,
        TValue defaultValue
    )
    {
        return Iso<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : defaultValue,
            constructor: constructor
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Constructor<T, TValue> constructor,
        Func<T, TValue> getDefaultValue
    )
    {
        return Iso<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : getDefaultValue(source),
            constructor: constructor.Construct
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Constructor<T, TValue> constructor,
        Func<TValue> getDefaultValue
    )
    {
        return Iso<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : getDefaultValue(),
            constructor: constructor.Construct
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this OptionalGetter<T, TValue> optionalGetter,
        Constructor<T, TValue> constructor,
        TValue defaultValue
    )
    {
        return Iso<T, TValue>.Of(
            getter: source => optionalGetter.Get(source) is { IsJust: true } just
                ? just.Value
                : defaultValue,
            constructor: constructor.Construct
        );
    }
}
