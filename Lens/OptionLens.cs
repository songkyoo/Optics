namespace Macaron.Optics;

public static partial class OptionLens
{
    public static OptionLensOf<T> Of<T>() => new();
}
