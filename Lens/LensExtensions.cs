using System.Collections.Immutable;

using static Macaron.Optics.Option;

namespace Macaron.Optics;

public static class LensExtensions
{
    /// <summary>
    /// <see cref="Lens{T, TValue}"/> 인스턴스를 사용하여 대상 인스턴스의 값을 읽고 그 값을 사용하여 새로운 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Lens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="lens"><see cref="Lens{T, TValue}"/> 인스턴스.</param>
    /// <param name="source">대상 인스턴스.</param>
    /// <param name="update">기존 값을 받아 새로운 값을 반환하는 함수.</param>
    /// <returns>새로운 <typeparamref name="T"/> 인스턴스.</returns>
    public static T Update<T, TValue>(
        this Lens<T, TValue> lens,
        T source,
        Func<TValue, TValue> update
    )
    {
        var value = lens.Get(source);
        var newValue = update(value);
        var newSource = lens.Set(source, newValue);

        return newSource;
    }

    /// <summary>
    /// <see cref="Lens{T, TValue}"/> 인스턴스를 사용하여 대상 인스턴스의 값을 읽고 그 값을 사용하여 새로운 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Lens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="lens"><see cref="Lens{T, TValue}"/> 인스턴스.</param>
    /// <param name="source">대상 인스턴스.</param>
    /// <param name="update">대상 인스턴스와 기존 값을 받아 새로운 값을 반환하는 함수.</param>
    /// <returns>새로운 <typeparamref name="T"/> 인스턴스.</returns>
    public static T Update<T, TValue>(
        this Lens<T, TValue> lens,
        T source,
        Func<T, TValue, TValue> update
    )
    {
        var value = lens.Get(source);
        var newValue = update(source, value);
        var newSource = lens.Set(source, newValue);

        return newSource;
    }

    /// <summary>
    /// <see cref="OptionLens{T, TValue}"/> 인스턴스를 사용하여 대상 인스턴스의 값을 읽고 그 값을 사용하여 새로운 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T"><see cref="OptionLens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="optionLens"><see cref="OptionLens{T, TValue}"/> 인스턴스.</param>
    /// <param name="source">대상 인스턴스.</param>
    /// <param name="update">기존 값을 받아 새로운 값을 반환하는 함수.</param>
    /// <returns>
    /// <paramref name="update"/> 함수가 반환한 <see cref="Option{TValue}"/> 인스턴스가 값을 가지고 있다면 해당 값을 설정한 새
    /// <typeparamref name="T"/> 인스턴스를 반환하고 그렇지 않다면 <paramref name="source"/>를 그대로 반환한다.
    /// </returns>
    public static T Update<T, TValue>(
        this OptionLens<T, TValue> optionLens,
        T source,
        Func<Option<TValue>, Option<TValue>> update
    )
    {
        var value = optionLens.Get(source);
        var newValue = update(value);
        var newSource = newValue.IsSome ? optionLens.Set(source, newValue.Value) : source;

        return newSource;
    }

    /// <summary>
    /// <see cref="OptionLens{T, TValue}"/> 인스턴스를 사용하여 대상 인스턴스의 값을 읽고 그 값을 사용하여 새로운 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T"><see cref="OptionLens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="optionLens"><see cref="OptionLens{T, TValue}"/> 인스턴스.</param>
    /// <param name="source">대상 인스턴스.</param>
    /// <param name="update">대상 인스턴스와 기존 값을 받아 새로운 값을 반환하는 함수.</param>
    /// <returns>
    /// <paramref name="update"/> 함수가 반환한 <see cref="Option{TValue}"/> 인스턴스가 값을 가지고 있다면 해당 값을 설정한 새
    /// <typeparamref name="T"/> 인스턴스를 반환하고 그렇지 않다면 <paramref name="source"/>를 그대로 반환한다.
    /// </returns>
    public static T Update<T, TValue>(
        this OptionLens<T, TValue> optionLens,
        T source,
        Func<T, Option<TValue>, Option<TValue>> update
    )
    {
        var value = optionLens.Get(source);
        var newValue = update(source, value);
        var newSource = newValue.IsSome ? optionLens.Set(source, newValue.Value) : source;

        return newSource;
    }

    /// <summary>
    /// 두 개의 <see cref="Lens{T, TValue}"/> 인스턴스를 연결하여 하나의 <see cref="Lens{T, TValue}"/>로 만든다.
    /// </summary>
    /// <typeparam name="T"><see cref="Lens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="lens1"><c>Lens&lt;T, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="lens2">
    /// 첫 번째 <see cref="Lens{T, TValue}"/>와 연결될 <c>Lens&lt;TValue1, TValue2&gt;</c> 인스턴스.
    /// </param>
    /// <returns>두 <see cref="Lens{T, TValue}"/>를 연결하여 생성된 <c>Lens&lt;T, TValue2&gt;</c> 인스턴스.</returns>
    public static Lens<T, TValue2> Chain<T, TValue1, TValue2>(
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
    /// <see cref="Lens{T, TValue}"/> 인스턴스와 <see cref="OptionLens{T, TValue}"/> 인스턴스를 연결하여 하나의
    /// <see cref="OptionLens{T, TValue}"/>로 만든다.
    /// </summary>
    /// <typeparam name="T"><see cref="Lens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="lens"><c>Lens&lt;T, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="optionLens">
    /// 첫 번째 <see cref="Lens{T, TValue}"/>와 연결될 <c>OptionLens&lt;TValue1, TValue2&gt;</c> 인스턴스.
    /// </param>
    /// <returns>
    /// <see cref="Lens{T, TValue}"/> 인스턴스와 <see cref="OptionLens{T, TValue}"/> 인스턴스를 연결하여 생성된
    /// <c>OptionLens&lt;T, TValue2&gt;</c> 인스턴스.
    /// </returns>
    public static OptionLens<T, TValue2> Chain<T, TValue1, TValue2>(
        this Lens<T, TValue1> lens,
        OptionLens<TValue1, TValue2> optionLens
    )
    {
        return OptionLens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens.Get(value0);
                var value2 = optionLens.Get(value1);

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens.Get(value0);

                var newValue1 = optionLens.Set(value1, value);
                var newValue0 = lens.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    /// <summary>
    /// 두 개의 <see cref="OptionLens{T, TValue}"/> 인스턴스를 연결하여 하나의 <see cref="OptionLens{T, TValue}"/>로 만든다.
    /// 생성된 인스턴스는 <see cref="OptionLens{T, TValue}.Get"/> 호출 결과에 값이 없는 경우
    /// <see cref="OptionLens{T, TValue}.Set"/> 호출 시 전달된 대상 인스턴스를 그대로 반환한다.
    /// </summary>
    /// <typeparam name="T"><see cref="OptionLens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="optionLens1"><c>OptionLens&lt;T, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="optionLens2">
    /// 첫 번째 <see cref="OptionLens{T, TValue}"/>와 연결될 <c>OptionLens&lt;TValue1, TValue2&gt;</c> 인스턴스.
    /// </param>
    /// <returns>
    /// 두 <see cref="OptionLens{T, TValue}"/>를 연결하여 생성된 <c>OptionLens&lt;T, TValue2&gt;</c> 인스턴스.
    /// </returns>
    public static OptionLens<T, TValue2> Chain<T, TValue1, TValue2>(
        this OptionLens<T, TValue1> optionLens1,
        OptionLens<TValue1, TValue2> optionLens2
    )
    {
        return OptionLens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optionLens1.Get(value0);
                var value2 = value1.IsSome ? optionLens2.Get(value1.Value) : None<TValue2>();

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optionLens1.Get(value0);

                var newValue1 = value1.IsSome ? Some(optionLens2.Set(value1.Value, value)) : None<TValue1>();
                var newValue0 = newValue1.IsSome ? optionLens1.Set(source, newValue1.Value) : source;

                return newValue0;
            }
        );
    }

    /// <summary>
    /// 두 개의 <see cref="OptionLens{T, TValue}"/> 인스턴스를 연결하여 하나의 <see cref="OptionLens{T, TValue}"/>로 만든다.
    /// 두 번째 인스턴스가 <see cref="Option{TValue1}"/>을 대상으로 하는 경우를 처리하며 생성된 인스턴스는
    /// <see cref="OptionLens{T, TValue}.Get"/> 호출 결과에 값이 없는 경우 <see cref="OptionLens{T, TValue}.Set"/> 호출 시
    /// 전달된 대상 인스턴스를 그대로 반환한다.
    /// </summary>
    /// <typeparam name="T"><see cref="OptionLens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <param name="optionLens1"><c>OptionLens&lt;T, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="optionLens2">
    /// 첫 번째 <see cref="OptionLens{T, TValue}"/>와 연결될 <c>OptionLens&lt;Option&lt;TValue1&gt;, TValue2&gt;</c>
    /// 인스턴스.
    /// </param>
    /// <returns>
    /// 두 <see cref="OptionLens{T, TValue}"/>를 연결하여 생성된 <c>OptionLens&lt;T, TValue2&gt;</c> 인스턴스.
    /// </returns>
    public static OptionLens<T, TValue2> Chain<T, TValue1, TValue2>(
        this OptionLens<T, TValue1> optionLens1,
        OptionLens<Option<TValue1>, TValue2> optionLens2
    )
    {
        return OptionLens<T, TValue2>.Of(
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
                var newValue0 = newValue1.IsSome ? optionLens1.Set(source, newValue1.Value): source;

                return newValue0;
            }
        );
    }

    /// <summary>
    /// 두 개의 <see cref="OptionLens{T, TValue}"/> 인스턴스를 연결하여 하나의 <see cref="OptionLens{T, TValue}"/>로 만든다.
    /// 두 인스턴스의 대상 타입이 <see cref="Option{T}"/>인 경우를 처리하며 생성된 인스턴스는
    /// <see cref="OptionLens{T, TValue}.Get"/> 호출 결과에 값이 없는 경우 <see cref="OptionLens{T, TValue}.Set"/> 호출 시
    /// 전달된 대상 인스턴스를 그대로 반환한다.
    /// </summary>
    /// <typeparam name="T"><see cref="OptionLens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <param name="optionLens1"><c>OptionLens&lt;Option&lt;T&gt;, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="optionLens2">
    /// 첫 번째 <see cref="OptionLens{T, TValue}"/>와 연결될 <c>OptionLens&lt;Option&lt;TValue1&gt;, TValue2&gt;</c>
    /// 인스턴스.
    /// </param>
    /// <returns>
    /// 두 <see cref="OptionLens{T, TValue}"/>를 연결하여 생성된 <c>OptionLens&lt;Option&lt;T&gt;, TValue2&gt;</c> 인스턴스.
    /// </returns>
    public static OptionLens<Option<T>, TValue2> Chain<T, TValue1, TValue2>(
        this OptionLens<Option<T>, TValue1> optionLens1,
        OptionLens<Option<TValue1>, TValue2> optionLens2
    )
    {
        return OptionLens<Option<T>, TValue2>.Of(
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
                var newValue0 = newValue1.IsSome ? optionLens1.Set(source, newValue1.Value): source;

                return newValue0;
            }
        );
    }

    /// <summary>
    /// <see cref="OptionLens{T, TValue}"/> 인스턴스와 <see cref="Lens{T, TValue}"/> 인스턴스를 연결하여 하나의
    /// <see cref="OptionLens{T, TValue}"/>로 만든다.
    /// </summary>
    /// <typeparam name="T"><see cref="OptionLens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue1"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <typeparam name="TValue2"><typeparamref name="TValue1"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <param name="optionLens"><c>OptionLens&lt;Option&lt;T&gt;, TValue1&gt;</c> 인스턴스.</param>
    /// <param name="lens">
    /// 첫 번째 <see cref="OptionLens{T, TValue}"/>와 연결될 <c>Lens&lt;TValue1, TValue2&gt;</c> 인스턴스.
    /// </param>
    /// <param name="getDefaultValue">
    /// <paramref name="optionLens"/>가 반환한 결과에 값이 없는 경우 사용할 기본 값을 반환하는 함수.
    /// </param>
    /// <returns>
    /// <see cref="OptionLens{T, TValue}"/> 인스턴스와 <see cref="Lens{T, TValue}"/> 인스턴스를 연결하여 생성된
    /// <c>OptionLens&lt;T, TValue2&gt;</c> 인스턴스.
    /// </returns>
    public static Lens<T, TValue2> Chain<T, TValue1, TValue2>(
        this OptionLens<T, TValue1> optionLens,
        Lens<TValue1, TValue2> lens,
        Func<T, TValue1> getDefaultValue
    )
    {
        return Lens<T, TValue2>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = optionLens.Get(value0);
                var value2 = lens.Get(value1.GetOrElse(getDefaultValue(source)));

                return value2;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = optionLens.Get(value0);

                var newValue1 = lens.Set(value1.GetOrElse(getDefaultValue(value0)), value);
                var newValue0 = optionLens.Set(source, newValue1);

                return newValue0;
            }
        );
    }

    /// <summary>
    /// <see cref="Lens{T, TValue}"/> 인스턴스를 <see cref="OptionLens{T, TValue}"/> 인스턴스로 변환한다.
    /// </summary>
    /// <param name="lens"><see cref="Lens{T, TValue}"/> 인스턴스.</param>
    /// <typeparam name="T"><see cref="Lens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <returns><see cref="OptionLens{T, TValue}"/> 인스턴스.</returns>
    public static OptionLens<T, TValue> ToOptionLens<T, TValue>(this Lens<T, TValue> lens)
    {
        return OptionLens<T, TValue>.Of(
            getter: source => Some(lens.Get(source)),
            setter: (source, value) => lens.Set(source, value)
        );
    }

    /// <summary>
    /// <see cref="OptionLens{T, TValue}"/> 인스턴스를 <see cref="Lens{T, TValue}"/> 인스턴스로 변환한다.
    /// </summary>
    /// <param name="optionLens"><see cref="OptionLens{T, TValue}"/> 인스턴스.</param>
    /// <param name="getDefaultValue">
    /// <paramref name="optionLens"/>가 반환한 결과에 값이 없는 경우 사용할 기본 값을 반환하는 함수.
    /// </param>
    /// <typeparam name="T"><see cref="OptionLens{T, TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버를 나타내는 타입.</typeparam>
    /// <returns><see cref="Lens{T, TValue}"/> 인스턴스.</returns>
    public static Lens<T, TValue> ToLens<T, TValue>(
        this OptionLens<T, TValue> optionLens,
        Func<T, TValue> getDefaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => optionLens.Get(source).GetOrElse(getDefaultValue(source)),
            setter: (source, value) => optionLens.Get(source) is { IsSome: true } val
                ? optionLens.Set(source, val.Value)
                : source
        );
    }

    public static OptionLens<T, TValue> Key<T, TKey, TValue>(
        this Lens<T, ImmutableDictionary<TKey, TValue>> lens,
        TKey key
    ) where TKey : notnull
    {
        return lens.AtKey(
            getter: static (dict, key) => dict.GetItem(key),
            setter: static (dict, key, value) => dict.SetItem(key, value),
            key: key
        );
    }

    public static OptionLens<T, TValue> Key<T, TKey, TValue>(
        this Lens<T, ImmutableSortedDictionary<TKey, TValue>> lens,
        TKey key
    ) where TKey : notnull
    {
        return lens.AtKey(
            getter: static (dict, key) => dict.GetItem(key),
            setter: static (dict, key, value) => dict.SetItem(key, value),
            key: key
        );
    }

    public static OptionLens<T, TValue> Index<T, TValue>(
        this Lens<T, ImmutableList<TValue>> lens,
        int index
    )
    {
        return lens.AtKey(
            getter: static (list, index) => list.GetItem(index),
            setter: static (list, index, value) => list.SetItem(index, value),
            key: index
        );
    }

    public static OptionLens<T, TValue> Index<T, TValue>(
        this Lens<T, ImmutableArray<TValue>> lens,
        int index
    )
    {
        return lens.AtKey(
            getter: static (array, index) => array.GetItem(index),
            setter: static (array, index, value) => array.SetItem(index, value),
            key: index
        );
    }

    public static OptionLens<T, TKeyedValue> AtKey<T, TValue, TKey, TKeyedValue>(
        this Lens<T, TValue> lens,
        Func<TValue, TKey, Option<TKeyedValue>> getter,
        Func<TValue, TKey, TKeyedValue, TValue> setter,
        TKey key
    ) where TKey : notnull
    {
        return OptionLens<T, TKeyedValue>.Of(
            getter: source => getter(lens.Get(source), key),
            setter: (source, value) => lens.Set(source, setter(lens.Get(source), key, value))
        );
    }

    public static Lens<T, TValue> OrElse<T, TValue>(
        this OptionLens<T, TValue> lens, Func<T, TValue> getDefaultValue
    )
    {
        return Lens<T, TValue>.Of(
            getter: source => lens.Get(source) is { IsSome: true } option ? option.Value : getDefaultValue(source),
            setter: (source, value) => lens.Set(source, value)
        );
    }
}

file static class FileScopeExtensions
{
    public static Option<TValue> GetItem<TKey, TValue>(this ImmutableDictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? Some(value) : None<TValue>();
    }

    public static Option<TValue> GetItem<TKey, TValue>(this ImmutableSortedDictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? Some(value) : None<TValue>();
    }

    public static Option<T> GetItem<T>(this ImmutableList<T> list, int index)
    {
        return index.IsWithinBoundsOf(list) ? Some(list[index]) : None<T>();
    }

    public static Option<T> GetItem<T>(this ImmutableArray<T> array, int index)
    {
        return index.IsWithinBoundsOf(array) ? Some(array[index]) : None<T>();
    }

    public static bool IsWithinBoundsOf<T>(this int value, IReadOnlyList<T> list)
    {
        return value >= 0 && value < list.Count;
    }
}
