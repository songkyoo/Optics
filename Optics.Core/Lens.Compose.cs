// <auto-generated />
namespace Macaron.Optics;

partial class Lens
{
    public static Lens<T, TValue2> Compose<T, TValue1, TValue2>(
        Lens<T, TValue1> lens1,
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

    public static Lens<T, TValue3> Compose<T, TValue1, TValue2, TValue3>(
        Lens<T, TValue1> lens1,
        Lens<TValue1, TValue2> lens2,
        Lens<TValue2, TValue3> lens3
    )
    {
        return Lens<T, TValue3>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);

                return value3;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);

                var newValue2 = lens3.Set(value2, value);
                var newValue1 = lens2.Set(value1, newValue2);
                var newValue0 = lens1.Set(value0, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue4> Compose<T, TValue1, TValue2, TValue3, TValue4>(
        Lens<T, TValue1> lens1,
        Lens<TValue1, TValue2> lens2,
        Lens<TValue2, TValue3> lens3,
        Lens<TValue3, TValue4> lens4
    )
    {
        return Lens<T, TValue4>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);
                var value4 = lens4.Get(value3);

                return value4;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);

                var newValue3 = lens4.Set(value3, value);
                var newValue2 = lens3.Set(value2, newValue3);
                var newValue1 = lens2.Set(value1, newValue2);
                var newValue0 = lens1.Set(value0, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue5> Compose<T, TValue1, TValue2, TValue3, TValue4, TValue5>(
        Lens<T, TValue1> lens1,
        Lens<TValue1, TValue2> lens2,
        Lens<TValue2, TValue3> lens3,
        Lens<TValue3, TValue4> lens4,
        Lens<TValue4, TValue5> lens5
    )
    {
        return Lens<T, TValue5>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);
                var value4 = lens4.Get(value3);
                var value5 = lens5.Get(value4);

                return value5;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);
                var value4 = lens4.Get(value3);

                var newValue4 = lens5.Set(value4, value);
                var newValue3 = lens4.Set(value3, newValue4);
                var newValue2 = lens3.Set(value2, newValue3);
                var newValue1 = lens2.Set(value1, newValue2);
                var newValue0 = lens1.Set(value0, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue6> Compose<T, TValue1, TValue2, TValue3, TValue4, TValue5, TValue6>(
        Lens<T, TValue1> lens1,
        Lens<TValue1, TValue2> lens2,
        Lens<TValue2, TValue3> lens3,
        Lens<TValue3, TValue4> lens4,
        Lens<TValue4, TValue5> lens5,
        Lens<TValue5, TValue6> lens6
    )
    {
        return Lens<T, TValue6>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);
                var value4 = lens4.Get(value3);
                var value5 = lens5.Get(value4);
                var value6 = lens6.Get(value5);

                return value6;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);
                var value4 = lens4.Get(value3);
                var value5 = lens5.Get(value4);

                var newValue5 = lens6.Set(value5, value);
                var newValue4 = lens5.Set(value4, newValue5);
                var newValue3 = lens4.Set(value3, newValue4);
                var newValue2 = lens3.Set(value2, newValue3);
                var newValue1 = lens2.Set(value1, newValue2);
                var newValue0 = lens1.Set(value0, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue7> Compose<T, TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7>(
        Lens<T, TValue1> lens1,
        Lens<TValue1, TValue2> lens2,
        Lens<TValue2, TValue3> lens3,
        Lens<TValue3, TValue4> lens4,
        Lens<TValue4, TValue5> lens5,
        Lens<TValue5, TValue6> lens6,
        Lens<TValue6, TValue7> lens7
    )
    {
        return Lens<T, TValue7>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);
                var value4 = lens4.Get(value3);
                var value5 = lens5.Get(value4);
                var value6 = lens6.Get(value5);
                var value7 = lens7.Get(value6);

                return value7;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);
                var value4 = lens4.Get(value3);
                var value5 = lens5.Get(value4);
                var value6 = lens6.Get(value5);

                var newValue6 = lens7.Set(value6, value);
                var newValue5 = lens6.Set(value5, newValue6);
                var newValue4 = lens5.Set(value4, newValue5);
                var newValue3 = lens4.Set(value3, newValue4);
                var newValue2 = lens3.Set(value2, newValue3);
                var newValue1 = lens2.Set(value1, newValue2);
                var newValue0 = lens1.Set(value0, newValue1);

                return newValue0;
            }
        );
    }

    public static Lens<T, TValue8> Compose<T, TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TValue8>(
        Lens<T, TValue1> lens1,
        Lens<TValue1, TValue2> lens2,
        Lens<TValue2, TValue3> lens3,
        Lens<TValue3, TValue4> lens4,
        Lens<TValue4, TValue5> lens5,
        Lens<TValue5, TValue6> lens6,
        Lens<TValue6, TValue7> lens7,
        Lens<TValue7, TValue8> lens8
    )
    {
        return Lens<T, TValue8>.Of(
            getter: source =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);
                var value4 = lens4.Get(value3);
                var value5 = lens5.Get(value4);
                var value6 = lens6.Get(value5);
                var value7 = lens7.Get(value6);
                var value8 = lens8.Get(value7);

                return value8;
            },
            setter: (source, value) =>
            {
                var value0 = source;
                var value1 = lens1.Get(value0);
                var value2 = lens2.Get(value1);
                var value3 = lens3.Get(value2);
                var value4 = lens4.Get(value3);
                var value5 = lens5.Get(value4);
                var value6 = lens6.Get(value5);
                var value7 = lens7.Get(value6);

                var newValue7 = lens8.Set(value7, value);
                var newValue6 = lens7.Set(value6, newValue7);
                var newValue5 = lens6.Set(value5, newValue6);
                var newValue4 = lens5.Set(value4, newValue5);
                var newValue3 = lens4.Set(value3, newValue4);
                var newValue2 = lens3.Set(value2, newValue3);
                var newValue1 = lens2.Set(value1, newValue2);
                var newValue0 = lens1.Set(value0, newValue1);

                return newValue0;
            }
        );
    }

}
