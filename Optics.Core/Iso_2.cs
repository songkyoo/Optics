namespace Macaron.Optics;

public readonly record struct Iso<T, TValue>(
    Func<T, TValue> Get,
    Func<TValue, T> Construct
)
{
    #region Static
    public static Iso<T, TValue> Of(Func<T, TValue> getter, Func<TValue, T> constructor) => new(getter, constructor);

    public static Iso<T, TValue> Of(Getter<T, TValue> getter, Constructor<T, TValue> constructor) => new(
        getter.Get,
        constructor.Construct
    );
    #endregion
}
