namespace Macaron.Optics;

/// <summary>
/// <typeparamref name="T"/> 타입으로부터 <typeparamref name="TValue"/> 타입인 멤버의 값을 획득하거나 설정한다.
/// <see cref="Lens{T, TValue"/>와 다르게 값의 획득에 대해서 <see cref="Option{TValue}"/>를 반환한다.
/// </summary>
/// <typeparam name="T">대상 타입.</typeparam>
/// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 값을 획득하거나 설정할 멤버의 타입.</typeparam>
/// <param name="Get">값을 획득하는 함수.</param>
/// <param name="Set">값을 설정하는 함수.</param>
public readonly record struct OptionLens<T, TValue>(
    Func<T, Option<TValue>> Get,
    Func<T, TValue, T> Set
)
{
    #region Static
    /// <summary>
    /// 지정된 <paramref name="getter"/>와 <paramref name="setter"/>로 <see cref="OptionLens{T, TValue}"/> 인스턴스를
    /// 생성한다.
    /// </summary>
    /// <param name="getter">값을 획득하는 함수.</param>
    /// <param name="setter">값을 설정하는 함수.</param>
    /// <returns><see cref="OptionLens{T, TValue}"/> 인스턴스.</returns>
    public static OptionLens<T, TValue> Of(Func<T, Option<TValue>> getter, Func<T, TValue, T> setter)
        => new(getter, setter);
    #endregion
}
