namespace Macaron.Optics;

public readonly struct Lens<T, TValue>(
    Func<T, TValue> get,
    Func<T, TValue, T> set
)
{
    #region Static
    public static Lens<T, TValue> Of(Func<T, TValue> getter, Func<T, TValue, T> setter) => new(getter, setter);

    public static Lens<T, TValue> Of(Getter<T, TValue> getter, Setter<T, TValue> setter) =>
        new(getter.Get, setter.Set);
    #endregion

    #region Methods
    public TValue Get(T source) => get.Invoke(source);

    public T Set(T source, TValue value) => set.Invoke(source, value);
    #endregion
}
