namespace Macaron.Optics;

public readonly struct Getter<T, TValue>(
    Func<T, TValue> get
)
{
    #region Static
    public static Getter<T, TValue> Of(Func<T, TValue> getter) => new(getter);
    #endregion

    #region Methods
    public TValue Get(T source) => get.Invoke(source);
    #endregion
}
