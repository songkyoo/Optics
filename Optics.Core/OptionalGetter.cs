using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class OptionalGetter
{
    #region Static
    public static OptionalGetter<T, TValue> Of<T, TValue>(Func<T, Maybe<TValue>> optionalGetter) => new(optionalGetter);

    public static OptionalGetter<T, TValue> Of<T, TValue>(Func<T, TValue> getter) =>
        new(source => Just(getter(source)));

    public static OptionalGetter<T, TValue> Of<T, TValue>(Getter<T, TValue> getter) =>
        new(source => Just(getter.Get(source)));
    #endregion
}
