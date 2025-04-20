using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class OptionalExtensions
{
    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optional,
        Func<Maybe<TValue1>, Maybe<TValue2>> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = optional.Get(value0);
            var value2 = optionalGetter.Invoke(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optional,
        OptionalGetter<Maybe<TValue1>, TValue2> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = optional.Get(value0);
            var value2 = optionalGetter.Get(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optional,
        Func<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = optional.Get(value0);
            var value2 = getter.Invoke(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optional,
        Getter<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = optional.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optional1,
        Optional<TValue1, TValue2> optional2
    )
    {
        return Optional<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value0 = source;
                var value1 = optional1.Get(value0);
                var value2 = value1.IsJust ? optional2.Get(value1.Value) : Nothing<TValue2>();

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional1.Get(value0);

                var newValue1 = value1.IsJust ? Just(optional2.Set(value1.Value, value)) : Nothing<TValue1>();
                var newValue0 = newValue1.IsJust ? optional1.Set(source, newValue1.Value) : source;

                return newValue0;
            }
        );
    }

    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optional1,
        Optional<Maybe<TValue1>, TValue2> optional2
    )
    {
        return Optional<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value0 = source;
                var value1 = optional1.Get(value0);
                var value2 = optional2.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional1.Get(value0);

                var newValue1 = optional2.Set(value1, value);
                var newValue0 = newValue1.IsJust ? optional1.Set(source, newValue1.Value): source;

                return newValue0;
            }
        );
    }

    public static Optional<Maybe<T>, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<Maybe<T>, TValue1> optional1,
        Optional<Maybe<TValue1>, TValue2> optional2
    )
    {
        return Optional<Maybe<T>, TValue2>.Of(
            optionalGetter: source =>
            {
                var value0 = source;
                var value1 = optional1.Get(value0);
                var value2 = optional2.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional1.Get(value0);

                var newValue1 = optional2.Set(value1, value);
                var newValue0 = newValue1.IsJust ? optional1.Set(source, newValue1.Value): source;

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optional,
        Lens<TValue1, TValue2> lens,
        Func<T, TValue1> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optional.Get(value0);
                var value2 = lens.Get(value1 is { IsJust: true } just ? just.Value : getDefaultValue(source));

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional.Get(value0);

                if (value1.IsNothing)
                {
                    return value0;
                }

                var newValue1 = lens.Set(value1.Value, value);
                var newValue0 = optional.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optional,
        Lens<TValue1, TValue2> lens,
        Func<TValue1> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optional.Get(value0);
                var value2 = lens.Get(value1.GetOrElse(getDefaultValue()));

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional.Get(value0);

                if (value1.IsNothing)
                {
                    return value0;
                }

                var newValue1 = lens.Set(value1.Value, value);
                var newValue0 = optional.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optional,
        Lens<TValue1, TValue2> lens,
        TValue1 defaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optional.Get(value0);
                var value2 = lens.Get(value1.GetOrElse(defaultValue));

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional.Get(value0);

                if (value1.IsNothing)
                {
                    return value0;
                }

                var newValue1 = lens.Set(value1.Value, value);
                var newValue0 = optional.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<Maybe<T>, TValue1> optional,
        Lens<TValue1, TValue2> lens,
        Func<T, TValue1> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optional.Get(Just(value0));
                var value2 = lens.Get(value1 is { IsJust: true } ? value1.Value : getDefaultValue(source));

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional.Get(Just(value0));

                if (value1.IsNothing)
                {
                    return value0;
                }

                var newValue1 = lens.Set(value1.Value, value);
                var newValue0 = optional.Set(Just(source), newValue1).Value; // optional.Set이 Just를 반환한다고 가정한다.

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<Maybe<T>, TValue1> optional,
        Lens<TValue1, TValue2> lens,
        Func<TValue1> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optional.Get(Just(value0));
                var value2 = lens.Get(value1 is { IsJust: true } ? value1.Value : getDefaultValue());

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional.Get(Just(value0));

                if (value1.IsNothing)
                {
                    return value0;
                }

                var newValue1 = lens.Set(value1.Value, value);
                var newValue0 = optional.Set(Just(source), newValue1).Value; // optional.Set이 Just를 반환한다고 가정한다.

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<Maybe<T>, TValue1> optional,
        Lens<TValue1, TValue2> lens,
        TValue1 defaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optional.Get(Just(value0));
                var value2 = lens.Get(value1 is { IsJust: true } ? value1.Value : defaultValue);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional.Get(Just(value0));

                if (value1.IsNothing)
                {
                    return value0;
                }

                var newValue1 = lens.Set(value1.Value, value);
                var newValue0 = optional.Set(Just(source), newValue1).Value; // optional.Set이 Just를 반환한다고 가정한다.

                return newValue0;
            }
        );
    }

    public static Optional<T, TValue> Transform<T, TValue>(
        this Optional<T, TValue> optional,
        Func<T, TValue, Maybe<TValue>>? mapGet = null,
        Func<T, TValue, TValue>? mapSet = null
    )
    {
        return Optional<T, TValue>.Of(
            optionalGetter: source =>
            {
                var value = optional.Get(source);
                if (value.IsNothing)
                {
                    return value;
                }

                return mapGet?.Invoke(source, value.Value) ?? value;
            },
            setter: (source, value) =>
            {
                return optional.Set(source, mapSet != null ? mapSet.Invoke(source, value) : value);
            }
        );
    }

    public static Optional<T, TValue> Transform<T, TValue>(
        this Optional<T, TValue> optional,
        Func<TValue, Maybe<TValue>>? mapGet = null,
        Func<TValue, TValue>? mapSet = null
    )
    {
        return Optional<T, TValue>.Of(
            optionalGetter: source =>
            {
                var value = optional.Get(source);
                if (value.IsNothing)
                {
                    return value;
                }

                return mapGet?.Invoke(value.Value) ?? value;
            },
            setter: (source, value) =>
            {
                return optional.Set(source, mapSet != null ? mapSet.Invoke(value) : value);
            }
        );
    }

    public static T Modify<T, TValue>(
        this Optional<T, TValue> optional,
        T source,
        Func<T, TValue, Maybe<TValue>> modifier
    )
    {
        var value = optional.Get(source);
        if (value.IsNothing)
        {
            return source;
        }

        var newValue = modifier.Invoke(source, value.Value);
        if (newValue.IsNothing)
        {
            return source;
        }

        var newSource = optional.Set(source, newValue.Value);
        return newSource;
    }

    public static T Modify<T, TValue>(
        this Optional<T, TValue> optional,
        T source,
        Func<TValue, Maybe<TValue>> modifier
    )
    {
        var value = optional.Get(source);
        if (value.IsNothing)
        {
            return source;
        }

        var newValue = modifier.Invoke(value.Value);
        if (newValue.IsNothing)
        {
            return source;
        }

        var newSource = optional.Set(source, newValue.Value);
        return newSource;
    }

    public static OptionalGetter<T, TValue> ToOptionalGetter<T, TValue>(
        this Optional<T, TValue> optional
    )
    {
        return OptionalGetter<T, TValue>.Of(
            optionalGetter: optional.Get
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Optional<T, TValue> optional,
        Func<T, TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            getter: source => optional.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(source)
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Optional<T, TValue> optional,
        Func<TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            getter: source => optional.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue()
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Optional<T, TValue> optional,
        TValue defaultValue
    )
    {
        return Getter<T, TValue>.Of(
            getter: source => optional.Get(source) is { IsJust: true } just ? just.Value : defaultValue
        );
    }

    public static Setter<T, TValue> ToSetter<T, TValue>(
        this Optional<T, TValue> optional
    )
    {
        return Setter<T, TValue>.Of(
            setter: optional.Set
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this Optional<T, TValue> optional,
        Func<T, TValue> getDefaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optional.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(source),
            setter: optional.Set
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this Optional<T, TValue> optional,
        Func<TValue> getDefaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optional.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(),
            setter: optional.Set
        );
    }

    public static Lens<T, TValue> ToLens<T, TValue>(
        this Optional<T, TValue> optional,
        TValue defaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optional.Get(source) is { IsJust: true } just ? just.Value : defaultValue,
            setter: optional.Set
        );
    }
}
