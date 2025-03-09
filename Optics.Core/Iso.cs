namespace Macaron.Optics;

public static partial class Iso
{
    #region Static
    public static Iso<T, TValue> Of<T, TValue>(Func<T, TValue> getter, Func<TValue, T> constructor) =>
        new(getter, constructor);

    public static Iso<T, TValue> Of<T, TValue>(Getter<T, TValue> getter, Constructor<T, TValue> constructor) => new(
        getter.Get,
        constructor.Construct
    );
    #endregion
}
