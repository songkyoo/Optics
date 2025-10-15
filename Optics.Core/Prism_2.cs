using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public readonly struct Prism<T, TValue>(
    Func<T, Maybe<TValue>> get,
    Func<TValue, T> construct
)
{
    #region Static
    public static Prism<T, TValue> Of(Func<T, Maybe<TValue>> optionalGetter, Func<TValue, T> constructor) =>
        new(optionalGetter, constructor);

    public static Prism<T, TValue> Of(OptionalGetter<T, TValue> optionalGetter, Constructor<T, TValue> constructor) =>
        new(optionalGetter.Get, constructor.Construct);

    public static Prism<T, TValue> Of(Func<T, TValue> getter, Func<TValue, T> constructor) =>
        new(source => Just(getter(source)), constructor);

    public static Prism<T, TValue> Of(Getter<T, TValue> getter, Constructor<T, TValue> constructor) =>
        new(source => Just(getter.Get(source)), constructor.Construct);
    #endregion

    #region Methods
    public Maybe<TValue> Get(T source) => get(source);

    public T Construct(TValue value) => construct(value);
    #endregion
}
