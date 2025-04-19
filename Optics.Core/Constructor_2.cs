namespace Macaron.Optics;

public readonly struct Constructor<T, TValue>(
    Func<TValue, T> construct
)
{
    #region Static
    public static Constructor<T, TValue> Of(Func<TValue, T> constructor) => new(constructor);
    #endregion

    #region Methods
    public T Construct(TValue value) => construct.Invoke(value);
    #endregion
}
