namespace Macaron.Optics;

public static class Getter
{
    #region Static
    public static Getter<T, TValue> Of<T, TValue>(Func<T, TValue> getter) => new(getter);
    #endregion
}
