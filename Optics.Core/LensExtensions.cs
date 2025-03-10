using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class LensExtensions
{
    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Func<TValue1, TValue2> getter
    )
    {
        return Getter.Of<T, TValue2>(source =>
        {
            var value0 = source;
            var value1 = lens.Get(value0);
            var value2 = getter.Invoke(value1);

            return value2;
        });
    }

    public static Getter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Getter<TValue1, TValue2> getter
    )
    {
        return Getter.Of<T, TValue2>(source =>
        {
            var value0 = source;
            var value1 = lens.Get(value0);
            var value2 = getter.Get(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Func<TValue1, Maybe<TValue2>> optionalGetter
    )
    {
        return OptionalGetter.Of<T, TValue2>(source =>
        {
            var value0 = source;
            var value1 = lens.Get(value0);
            var value2 = optionalGetter.Invoke(value1);

            return value2;
        });
    }

    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        OptionalGetter<TValue1, TValue2> optionalGetter
    )
    {
        return OptionalGetter.Of<T, TValue2>(source =>
        {
            var value0 = source;
            var value1 = lens.Get(value0);
            var value2 = optionalGetter.Get(value1);

            return value2;
        });
    }

    /// <summary>
    /// 두 개의 <see cref="Lens{T,TValue}"/> 인스턴스를 연결하여 하나의 <see cref="Lens{T,TValue}"/>로 만든다.
    /// </summary>
    /// <typeparam name="T"><see cref="Lens{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="lens1"><c>Lens&lt;T, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="lens2">
    /// 첫 번째 <see cref="Lens{T,TValue}"/>와 연결될 <c>Lens&lt;TValue1, TValue2&gt;</c> 인스턴스.
    /// </param>
    /// <returns>두 <see cref="Lens{T,TValue}"/>를 연결하여 생성된 <c>Lens&lt;T, TValue2&gt;</c> 인스턴스.</returns>
    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens1,
        Lens<TValue1, TValue2> lens2
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);

                var newValue1 = lens2.Set(value1, value);
                var newValue0 = lens1.Set(value0, newValue1);

                return newValue0;
            }
        );
    }

    /// <summary>
    /// <see cref="Lens{T,TValue}"/> 인스턴스와 <see cref="Optional{T,TValue}"/> 인스턴스를 연결하여 하나의
    /// <see cref="Optional{T,TValue}"/>로 만든다.
    /// </summary>
    /// <typeparam name="T"><see cref="Lens{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="lens"><c>Lens&lt;T, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="optional">
    /// 첫 번째 <see cref="Lens{T,TValue}"/>와 연결될 <c>Optional&lt;TValue1, TValue2&gt;</c> 인스턴스.
    /// </param>
    /// <returns>
    /// <see cref="Lens{T,TValue}"/> 인스턴스와 <see cref="Optional{T,TValue}"/> 인스턴스를 연결하여 생성된
    /// <c>Optional&lt;T, TValue2&gt;</c> 인스턴스.
    /// </returns>
    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Optional<TValue1, TValue2> optional
    )
    {
        return Optional<T, TValue2>.Of(
            optionalGetter: source =>
            {
                var value0 = source;
                var value1 = lens.Get(value0);
                var value2 = optional.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens.Get(value0);

                var newValue1 = optional.Set(value1, value);
                var newValue0 = lens.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        Iso<TValue1, TValue2> iso
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens.Get(value0);
                var value2 = iso.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var newValue1 = iso.Construct(value);
                var newValue0 = lens.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    /// <summary>
    /// <see cref="Lens{T,TValue}"/> 인스턴스를 사용하여 대상 인스턴스의 값을 읽고 그 값을 사용하여 새로운 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Lens{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="lens"><see cref="Lens{T,TValue}"/> 인스턴스.</param>
    /// <param name="source">대상 인스턴스.</param>
    /// <param name="modifier">기존 값을 받아 새로운 값을 반환하는 함수.</param>
    /// <returns>새로운 <typeparamref name="T"/> 인스턴스.</returns>
    public static T Modify<T, TValue>(this Lens<T, TValue> lens, T source, Func<TValue, TValue> modifier)
    {
        var value = lens.Get(source);
        var newValue = modifier.Invoke(value);
        var newSource = lens.Set(source, newValue);

        return newSource;
    }

    /// <summary>
    /// <see cref="Lens{T,TValue}"/> 인스턴스를 사용하여 대상 인스턴스의 값을 읽고 그 값을 사용하여 새로운 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Lens{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="lens"><see cref="Lens{T,TValue}"/> 인스턴스.</param>
    /// <param name="source">대상 인스턴스.</param>
    /// <param name="modifier">대상 인스턴스와 기존 값을 받아 새로운 값을 반환하는 함수.</param>
    /// <returns>새로운 <typeparamref name="T"/> 인스턴스.</returns>
    public static T Modify<T, TValue>(this Lens<T, TValue> lens, T source, Func<T, TValue, TValue> modifier)
    {
        var value = lens.Get(source);
        var newValue = modifier.Invoke(source, value);
        var newSource = lens.Set(source, newValue);

        return newSource;
    }

    public static Setter<T, TValue> ToSetter<T, TValue>(this Lens<T, TValue> lens)
    {
        return Setter<T, TValue>.Of(lens.Set);
    }

    public static Getter<T, TValue> ToGetter<T, TValue>(this Lens<T, TValue> lens)
    {
        return Getter<T, TValue>.Of(lens.Get);
    }

    public static OptionalGetter<T, TValue> ToOptionalGetter<T, TValue>(this Lens<T, TValue> lens)
    {
        return OptionalGetter<T, TValue>.Of(lens.Get);
    }

    /// <summary>
    /// <see cref="Lens{T,TValue}"/> 인스턴스를 <see cref="Optional{T,TValue}"/> 인스턴스로 변환한다.
    /// </summary>
    /// <param name="lens"><see cref="Lens{T,TValue}"/> 인스턴스.</param>
    /// <typeparam name="T"><see cref="Lens{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <returns><see cref="Optional{T,TValue}"/> 인스턴스.</returns>
    public static Optional<T, TValue> ToOptional<T, TValue>(this Lens<T, TValue> lens)
    {
        return Optional<T, TValue>.Of(
            optionalGetter: source => Just(lens.Get(source)),
            setter: (source, value) => lens.Set(source, value)
        );
    }
}
