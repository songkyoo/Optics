namespace Macaron.Optics;

public readonly record struct Lens<T, TValue>(
    Func<T, TValue> Get,
    Func<T, TValue, T> Set
)
{
    #region Static
    public static Lens<T, TValue> Of(Func<T, TValue> getter, Func<T, TValue, T> setter) => new(getter, setter);
    #endregion
}
