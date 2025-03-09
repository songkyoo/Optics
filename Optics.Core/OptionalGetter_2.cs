using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public readonly record struct OptionalGetter<T, TValue>(
    Func<T, Maybe<TValue>> Get
)
{
    #region Static
    public static OptionalGetter<T, TValue> Of(Func<T, Maybe<TValue>> optionalGetter) => new(optionalGetter);

    public static OptionalGetter<T, TValue> Of(Getter<T, TValue> getter) => new(source => Just(getter.Get(source)));

    public static OptionalGetter<T, TValue> Of(Func<T, TValue> getter) => new(source => Just(getter(source)));
    #endregion
}
