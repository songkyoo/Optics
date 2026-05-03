using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class PrismExtensions
{
    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Func<Maybe<TValue1>, Maybe<TValue2>> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = prism.Get(value0);
            var value2 = optionalGetter(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        OptionalGetter<Maybe<TValue1>, TValue2> optionalGetter
    )
    {
        return OptionalGetter<T, TValue2>.Of(optionalGetter: source =>
        {
            var value0 = source;
            var value1 = prism.Get(value0);
            var value2 = optionalGetter.Get(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Func<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = prism.Get(value0);
            var value2 = getter(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Getter<Maybe<TValue1>, TValue2> getter
    )
    {
        return Getter<T, TValue2>.Of(getter: source =>
        {
            var value0 = source;
            var value1 = prism.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static Constructor<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Func<TValue2, TValue1> constructor
    )
    {
        return Constructor<T, TValue2>.Of(constructor: value =>
        {
            var newValue1 = constructor(value);
            var newValue0 = prism.Construct(newValue1);

            return newValue0;
        });
    }

    public static Constructor<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Constructor<TValue1, TValue2> constructor
    )
    {
        return Constructor<T, TValue2>.Of(constructor: value =>
        {
            var newValue1 = constructor.Construct(value);
            var newValue0 = prism.Construct(newValue1);

            return newValue0;
        });
    }

    public static Setter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Setter<TValue1, TValue2> setter
    )
    {
        return Setter<T, TValue2>.Of(setter: (source, value) =>
        {
            var value1 = prism.Get(source);
            if (value1.IsNothing)
            {
                return source;
            }

            var newValue1 = setter.Set(value1.Value, value);
            var newValue0 = prism.Construct(newValue1);

            return newValue0;
        });
    }

    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Lens<TValue1, TValue2> lens
    )
    {
        return Optional<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = value1.IsJust ? Just(lens.Get(value1.Value)) : Nothing<TValue2>();

                return value2;
            },
            setter: (source, value) =>
            {
                var value1 = prism.Get(source);
                if (value1.IsNothing)
                {
                    return source;
                }

                var newValue1 = lens.Set(value1.Value, value);
                var newValue0 = prism.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Lens<TValue1, TValue2> lens,
        Func<T, TValue1> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = lens.Get(value1 is { IsJust: true } just ? just.Value : getDefaultValue(source));

                return value2;
            },
            setter: (source, value) =>
            {
                var value1 = prism.Get(source);
                var newValue1 = lens.Set(
                    value1 is { IsJust: true } just ? just.Value : getDefaultValue(source),
                    value
                );
                var newValue0 = prism.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Lens<TValue1, TValue2> lens,
        Func<TValue1> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = lens.Get(value1 is { IsJust: true } just ? just.Value : getDefaultValue());

                return value2;
            },
            setter: (source, value) =>
            {
                var value1 = prism.Get(source);
                var newValue1 = lens.Set(
                    value1 is { IsJust: true } just ? just.Value : getDefaultValue(),
                    value
                );
                var newValue0 = prism.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Lens<TValue1, TValue2> lens,
        TValue1 defaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = lens.Get(value1.GetOrElse(defaultValue));

                return value2;
            },
            setter: (source, value) =>
            {
                var value1 = prism.Get(source);
                var newValue1 = lens.Set(value1.GetOrElse(defaultValue), value);
                var newValue0 = prism.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Optional<TValue1, TValue2> optional
    )
    {
        return Optional<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = value1.IsJust ? optional.Get(value1.Value) : Nothing<TValue2>();

                return value2;
            },
            setter: (source, value) =>
            {
                var value1 = prism.Get(source);
                if (value1.IsNothing)
                {
                    return source;
                }

                var newValue1 = optional.Set(value1.Value, value);
                var newValue0 = prism.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Optional<Maybe<TValue1>, TValue2> optional
    )
    {
        return Optional<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = optional.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value1 = prism.Get(source);
                var newValue1 = optional.Set(value1, value);
                var newValue0 = newValue1.IsJust ? prism.Construct(newValue1.Value) : source;

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Optional<Maybe<TValue1>, TValue2> optional,
        Func<T, TValue2> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = optional.Get(value1) is { IsJust: true } just ? just.Value : getDefaultValue(source);

                return value2;
            },
            setter: (source, value) =>
            {
                var value1 = prism.Get(source);
                var newValue1 = optional.Set(value1, value);
                var newValue0 = newValue1.IsJust ? prism.Construct(newValue1.Value) : source;

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Optional<Maybe<TValue1>, TValue2> optional,
        Func<TValue2> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = optional.Get(value1) is { IsJust: true } just ? just.Value : getDefaultValue();

                return value2;
            },
            setter: (source, value) =>
            {
                var value1 = prism.Get(source);
                var newValue1 = optional.Set(value1, value);
                var newValue0 = newValue1.IsJust ? prism.Construct(newValue1.Value) : source;

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Optional<Maybe<TValue1>, TValue2> optional,
        TValue2 defaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = optional.Get(value1).GetOrElse(defaultValue);

                return value2;
            },
            setter: (source, value) =>
            {
                var value1 = prism.Get(source);
                var newValue1 = optional.Set(value1, value);
                var newValue0 = newValue1.IsJust ? prism.Construct(newValue1.Value) : source;

                return newValue0;
            }
        );
    }

    public static Prism<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism1,
        Prism<TValue1, TValue2> prism2
    )
    {
        return Prism<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value0 = source;
                var value1 = prism1.Get(value0);
                var value2 = value1 is { IsJust: true } ? prism2.Get(value1.Value) : Nothing();

                return value2;
            },
            constructor: value =>
            {
                var newValue1 = prism2.Construct(value);
                var newValue0 = prism1.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Prism<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Iso<TValue1, TValue2> iso
    )
    {
        return Prism<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value1 = prism.Get(source);
                var value2 = value1.IsJust ? Just(iso.Get(value1.Value)) : Nothing<TValue2>();

                return value2;
            },
            constructor: value =>
            {
                var newValue1 = iso.Construct(value);
                var newValue0 = prism.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Iso<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Iso<TValue1, TValue2> iso,
        Func<T, TValue1> getDefaultValue
    )
    {
        return Iso<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = prism.Get(value0) is { IsJust: true } just ? just.Value : getDefaultValue(value0);
                var value2 = iso.Get(value1);

                return value2;
            },
            constructor: value =>
            {
                var newValue1 = iso.Construct(value);
                var newValue0 = prism.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Iso<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Iso<TValue1, TValue2> iso,
        Func<TValue1> getDefaultValue
    )
    {
        return Iso<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = prism.Get(value0) is { IsJust: true } just ? just.Value : getDefaultValue();
                var value2 = iso.Get(value1);

                return value2;
            },
            constructor: value =>
            {
                var newValue1 = iso.Construct(value);
                var newValue0 = prism.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Iso<T, TValue2> Compose<T, TValue1, TValue2>(
        this Prism<T, TValue1> prism,
        Iso<TValue1, TValue2> iso,
        TValue1 defaultValue
    )
    {
        return Iso<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = prism.Get(value0) is { IsJust: true } just ? just.Value : defaultValue;
                var value2 = iso.Get(value1);

                return value2;
            },
            constructor: value =>
            {
                var newValue1 = iso.Construct(value);
                var newValue0 = prism.Construct(newValue1);

                return newValue0;
            }
        );
    }

    public static Prism<T, TValue> Transform<T, TValue>(
        this Prism<T, TValue> prism,
        Func<T, TValue, Maybe<TValue>>? mapGet = null,
        Func<TValue, T, T>? mapConstruct = null
    )
    {
        return Prism<T, TValue>.Of(
            optionalGetter: source =>
            {
                var value = prism.Get(source);
                if (value.IsNothing)
                {
                    return value;
                }

                return mapGet?.Invoke(source, value.Value) ?? value;
            },
            constructor: value =>
            {
                var source = prism.Construct(value);
                return mapConstruct != null ? mapConstruct(value, source) : source;
            }
        );
    }

    public static Prism<T, TValue> Transform<T, TValue>(
        this Prism<T, TValue> prism,
        Func<TValue, Maybe<TValue>>? mapGet = null,
        Func<T, T>? mapConstruct = null
    )
    {
        return Prism<T, TValue>.Of(
            optionalGetter: source =>
            {
                var value = prism.Get(source);
                if (value.IsNothing)
                {
                    return value;
                }

                return mapGet?.Invoke(value.Value) ?? value;
            },
            constructor: value =>
            {
                var source = prism.Construct(value);
                return mapConstruct != null ? mapConstruct(source) : source;
            }
        );
    }

    public static OptionalGetter<T, TValue> ToOptionalGetter<T, TValue>(
        this Prism<T, TValue> prism
    )
    {
        return OptionalGetter<T, TValue>.Of(optionalGetter: prism.Get);
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Prism<T, TValue> prism,
        Func<T, TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            getter: source => prism.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(source)
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Prism<T, TValue> prism,
        Func<TValue> getDefaultValue
    )
    {
        return Getter<T, TValue>.Of(
            getter: source => prism.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue()
        );
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(
        this Prism<T, TValue> prism,
        TValue defaultValue
    )
    {
        return Getter<T, TValue>.Of(
            getter: source => prism.Get(source) is { IsJust: true } just ? just.Value : defaultValue
        );
    }

    public static Constructor<T, TValue> ToConstructor<T, TValue>(
        this Prism<T, TValue> prism
    )
    {
        return Constructor<T, TValue>.Of(constructor: prism.Construct);
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Prism<T, TValue> prism,
        Func<T, TValue> getDefaultValue
    )
    {
        return Iso<T, TValue>.Of(
            getter: source => prism.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(source),
            constructor: prism.Construct
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Prism<T, TValue> prism,
        Func<TValue> getDefaultValue
    )
    {
        return Iso<T, TValue>.Of(
            getter: source => prism.Get(source) is { IsJust: true } just ? just.Value : getDefaultValue(),
            constructor: prism.Construct
        );
    }

    public static Iso<T, TValue> ToIso<T, TValue>(
        this Prism<T, TValue> prism,
        TValue defaultValue
    )
    {
        return Iso<T, TValue>.Of(
            getter: source => prism.Get(source) is { IsJust: true } just ? just.Value : defaultValue,
            constructor: prism.Construct
        );
    }
}
