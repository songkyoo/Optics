namespace Macaron.Optics;

public static class SetterExtensions
{
    public static Lens<T, TValue> ToLens<T, TValue>(
        this Setter<T, TValue> setter,
        Func<T, TValue> getter
    )
    {
        return Lens<T, TValue>.Of(getter, setter.Set);
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this Setter<T, TValue> setter,
        Getter<T, TValue> getter
    )
    {
        return Lens<T, TValue>.Of(getter.Get, setter.Set);
    }
}
