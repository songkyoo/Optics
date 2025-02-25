using Macaron.Functional;

namespace Macaron.Optics;

public static class Optional
{
    /// <summary>
    /// <see cref="OptionalOf{T}"/> 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T">
    /// <see cref="Optional{T,TValue}"/>가 사용할 타입. 실제 생성된 Optional에서는 <see cref="Maybe{T}"/>로 치환된다.
    /// </typeparam>
    /// <returns><see cref="OptionalOf{T}"/> 인스턴스.</returns>
    /// <remarks>
    /// 이 메서드에 지정된 <typeparamref name="T"/> 타입에서 <c>with</c> 문을 적용 가능한 멤버에 대해
    /// <c>Lens&lt;T, TValue&gt;</c>, <c>Optional&lt;Option&lt;T&gt;, TValue&gt;</c>를 반환하는 확장 메서드가 생성된다.
    /// </remarks>
    public static OptionalOf<T> Of<T>() => new();
}
