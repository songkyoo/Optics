namespace Macaron.Optics;

public readonly struct Option<T>
{
    #region Fields
    private readonly bool _isSome;
    private readonly T _value;
    #endregion

    #region Properties
    public bool IsSome => _isSome;

    public bool IsNone => !_isSome;

    public T Value => _isSome ? _value : throw new InvalidOperationException("Option is None.");
    #endregion

    #region Constructors
    public Option(bool isSome, T value)
    {
        _isSome = isSome;
        _value = value;
    }
    #endregion
}
