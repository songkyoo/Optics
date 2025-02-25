﻿using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class OptionalExtensions
{
    /// <summary>
    /// <see cref="Optional{T,TValue}"/> 인스턴스를 사용하여 대상 인스턴스의 값을 읽고 그 값을 사용하여 새로운 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Optional{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="optional"><see cref="Optional{T,TValue}"/> 인스턴스.</param>
    /// <param name="source">대상 인스턴스.</param>
    /// <param name="fn">
    /// 대상 인스턴스와 기존 값을 받아 새로운 값을 반환하는 함수. <paramref name="optional"/>의
    /// <see cref="Optional{T,TValue}.Get"/> 호출의 결과가 값을 가지고 있지 않다면 호출되지 않는다.
    /// </param>
    /// <returns>
    /// <paramref name="fn"/> 함수가 반환한 <see cref="Maybe{TValue}"/> 인스턴스가 값을 가지고 있다면 해당 값을 설정한 새
    /// <typeparamref name="T"/> 인스턴스를 반환하고 그렇지 않다면 <paramref name="source"/>를 그대로 반환한다.
    /// </returns>
    public static T Modify<T, TValue>(this Optional<T, TValue> optional, T source, Func<T, TValue, Maybe<TValue>> fn)
    {
        var value = optional.Get(source);
        if (value.IsNothing)
        {
            return source;
        }

        var newValue = fn(source, value.Value);
        if (newValue.IsNothing)
        {
            return source;
        }

        var newSource = optional.Set(source, newValue.Value);
        return newSource;
    }

    /// <summary>
    /// <see cref="Optional{T,TValue}"/> 인스턴스를 사용하여 대상 인스턴스의 값을 읽고 그 값을 사용하여 새로운 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Optional{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="optional"><see cref="Optional{T,TValue}"/> 인스턴스.</param>
    /// <param name="source">대상 인스턴스.</param>
    /// <param name="fn">
    /// 기존 값을 받아 새로운 값을 반환하는 함수. <paramref name="optional"/>의 <see cref="Optional{T,TValue}.Get"/> 호출의
    /// 결과가 값을 가지고 있지 않다면 호출되지 않는다.
    /// </param>
    /// <returns>
    /// <paramref name="fn"/> 함수가 반환한 <see cref="Maybe{TValue}"/> 인스턴스가 값을 가지고 있다면 해당 값을 설정한 새
    /// <typeparamref name="T"/> 인스턴스를 반환하고 그렇지 않다면 <paramref name="source"/>를 그대로 반환한다.
    /// </returns>
    public static T Modify<T, TValue>(this Optional<T, TValue> optional, T source, Func<TValue, Maybe<TValue>> fn)
    {
        return optional.Modify(source, fn: (_, value) => fn(value));
    }

    /// <summary>
    /// 두 개의 <see cref="Optional{T,TValue}"/> 인스턴스를 연결하여 하나의 <see cref="Optional{T,TValue}"/>로 만든다.
    /// 생성된 인스턴스는 <see cref="Optional{T,TValue}.Get"/> 호출 결과에 값이 없는 경우
    /// <see cref="Optional{T,TValue}.Set"/> 호출 시 전달된 대상 인스턴스를 그대로 반환한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Optional{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="optionLens1"><c>Optional&lt;T, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="optionLens2">
    /// 첫 번째 <see cref="Optional{T,TValue}"/>와 연결될 <c>Optional&lt;TValue1, TValue2&gt;</c> 인스턴스.
    /// </param>
    /// <returns>
    /// 두 <see cref="Optional{T,TValue}"/>를 연결하여 생성된 <c>Optional&lt;T, TValue2&gt;</c> 인스턴스.
    /// </returns>
    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optionLens1,
        Optional<TValue1, TValue2> optionLens2
    )
    {
        return Optional<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optionLens1.Get(value0);
                var value2 = value1.IsJust ? optionLens2.Get(value1.Value) : Nothing<TValue2>();

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optionLens1.Get(value0);

                var newValue1 = value1.IsJust ? Just(optionLens2.Set(value1.Value, value)) : Nothing<TValue1>();
                var newValue0 = newValue1.IsJust ? optionLens1.Set(source, newValue1.Value) : source;

                return newValue0;
            }
        );
    }

    /// <summary>
    /// 두 개의 <see cref="Optional{T,TValue}"/> 인스턴스를 연결하여 하나의 <see cref="Optional{T,TValue}"/>로 만든다.
    /// 두 번째 인스턴스가 <see cref="Maybe{TValue1}"/>을 대상으로 하는 경우를 처리하며 생성된 인스턴스는
    /// <see cref="Optional{T,TValue}.Get"/> 호출 결과에 값이 없는 경우 <see cref="Optional{T,TValue}.Set"/> 호출 시
    /// 전달된 대상 인스턴스를 그대로 반환한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Optional{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <param name="optionLens1"><c>Optional&lt;T, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="optionLens2">
    /// 첫 번째 <see cref="Optional{T,TValue}"/>와 연결될 <c>Optional&lt;Option&lt;TValue1&gt;, TValue2&gt;</c>
    /// 인스턴스.
    /// </param>
    /// <returns>
    /// 두 <see cref="Optional{T,TValue}"/>를 연결하여 생성된 <c>Optional&lt;T, TValue2&gt;</c> 인스턴스.
    /// </returns>
    public static Optional<T, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<T, TValue1> optionLens1,
        Optional<Maybe<TValue1>, TValue2> optionLens2
    )
    {
        return Optional<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optionLens1.Get(value0);
                var value2 = optionLens2.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optionLens1.Get(value0);

                var newValue1 = optionLens2.Set(value1, value);
                var newValue0 = newValue1.IsJust ? optionLens1.Set(source, newValue1.Value): source;

                return newValue0;
            }
        );
    }

    /// <summary>
    /// 두 개의 <see cref="Optional{T,TValue}"/> 인스턴스를 연결하여 하나의 <see cref="Optional{T,TValue}"/>로 만든다.
    /// 두 인스턴스의 대상 타입이 <see cref="Maybe{T}"/>인 경우를 처리하며 생성된 인스턴스는
    /// <see cref="Optional{T,TValue}.Get"/> 호출 결과에 값이 없는 경우 <see cref="Optional{T,TValue}.Set"/> 호출 시
    /// 전달된 대상 인스턴스를 그대로 반환한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Optional{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <param name="optionLens1"><c>Optional&lt;Option&lt;T&gt;, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="optionLens2">
    /// 첫 번째 <see cref="Optional{T,TValue}"/>와 연결될 <c>Optional&lt;Option&lt;TValue1&gt;, TValue2&gt;</c>
    /// 인스턴스.
    /// </param>
    /// <returns>
    /// 두 <see cref="Optional{T,TValue}"/>를 연결하여 생성된 <c>Optional&lt;Option&lt;T&gt;, TValue2&gt;</c> 인스턴스.
    /// </returns>
    public static Optional<Maybe<T>, TValue2> Compose<T, TValue1, TValue2>(
        this Optional<Maybe<T>, TValue1> optionLens1,
        Optional<Maybe<TValue1>, TValue2> optionLens2
    )
    {
        return Optional<Maybe<T>, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optionLens1.Get(value0);
                var value2 = optionLens2.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optionLens1.Get(value0);

                var newValue1 = optionLens2.Set(value1, value);
                var newValue0 = newValue1.IsJust ? optionLens1.Set(source, newValue1.Value): source;

                return newValue0;
            }
        );
    }

    /// <summary>
    /// <see cref="Optional{T,TValue}"/> 인스턴스와 <see cref="Lens{T,TValue}"/> 인스턴스를 연결하여 하나의
    /// <see cref="Optional{T,TValue}"/>로 만든다.
    /// </summary>
    /// <typeparam name="T"><see cref="Optional{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <param name="optional"><c>Optional&lt;Option&lt;T&gt;, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="lens">
    /// 첫 번째 <see cref="Optional{T,TValue}"/>와 연결될 <c>Lens&lt;TValue1, TValue2&gt;</c> 인스턴스.
    /// </param>
    /// <param name="getDefaultValue">
    /// <paramref name="optional"/>가 반환한 결과에 값이 없는 경우 사용할 기본 값을 반환하는 함수.
    /// </param>
    /// <returns>
    /// <see cref="Optional{T,TValue}"/> 인스턴스와 <see cref="Lens{T,TValue}"/> 인스턴스를 연결하여 생성된
    /// <c>Optional&lt;T, TValue2&gt;</c> 인스턴스.
    /// </returns>
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
                var value2 = lens.Get(value1.GetOrElse(getDefaultValue(source)));

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optional.Get(value0);

                var newValue1 = lens.Set(value1.GetOrElse(getDefaultValue(value0)), value);
                var newValue0 = optional.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    /// <summary>
    /// <see cref="Optional{T,TValue}"/> 인스턴스를 <see cref="Lens{T,TValue}"/> 인스턴스로 변환한다.
    /// </summary>
    /// <param name="optional"><see cref="Optional{T,TValue}"/> 인스턴스.</param>
    /// <param name="getDefaultValue">
    /// <paramref name="optional"/>가 반환한 결과에 값이 없는 경우 대상 인스턴스를 사용하여 기본 값을 반환하는 함수.
    /// </param>
    /// <typeparam name="T"><see cref="Optional{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <returns><see cref="Lens{T,TValue}"/> 인스턴스.</returns>
    public static Lens<T, TValue> ToLens<T, TValue>(this Optional<T, TValue> optional, Func<T, TValue> getDefaultValue)
    {
        return Lens<T, TValue>.Of(
            getter: source => optional.Get(source).GetOrElse(getDefaultValue(source)),
            setter: (source, value) => optional.Get(source) is { IsJust: true } val
                ? optional.Set(source, val.Value)
                : source
        );
    }

    /// <summary>
    /// <see cref="Optional{T,TValue}"/> 인스턴스를 <see cref="Lens{T,TValue}"/> 인스턴스로 변환한다.
    /// </summary>
    /// <param name="optional"><see cref="Optional{T,TValue}"/> 인스턴스.</param>
    /// <param name="getDefaultValue">
    /// <paramref name="optional"/>가 반환한 결과에 값이 없는 경우 기본 값을 반환하는 함수.
    /// </param>
    /// <typeparam name="T"><see cref="Optional{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <returns><see cref="Lens{T,TValue}"/> 인스턴스.</returns>
    public static Lens<T, TValue> ToLens<T, TValue>(this Optional<T, TValue> optional, Func<TValue> getDefaultValue)
    {
        return optional.ToLens(_ => getDefaultValue());
    }

    public static Lens<T, TValue> OrElse<T, TValue>(this Optional<T, TValue> lens, Func<T, TValue> getDefaultValue)
    {
        return Lens<T, TValue>.Of(
            getter: source => lens.Get(source) is { IsJust: true } option ? option.Value : getDefaultValue(source),
            setter: (source, value) => lens.Set(source, value)
        );
    }

    public static Lens<T, TValue> OrElse<T, TValue>(this Optional<T, TValue> lens, Func<TValue> getDefaultValue)
    {
        return lens.OrElse(_ => getDefaultValue());
    }
}
