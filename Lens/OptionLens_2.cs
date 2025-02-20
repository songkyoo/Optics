namespace Macaron.Optics;

public readonly record struct OptionLens<T, TValue>(
    Func<T, Option<TValue>> Get,
    Func<T, TValue, T> Set
)
{
    #region Static
    public static OptionLens<T, TValue> Of(Func<T, Option<TValue>> getter, Func<T, TValue, T> setter)
        => new(getter, setter);
    #endregion
}
