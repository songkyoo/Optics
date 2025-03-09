namespace Macaron.Optics;

public readonly record struct Constructor<T, TValue>(
    Func<TValue, T> Construct
)
{
    #region Static
    public static Constructor<T, TValue> Of(Func<TValue, T> constructor) => new(constructor);
    #endregion
}
