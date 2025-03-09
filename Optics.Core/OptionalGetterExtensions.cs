using Macaron.Functional;

namespace Macaron.Optics;

public static class OptionalGetterExtensions
{
    public static OptionalGetter<T, TValue2> Compose<T, TValue1, TValue2>(
        this OptionalGetter<T, TValue1> optionalGetter1,
        OptionalGetter<Maybe<TValue1>, TValue2> optionalGetter2
    )
    {
        return OptionalGetter.Of<T, TValue2>(source =>
        {
            var value0 = source;
            var value1 = optionalGetter1.Get(value0);
            var value2 = optionalGetter2.Get(value1);

            return value2;
        });
    }
}
