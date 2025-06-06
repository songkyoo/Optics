namespace Macaron.Optics;

public static partial class Constructor
{
    #region Static
    public static Constructor<T, TValue> Of<T, TValue>(Func<TValue, T> constructor) => new(constructor);
    #endregion
}
