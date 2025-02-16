namespace Macaron.Optics;

public static partial class Lens
{
    public static LensOf<T> Of<T>() => new();
}
