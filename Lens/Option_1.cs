namespace Macaron.Optics;

public readonly struct Option<T>
{
    #region Fields
    private readonly bool? _isSome;
    private readonly T _value;
    #endregion

    #region Properties
    public bool IsSome => _isSome ?? throw new InvalidOperationException("Option is not initialized.");

    public bool IsNone => !_isSome ?? throw new InvalidOperationException("Option is not initialized.");

    public T Value => IsSome ? _value : throw new InvalidOperationException("Option is None.");
    #endregion

    #region Constructors
    public Option(bool isSome, T value)
    {
        _isSome = isSome;
        _value = value;
    }
    #endregion

    #region Methods
    public T GetOrElse(in T value)
    {
        return IsSome ? _value! : value;
    }

    public Option<T> OrElse(in T value)
    {
        return IsSome switch
        {
            true => this,
            false => Option.Some(value)
        };
    }

    public Option<T2> Map<T2>(Func<T, T2> func)
    {
        return IsSome switch
        {
            true => Option.Some(func(_value!)),
            false => Option.None<T2>()
        };
    }

    public Option<T2> FlatMap<T2>(Func<T, Option<T2>> func)
    {
        return IsSome switch
        {
            true => func(_value!),
            false => Option.None<T2>()
        };
    }
    #endregion
}
