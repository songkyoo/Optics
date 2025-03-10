namespace Macaron.Optics;

public static class PrismExtensions
{
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
