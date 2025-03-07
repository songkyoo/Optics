namespace Macaron.Optics;

public static class Setter
{
    #region Static
    public static Setter<T, TValue> Of<T, TValue>(Func<T, TValue, T> setter) => new(setter);
    #endregion
}
