using System.Collections.Immutable;
using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics;

public static class LensExtensions
{
    /// <summary>
    /// <see cref="Lens{T,TValue}"/> 인스턴스를 사용하여 대상 인스턴스의 값을 읽고 그 값을 사용하여 새로운 인스턴스를 생성한다.
    /// </summary>
    /// <typeparam name="T"><see cref="Lens{T,TValue}"/>가 다루는 원본 객체의 타입.</typeparam>
    /// <typeparam name="TValue"><typeparamref name="T"/> 타입에서 렌즈가 다루는 대상 멤버의 타입.</typeparam>
    /// <param name="lens"><see cref="Lens{T,TValue}"/> 인스턴스.</param>
    /// <param name="source">대상 인스턴스.</param>
    /// <param name="fn">기존 값을 받아 새로운 값을 반환하는 함수.</param>
    /// <returns>새로운 <typeparamref name="T"/> 인스턴스.</returns>
    public static T Modify<T, TValue>(this Lens<T, TValue> lens, T source, Func<TValue, TValue> fn)
    {
        var value = lens.Get(source);
        var newValue = fn(value);
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
    /// <param name="fn">대상 인스턴스와 기존 값을 받아 새로운 값을 반환하는 함수.</param>
    /// <returns>새로운 <typeparamref name="T"/> 인스턴스.</returns>
    public static T Modify<T, TValue>(this Lens<T, TValue> lens, T source, Func<T, TValue, TValue> fn)
    {
        var value = lens.Get(source);
        var newValue = fn(source, value);
        var newSource = lens.Set(source, newValue);

        return newSource;
    }

    public static T Modify<T, TValue, TContext>(this Lens<T, TValue> lens, TContext context, T source, Func<TContext, TValue, TValue> fn)
    {
        var value = lens.Get(source);
        var newValue = fn(context, value);
        var newSource = lens.Set(source, newValue);

        return newSource;
    }

    public static T Modify<T, TValue, TContext>(this Lens<T, TValue> lens, TContext context, T source, Func<TContext, T, TValue, TValue> fn)
    {
        var value = lens.Get(source);
        var newValue = fn(context, source, value);
        var newSource = lens.Set(source, newValue);

        return newSource;
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
            getter: source =>
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
            getter: source => Just(lens.Get(source)),
            setter: (source, value) => lens.Set(source, value)
        );
    }

    public static Optional<T, TValue> Key<T, TKey, TValue>(
        this Lens<T, ImmutableDictionary<TKey, TValue>> lens,
        TKey key
    ) where TKey : notnull
    {
        return lens.At(
            getter: static (dict, key) => dict.GetItem(key),
            setter: static (dict, key, value) => dict.ContainsKey(key) ? dict.SetItem(key, value) : dict
            ,
            index: key
        );
    }

    public static Optional<T, TValue> Key<T, TKey, TValue>(
        this Lens<T, ImmutableSortedDictionary<TKey, TValue>> lens,
        TKey key
    ) where TKey : notnull
    {
        return lens.At(
            getter: static (dict, key) => dict.GetItem(key),
            setter: static (dict, key, value) => dict.SetItem(key, value),
            index: key
        );
    }

    public static Optional<T, TValue> Index<T, TValue>(
        this Lens<T, ImmutableList<TValue>> lens,
        int index
    )
    {
        return lens.At(
            getter: static (list, index) => list.GetItem(index),
            setter: static (list, index, value) => index.IsWithinBoundsOf(list) ? list.SetItem(index, value) : list,
            index: index
        );
    }

    public static Optional<T, TValue> Index<T, TValue>(
        this Lens<T, ImmutableArray<TValue>> lens,
        int index
    )
    {
        return lens.At(
            getter: static (array, index) => array.GetItem(index),
            setter: static (array, index, value) => index.IsWithinBoundsOf(array) ? array.SetItem(index, value) : array,
            index: index
        );
    }

    public static Optional<T, TIndexedValue> At<T, TValue, TIndex, TIndexedValue>(
        this Lens<T, TValue> lens,
        Func<TValue, TIndex, Maybe<TIndexedValue>> getter,
        Func<TValue, TIndex, TIndexedValue, TValue> setter,
        TIndex index
    ) where TIndex : notnull
    {
        return Optional<T, TIndexedValue>.Of(
            getter: source => getter(lens.Get(source), index),
            setter: (source, value) => lens.Set(source, setter(lens.Get(source), index, value))
        );
    }
}

file static class FileScopeExtensions
{
    public static Maybe<TValue> GetItem<TKey, TValue>(this ImmutableDictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? Just(value) : Nothing<TValue>();
    }

    public static Maybe<TValue> GetItem<TKey, TValue>(this ImmutableSortedDictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? Just(value) : Nothing<TValue>();
    }

    public static Maybe<T> GetItem<T>(this ImmutableList<T> list, int index)
    {
        return index.IsWithinBoundsOf(list) ? Just(list[index]) : Nothing<T>();
    }

    public static Maybe<T> GetItem<T>(this ImmutableArray<T> array, int index)
    {
        return index.IsWithinBoundsOf(array) ? Just(array[index]) : Nothing<T>();
    }

    public static bool IsWithinBoundsOf<T>(this int value, IReadOnlyList<T> list)
    {
        return value >= 0 && value < list.Count;
    }
}
