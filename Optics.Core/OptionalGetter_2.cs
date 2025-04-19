using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public readonly struct OptionalGetter<T, TValue>(
    Func<T, Maybe<TValue>> get
)
{
    #region Static
    public static OptionalGetter<T, TValue> Of(Func<T, Maybe<TValue>> optionalGetter) => new(optionalGetter);

    public static OptionalGetter<T, TValue> Of(Func<T, TValue> getter) => new(source => Just(getter(source)));

    public static OptionalGetter<T, TValue> Of(Getter<T, TValue> getter) => new(source => Just(getter.Get(source)));
    #endregion

    #region Methods
    public Maybe<TValue> Get(T source) => get.Invoke(source);
    #endregion
}
