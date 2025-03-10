namespace Macaron.Optics;

public readonly record struct Setter<T, TValue>(
    Func<T, TValue, T> Set
)
{
    #region Static
    public static Setter<T, TValue> Of(Func<T, TValue, T> setter) => new(setter);

    public static Setter<T, TValue> Of(Func<TValue, T> constructor) => new((_, value) => constructor.Invoke(value));

    public static Setter<T, TValue> Of(Constructor<T, TValue> constructor) =>
        new((_, value) => constructor.Construct(value));
    #endregion
}
