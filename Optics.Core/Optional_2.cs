using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public readonly record struct Optional<T, TValue>(
    Func<T, Maybe<TValue>> Get,
    Func<T, TValue, T> Set
)
{
    #region Static
    public static Optional<T, TValue> Of(Func<T, Maybe<TValue>> optionalGetter, Func<T, TValue, T> setter) =>
        new(optionalGetter, setter);

    public static Optional<T, TValue> Of(OptionalGetter<T, TValue> optionalGetter, Setter<T, TValue> setter) =>
        new(optionalGetter.Get, setter.Set);

    public static Optional<T, TValue> Of(Func<T, TValue> getter, Func<T, TValue, T> setter) =>
        new(source => Just(getter.Invoke(source)), setter);

    public static Optional<T, TValue> Of(Getter<T, TValue> getter, Setter<T, TValue> setter) =>
        new(source => Just(getter.Get(source)), setter.Set);
    #endregion
}
