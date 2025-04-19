namespace Macaron.Optics;

public readonly struct Iso<T, TValue>(
    Func<T, TValue> get,
    Func<TValue, T> construct
)
{
    #region Static
    public static Iso<T, TValue> Of(Func<T, TValue> getter, Func<TValue, T> constructor) => new(getter, constructor);

    public static Iso<T, TValue> Of(Getter<T, TValue> getter, Constructor<T, TValue> constructor) =>
        new(getter.Get, constructor.Construct);
    #endregion

    #region Methods
    public TValue Get(T source) => get.Invoke(source);

    public T Construct(TValue value) => construct.Invoke(value);
    #endregion
}
