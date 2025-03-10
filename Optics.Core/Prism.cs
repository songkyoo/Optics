using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class Prism
{
    #region Static
    public static Prism<T, TValue> Of<T, TValue>(Func<T, Maybe<TValue>> optionalGetter, Func<TValue, T> constructor) =>
        Prism<T, TValue>.Of(optionalGetter, constructor);

    public static Prism<T, TValue> Of<T, TValue>(
        OptionalGetter<T, TValue> optionalGetter,
        Constructor<T, TValue> constructor
    ) => new(optionalGetter.Get, constructor.Construct);

    public static Prism<T, TValue> Of<T, TValue>(Func<T, TValue> getter, Func<TValue, T> constructor) =>
        new(source => Just(getter.Invoke(source)), constructor);

    public static Prism<T, TValue> Of<T, TValue>(Getter<T, TValue> getter, Constructor<T, TValue> constructor) =>
        new(source => Just(getter.Get(source)), constructor.Construct);
    #endregion
}
