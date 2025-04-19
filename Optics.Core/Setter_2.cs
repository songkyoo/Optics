namespace Macaron.Optics;

public readonly struct Setter<T, TValue>(
    Func<T, TValue, T> set
)
{
    #region Static
    public static Setter<T, TValue> Of(Func<T, TValue, T> setter) => new(setter);
    #endregion

    #region Methods
    public T Set(T source, TValue value) => set.Invoke(source, value);
    #endregion
}
