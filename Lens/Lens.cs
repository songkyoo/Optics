namespace Macaron.Optics;

public static partial class Lens
{
    /// <summary>
    /// <see cref="LensOf{T}"/> 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T">대상 타입.</typeparam>
    /// <returns><see cref="LensOf{T}"/> 인스턴스.</returns>
    /// <remarks>
    /// 이 메서드에 지정된 <typeparamref name="T"/> 타입에서 <c>with</c> 문을 적용 가능한 멤버에 대해
    /// <c>Lens&lt;T, TValue&gt;</c>, <c>OptionLens&lt;Option&lt;T&gt;, TValue&gt;</c>를 반환하는 확장 메서드가 생성된다.
    /// </remarks>
    public static LensOf<T> Of<T>() => new();
}
