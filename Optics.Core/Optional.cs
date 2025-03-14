using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static partial class Optional
{
    public static OptionalOf<T> Of<T>() => new();

    public static Optional<T, TValue> Of<T, TValue>(Func<T, Maybe<TValue>> optionalGetter, Func<T, TValue, T> setter) =>
        new(optionalGetter, setter);

    public static Optional<T, TValue> Of<T, TValue>(
        OptionalGetter<T, TValue> optionalGetter,
        Setter<T, TValue> setter
    ) => new(optionalGetter.Get, setter.Set);

    public static Optional<T, TValue> Of<T, TValue>(Func<T, TValue> getter, Func<T, TValue, T> setter) =>
        new(source => Just(getter(source)), setter);

    public static Optional<T, TValue> Of<T, TValue>(Getter<T, TValue> getter, Setter<T, TValue> setter) =>
        new(source => Just(getter.Get(source)), setter.Set);
}
