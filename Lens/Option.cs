namespace Macaron.Optics;

public static class Option
{
    #region Static
    public static Option<T> Some<T>(T value) => new(true, value);

    public static Option<T> None<T>() => new(false, default!);
    #endregion
}
