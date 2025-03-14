namespace Macaron.Optics;

public static partial class Lens
{
    public static LensOf<T> Of<T>() => new();

    public static Lens<T, TValue> Of<T, TValue>(Func<T, TValue> getter, Func<T, TValue, T> setter) =>
        new(getter, setter);

    public static Lens<T, TValue> Of<T, TValue>(Getter<T, TValue> getter, Setter<T, TValue> setter) =>
        new(getter.Get, setter.Set);
}
