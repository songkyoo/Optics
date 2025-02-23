namespace Macaron.Optics;

/// <summary>
/// <typeparamref name="T"/> 타입에 대한 <c>Lens&lt;T, TValue&gt;</c> 렌즈 목록을 가지는 구조체.
/// </summary>
/// <typeparam name="T">렌즈 목록을 가지는 타입.</typeparam>
public readonly record struct LensOf<T>;
