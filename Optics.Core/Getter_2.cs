namespace Macaron.Optics;

public readonly record struct Getter<T, TValue>(
    Func<T, TValue> Get
)
{
    #region Static
    public static Getter<T, TValue> Of(Func<T, TValue> getter) => new(getter);
    #endregion
}
