namespace Macaron.Optics;

public static class LensExtensions
{
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
}
