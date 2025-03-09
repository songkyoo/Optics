using Macaron.Functional;

namespace Macaron.Optics;

public static class Prism
{
    #region Static
    public static Prism<T, TValue> Of<T, TValue>(Func<T, Maybe<TValue>> getter, Func<TValue, T> constructor) =>
        Prism<T, TValue>.Of(getter, constructor);
    #endregion
}
