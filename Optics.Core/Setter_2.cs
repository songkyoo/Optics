namespace Macaron.Optics;

public readonly record struct Setter<T, TValue>(
    Func<T, TValue, T> Set
)
{
    #region Static
    public static Setter<T, TValue> Of(Func<T, TValue, T> setter) => new(setter);
    #endregion
}
