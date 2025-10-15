using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public readonly struct Optional<T, TValue>(
    Func<T, Maybe<TValue>> get,
    Func<T, TValue, T> set
)
{
    #region Static
    public static Optional<T, TValue> Of(Func<T, Maybe<TValue>> optionalGetter, Func<T, TValue, T> setter) =>
        new(optionalGetter, setter);

    public static Optional<T, TValue> Of(OptionalGetter<T, TValue> optionalGetter, Setter<T, TValue> setter) =>
        new(optionalGetter.Get, setter.Set);

    public static Optional<T, TValue> Of(Func<T, TValue> getter, Func<T, TValue, T> setter) =>
        new(source => Just(getter(source)), setter);

    public static Optional<T, TValue> Of(Getter<T, TValue> getter, Setter<T, TValue> setter) =>
        new(source => Just(getter.Get(source)), setter.Set);
    #endregion

    #region Methods
    public Maybe<TValue> Get(T source) => get(source);

    public T Set(T source, TValue value) => set(source, value);
    #endregion
}
