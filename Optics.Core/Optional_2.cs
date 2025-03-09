using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

/// <summary>
/// <typeparamref name="T"/> 타입으로부터 <typeparamref name="TValue"/> 타입인 멤버의 값을 획득하거나 설정한다.
/// <see cref="Lens{T, TValue"/>와 다르게 값의 획득에 대해서 <see cref="Maybe{TValue}"/>를 반환한다.
/// </summary>
/// <typeparam name="T">대상 타입.</typeparam>
/// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 값을 획득하거나 설정할 멤버의 타입.</typeparam>
/// <param name="Get">값을 획득하는 함수.</param>
/// <param name="Set">값을 설정하는 함수.</param>
public readonly record struct Optional<T, TValue>(
    Func<T, Maybe<TValue>> Get,
    Func<T, TValue, T> Set
)
{
    #region Static
    /// <summary>
    /// 지정된 <paramref name="optionalGetter"/>와 <paramref name="setter"/>로 <see cref="Optional{T,TValue}"/> 인스턴스를
    /// 생성한다.
    /// </summary>
    /// <param name="optionalGetter">값을 획득하는 함수.</param>
    /// <param name="setter">값을 설정하는 함수.</param>
    /// <returns><see cref="Optional{T,TValue}"/> 인스턴스.</returns>
    public static Optional<T, TValue> Of(Func<T, Maybe<TValue>> optionalGetter, Func<T, TValue, T> setter) =>
        new(optionalGetter, setter);

    public static Optional<T, TValue> Of(OptionalGetter<T, TValue> optionalGetter, Setter<T, TValue> setter) => new(
        optionalGetter.Get,
        setter.Set
    );

    public static Optional<T, TValue> Of(Func<T, TValue> getter, Func<T, TValue, T> setter)
    {
        return new(source => Just(getter(source)), setter);
    }

    public static Optional<T, TValue> Of(Getter<T, TValue> getter, Setter<T, TValue> setter)
    {
        return new(source => Just(getter.Get(source)), setter.Set);
    }
    #endregion
}
