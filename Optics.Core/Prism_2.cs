using Macaron.Functional;

namespace Macaron.Optics;

public readonly record struct Prism<T, TValue>(
    Func<T, Maybe<TValue>> Get,
    Func<TValue, T> Construct
)
{
    #region Static
    public static Prism<T, TValue> Of(Func<T, Maybe<TValue>> getter, Func<TValue, T> constructor) =>
        new(getter, constructor);
    #endregion
}
