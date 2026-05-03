using System.Collections.Immutable;
using Macaron.Functional;

using static Macaron.Functional.Maybe;

namespace Macaron.Optics.Tests;

[TestFixture]
public partial class LensTests
{
    public partial record Person(string Name, int Age, Address? Address)
    {
        [LensOf]
        public static partial class Lens;
    }

    public partial record Address(string Street, string City)
    {
        [OptionalOf]
        public static partial class Optional;
    }

    public partial record struct Point(int X, int Y)
    {
        [LensOf]
        public static partial class Lens;
    }

    [Test]
    public void When_UsingGeneratedLenses_Should_GetAndSetCorrectly()
    {
        var person = new Person(Name: "Alice", Age: 30, Address: null);

        Assert.Multiple(() =>
        {
            var name = Person.Lens.Name.Get(person);
            var age = Person.Lens.Age.Get(person);

            Assert.That(name, Is.EqualTo("Alice"));
            Assert.That(age, Is.EqualTo(30));
        });

        Assert.Multiple(() =>
        {
            var updatedPerson = Person.Lens.Name.Set(person, "Bob");
            var updatedAge = Person.Lens.Age.Set(updatedPerson, 35);

            Assert.That(updatedPerson.Name, Is.EqualTo("Bob"));
            Assert.That(updatedPerson.Age, Is.EqualTo(30));
            Assert.That(updatedAge.Name, Is.EqualTo("Bob"));
            Assert.That(updatedAge.Age, Is.EqualTo(35));
        });
    }

    [Test]
    public void When_UsingNullableLens_Should_HandleMaybeCorrectly()
    {
        var personWithoutAddress = new Person(Name: "Alice", Age: 30, Address: null);
        var personWithAddress = new Person(
            Name: "Bob",
            Age: 35,
            Address: new Address(Street: "123 Main St", City: "Anytown")
        );

        Assert.Multiple(() =>
        {
            var addressMaybe1 = Person.Lens.Address.Get(personWithoutAddress);
            var addressMaybe2 = Person.Lens.Address.Get(personWithAddress);

            Assert.That(addressMaybe1.IsNothing, Is.True);
            Assert.That(addressMaybe2.IsJust, Is.True);
            Assert.That(addressMaybe2.Value, Is.EqualTo(new Address("123 Main St", "Anytown")));
        });

        Assert.Multiple(() =>
        {
            var newAddress = new Address("456 Oak Ave", "Springfield");
            var updatedPerson1 = Person.Lens.Address.Set(personWithoutAddress, Just(newAddress));
            var updatedPerson2 = Person.Lens.Address.Set(personWithAddress, Nothing<Address>());

            Assert.That(updatedPerson1.Address, Is.EqualTo(newAddress));
            Assert.That(updatedPerson2.Address, Is.Null);
        });
    }

    [Test]
    public void When_ComposingLenses_Should_AccessNestedProperties()
    {
        var address = new Address(Street: "123 Main St", City: "Anytown");
        var person = new Person(Name: "Alice", Age: 30, Address: address);

        Assert.Multiple(() =>
        {
            var nestedStreetLens = Person.Lens.Address.Compose(Address.Optional.Street).ToLens("123 Main St");

            var street = nestedStreetLens.Get(person);
            var updatedPerson = nestedStreetLens.Set(person, "456 Oak Ave");

            Assert.That(street, Is.EqualTo("123 Main St"));
            Assert.That(updatedPerson.Address!.Street, Is.EqualTo("456 Oak Ave"));
            Assert.That(updatedPerson.Address.City, Is.EqualTo("Anytown"));
        });
    }

    [Test]
    public void When_UsingLensModify_Should_ApplyFunction()
    {
        var person = new Person(Name: "alice", Age: 30, Address: null);

        Assert.Multiple(() =>
        {
            var updatedPerson = Person.Lens.Name.Modify(person, name => name.ToUpper());

            Assert.That(updatedPerson.Name, Is.EqualTo("ALICE"));
            Assert.That(updatedPerson.Age, Is.EqualTo(30));
        });
    }

    [Test]
    public void When_UsingRecordStructLenses_Should_WorkCorrectly()
    {
        var point = new Point(10, 20);

        Assert.Multiple(() =>
        {
            var x = Point.Lens.X.Get(point);
            var y = Point.Lens.Y.Get(point);

            Assert.That(x, Is.EqualTo(10));
            Assert.That(y, Is.EqualTo(20));
        });

        Assert.Multiple(() =>
        {
            var updatedPoint = Point.Lens.X.Set(point, 100);
            var finalPoint = Point.Lens.Y.Set(updatedPoint, 200);

            Assert.That(finalPoint.X, Is.EqualTo(100));
            Assert.That(finalPoint.Y, Is.EqualTo(200));
        });
    }

    [Test]
    public void When_UsingLensOfExtensions_Should_WorkWithFluentAPI()
    {
        var person = new Person(Name: "Alice", Age: 30, Address: null);
        var lensOf = Lens.Of<Person>();

        Assert.Multiple(() =>
        {
            var name = lensOf.Name().Get(person);
            var age = lensOf.Age().Get(person);

            Assert.That(name, Is.EqualTo("Alice"));
            Assert.That(age, Is.EqualTo(30));
        });

        Assert.Multiple(() =>
        {
            var updatedPerson = lensOf.Name().Set(person, "Bob");

            Assert.That(updatedPerson.Name, Is.EqualTo("Bob"));
            Assert.That(updatedPerson.Age, Is.EqualTo(30));
        });
    }

    [Test]
    public void When_ChainingLensOperations_Should_ComposeCorrectly()
    {
        var address = new Address(Street: "123 Main St", City: "Anytown");
        var person = new Person(Name: "Alice", Age: 30, Address: address);

        Assert.Multiple(() =>
        {
            var result = person
                .Let("Bob", Person.Lens.Name.Set)
                .Let(35, Person.Lens.Age.Set);

            Assert.That(result.Name, Is.EqualTo("Bob"));
            Assert.That(result.Age, Is.EqualTo(35));
            Assert.That(result.Address, Is.EqualTo(address));
        });
    }

    [Test]
    public void When_UsingOptionalWithNullableValues_Should_HandleGracefully()
    {
        var personWithoutAddress = new Person("Alice", 30, null);
        var optional = Optional.Of<Person>();

        Assert.Multiple(() =>
        {
            var streetOptional = optional.Address().Compose(Address.Optional.Street).ToLens(defaultValue: "Unknown");
            var street = streetOptional.ToGetter().Get(Just(personWithoutAddress));

            Assert.That(street, Is.EqualTo("Unknown"));
        });
    }

    [Test]
    public void When_ConvertingBetweenOpticsTypes_Should_WorkCorrectly()
    {
        var person = new Person("Alice", 30, null);

        Assert.Multiple(() =>
        {
            var nameGetter = Person.Lens.Name.ToGetter();
            var name = nameGetter.Get(person);

            Assert.That(name, Is.EqualTo("Alice"));
        });

        Assert.Multiple(() =>
        {
            var nameSetter = Person.Lens.Name.ToSetter();
            var updatedPerson = nameSetter.Set(person, "Bob");

            Assert.That(updatedPerson.Name, Is.EqualTo("Bob"));
        });

        Assert.Multiple(() =>
        {
            var nameOptional = Person.Lens.Name.ToOptional();
            var nameFromOptional = nameOptional.Get(person);

            Assert.That(nameFromOptional.IsJust, Is.True);
            Assert.That(nameFromOptional.Value, Is.EqualTo("Alice"));
        });

        Assert.Multiple(() =>
        {
            var nameConstructor = Constructor<Person, string>.Of(value => person with { Name = value });
            var lensPrism = Person.Lens.Name.ToPrism(nameConstructor);
            var lensIso = Person.Lens.Name.ToIso(value => person with { Name = value });
            var nameOptional = Optional<Person, string>.Of(
                optionalGetter: source => source.Name is { } name ? Just(name) : Nothing<string>(),
                setter: (source, value) => source with { Name = value }
            );
            var optionalPrism = nameOptional.ToPrism(nameConstructor);
            var optionalIso = nameOptional.ToIso(value => person with { Name = value }, defaultValue: "Unknown");

            Assert.That(lensPrism.Get(person).Value, Is.EqualTo("Alice"));
            Assert.That(lensPrism.Construct("Bob").Name, Is.EqualTo("Bob"));
            Assert.That(lensIso.Get(person), Is.EqualTo("Alice"));
            Assert.That(lensIso.Construct("Bob").Name, Is.EqualTo("Bob"));
            Assert.That(optionalPrism.Get(person).Value, Is.EqualTo("Alice"));
            Assert.That(optionalPrism.Construct("Bob").Name, Is.EqualTo("Bob"));
            Assert.That(optionalIso.Get(person with { Name = null! }), Is.EqualTo("Unknown"));
            Assert.That(optionalIso.Construct("Bob").Name, Is.EqualTo("Bob"));
        });
    }

    [Test]
    public void When_UsingImmutableCollections_Should_WorkWithExtensions()
    {
        Assert.Multiple(() =>
        {
            var dict = ImmutableDictionary<string, int>.Empty
                .Add("a", 1)
                .Add("b", 2);
            var dictLens = Lens<ImmutableDictionary<string, int>, ImmutableDictionary<string, int>>.Of(
                getter: x => x,
                setter: (_, value) => value
            );

            var keyOptional = dictLens.Key("a");
            var valueA = keyOptional.Get(dict);

            Assert.That(valueA.IsJust, Is.True);
            Assert.That(valueA.Value, Is.EqualTo(1));

            var updatedDict = keyOptional.Set(dict, 10);

            Assert.That(updatedDict["a"], Is.EqualTo(10));
        });

        Assert.Multiple(() =>
        {
            var list = ImmutableList<string>.Empty
                .Add("first")
                .Add("second");
            var listLens = Lens<ImmutableList<string>, ImmutableList<string>>.Of(
                getter: x => x,
                setter: (_, value) => value
            );

            var indexOptional = listLens.Index(0);
            var firstItem = indexOptional.Get(list);

            Assert.That(firstItem.IsJust, Is.True);
            Assert.That(firstItem.Value, Is.EqualTo("first"));

            var updatedList = indexOptional.Set(list, "updated");

            Assert.That(updatedList[0], Is.EqualTo("updated"));
        });
    }

    [Test]
    public void When_UsingLensCompose_Should_ChainCorrectly()
    {
        var address = new Address("123 Main St", "Anytown");
        var person = new Person("Alice", 30, address);

        Assert.Multiple(() =>
        {
            var composedLens = Person.Lens.Address.Compose(Address.Optional.Street).ToLens("123 Main St");

            var street = composedLens.Get(person);
            var updatedPerson = composedLens.Set(person, "456 Oak Ave");

            Assert.That(street, Is.EqualTo("123 Main St"));
            Assert.That(updatedPerson.Address!.Street, Is.EqualTo("456 Oak Ave"));
        });
    }

    [Test]
    public void When_ComposingNaturalOptics_Should_UseTheWeakestValidResultType()
    {
        var person = new Person("123", 30, new Address("Main", "Seoul"));
        var nameLens = Lens<Person, string>.Of(
            getter: source => source.Name,
            setter: (source, value) => source with { Name = value }
        );
        var addressOptional = Optional<Person, Address>.Of(
            optionalGetter: source => source.Address is { } address ? Just(address) : Nothing<Address>(),
            setter: (source, value) => source with { Address = value }
        );
        var streetLens = Lens<Address, string>.Of(
            getter: source => source.Street,
            setter: (source, value) => source with { Street = value }
        );
        var intPrism = Prism<string, int>.Of(
            optionalGetter: source => int.TryParse(source, out var value) ? Just(value) : Nothing<int>(),
            constructor: value => value.ToString()
        );
        var stringIso = Iso<Address, string>.Of(
            getter: source => source.Street,
            constructor: value => new Address(value, "Constructed")
        );
        var maybeStringOptional = Optional<Maybe<string>, int>.Of(
            optionalGetter: source => source.IsJust && int.TryParse(source.Value, out var value)
                ? Just(value)
                : Nothing<int>(),
            setter: (_, value) => Just(value.ToString())
        );

        Assert.Multiple(() =>
        {
            var lensThenPrism = nameLens.Compose(intPrism);
            var lensThenPrismWithDefault = nameLens.Compose(intPrism, defaultValue: -1);
            var lensThenMaybeOptional = nameLens.Compose(maybeStringOptional);
            var optionalThenLens = addressOptional.Compose(streetLens);
            var optionalThenPrismWithDefault = Optional<Person, string>.Of(
                optionalGetter: source => source.Name is { } name ? Just(name) : Nothing<string>(),
                setter: (source, value) => source with { Name = value }
            ).Compose(intPrism, defaultValue: -1);
            var optionalThenIso = addressOptional.Compose(stringIso);
            var optionalThenIsoWithDefault = addressOptional.Compose(stringIso, new Address("Default", "City"));

            Assert.That(lensThenPrism.Get(person).Value, Is.EqualTo(123));
            Assert.That(lensThenPrism.Set(person, 456).Name, Is.EqualTo("456"));
            Assert.That(lensThenPrismWithDefault.Get(person with { Name = "unknown" }), Is.EqualTo(-1));
            Assert.That(lensThenPrismWithDefault.Set(person, 456).Name, Is.EqualTo("456"));
            Assert.That(lensThenMaybeOptional.Get(person).Value, Is.EqualTo(123));
            Assert.That(lensThenMaybeOptional.Set(person, 654).Name, Is.EqualTo("654"));
            Assert.That(optionalThenLens.Get(person).Value, Is.EqualTo("Main"));
            Assert.That(optionalThenLens.Set(person, "Oak").Address!.Street, Is.EqualTo("Oak"));
            Assert.That(optionalThenPrismWithDefault.Get(person), Is.EqualTo(123));
            Assert.That(optionalThenPrismWithDefault.Get(person with { Name = "NaN" }), Is.EqualTo(-1));
            Assert.That(optionalThenPrismWithDefault.Get(person with { Name = null! }), Is.EqualTo(-1));
            Assert.That(optionalThenPrismWithDefault.Set(person, 777).Name, Is.EqualTo("777"));
            Assert.That(optionalThenPrismWithDefault.Set(person with { Name = null! }, 777).Name, Is.Null);
            Assert.That(optionalThenIso.Get(person).Value, Is.EqualTo("Main"));
            Assert.That(optionalThenIso.Set(person, "Pine").Address, Is.EqualTo(new Address("Pine", "Constructed")));
            Assert.That(optionalThenIsoWithDefault.Get(person with { Address = null }), Is.EqualTo("Default"));
            Assert.That(optionalThenIsoWithDefault.Set(person, "Birch").Address, Is.EqualTo(new Address("Birch", "Constructed")));
        });

        Assert.Multiple(() =>
        {
            var prismThenLensWithDefault = intPrism.Compose(
                Lens<int, bool>.Of(
                    getter: source => source > 0,
                    setter: (source, value) => value ? Math.Abs(source) : -Math.Abs(source)
                ),
                defaultValue: 1
            );
            var prismThenIso = intPrism.Compose(Iso<int, string>.Of(
                getter: source => source.ToString(),
                constructor: int.Parse
            ));
            var setterThenIso = nameLens.ToSetter().Compose(Iso<string, int>.Of(
                getter: int.Parse,
                constructor: value => value.ToString()
            ));
            var constructorThenPrism = Constructor<Person, string>
                .Of(value => person with { Name = value })
                .Compose(intPrism);

            Assert.That(prismThenLensWithDefault.Get("123"), Is.True);
            Assert.That(prismThenLensWithDefault.Get("unknown"), Is.True);
            Assert.That(prismThenLensWithDefault.Set("unknown", false), Is.EqualTo("-1"));
            Assert.That(prismThenIso.Get("123").Value, Is.EqualTo("123"));
            Assert.That(prismThenIso.Construct("456"), Is.EqualTo("456"));
            Assert.That(setterThenIso.Set(person, 789).Name, Is.EqualTo("789"));
            Assert.That(constructorThenPrism.Construct(321).Name, Is.EqualTo("321"));
        });
    }

    [Test]
    public void When_ComposingReadOnlyOpticsWithDefaults_Should_PromoteToGetter()
    {
        var person = new Person("123", 30, new Address("Main", "Seoul"));
        var nameGetter = Getter<Person, string>.Of(getter: source => source.Name);
        var addressOptionalGetter = OptionalGetter<Person, Address>.Of(
            optionalGetter: source => source.Address is { } address ? Just(address) : Nothing<Address>()
        );
        var streetLens = Lens<Address, string>.Of(
            getter: source => source.Street,
            setter: (source, value) => source with { Street = value }
        );
        var cityOptional = Optional<Address, string>.Of(
            optionalGetter: source => string.IsNullOrEmpty(source.City) ? Nothing<string>() : Just(source.City),
            setter: (source, value) => source with { City = value }
        );
        var intPrism = Prism<string, int>.Of(
            optionalGetter: source => int.TryParse(source, out var value) ? Just(value) : Nothing<int>(),
            constructor: value => value.ToString()
        );
        var streetLengthIso = Iso<string, int>.Of(
            getter: source => source.Length,
            constructor: value => new string('x', value)
        );

        Assert.Multiple(() =>
        {
            var getterThenOptional = Getter<Person, Address>.Of(getter: source => source.Address!)
                .Compose(cityOptional, defaultValue: "Unknown");
            var getterThenPrism = nameGetter.Compose(intPrism, defaultValue: -1);

            Assert.That(getterThenOptional.Get(person), Is.EqualTo("Seoul"));
            Assert.That(getterThenOptional.Get(person with { Address = new Address("Main", "") }), Is.EqualTo("Unknown"));
            Assert.That(getterThenPrism.Get(person), Is.EqualTo(123));
            Assert.That(getterThenPrism.Get(person with { Name = "NaN" }), Is.EqualTo(-1));
        });

        Assert.Multiple(() =>
        {
            var optionalGetterThenLens = addressOptionalGetter.Compose(streetLens, defaultValue: "Unknown");
            var optionalGetterThenOptional = addressOptionalGetter.Compose(cityOptional, defaultValue: "Unknown");
            var nameOptionalGetter = OptionalGetter<Person, string>.Of(
                optionalGetter: source => source.Name is { } name ? Just(name) : Nothing<string>()
            );
            var optionalGetterThenPrism = nameOptionalGetter
                .Compose(intPrism, defaultValue: -1);
            var optionalGetterThenIso = nameOptionalGetter
                .Compose(streetLengthIso, defaultValue: -1);

            Assert.That(optionalGetterThenLens.Get(person), Is.EqualTo("Main"));
            Assert.That(optionalGetterThenLens.Get(person with { Address = null }), Is.EqualTo("Unknown"));
            Assert.That(optionalGetterThenOptional.Get(person), Is.EqualTo("Seoul"));
            Assert.That(optionalGetterThenOptional.Get(person with { Address = null }), Is.EqualTo("Unknown"));
            Assert.That(optionalGetterThenPrism.Get(person), Is.EqualTo(123));
            Assert.That(optionalGetterThenPrism.Get(person with { Name = "NaN" }), Is.EqualTo(-1));
            Assert.That(optionalGetterThenIso.Get(person), Is.EqualTo(3));
            Assert.That(optionalGetterThenIso.Get(person with { Name = null! }), Is.EqualTo(-1));
        });
    }

    [Test]
    public void When_ComposingMaybeSourceOptionalWithDefaults_Should_TreatNothingAsValidInput()
    {
        var maybeIntOptional = Optional<Maybe<string>, int>.Of(
            optionalGetter: source => source.IsJust && int.TryParse(source.Value, out var value)
                ? Just(value)
                : Nothing<int>(),
            setter: (_, value) => Just(value.ToString())
        );
        var identityGetter = Getter<string, string>.Of(getter: source => source);
        var identityLens = Lens<string, string>.Of(
            getter: source => source,
            setter: (_, value) => value
        );
        var identityIso = Iso<string, string>.Of(
            getter: source => source,
            constructor: value => value
        );
        var optionalString = Optional<string, string>.Of(
            optionalGetter: source => string.IsNullOrEmpty(source) ? Nothing<string>() : Just(source),
            setter: (_, value) => value
        );
        var optionalStringGetter = OptionalGetter<string, string>.Of(
            optionalGetter: source => string.IsNullOrEmpty(source) ? Nothing<string>() : Just(source)
        );
        var intIdentityLens = Lens<int, int>.Of(
            getter: source => source,
            setter: (_, value) => value
        );
        var intStringIso = Iso<int, string>.Of(
            getter: source => source.ToString(),
            constructor: int.Parse
        );
        var rejectingMaybeIntOptional = Optional<Maybe<string>, int>.Of(
            optionalGetter: source => source.IsJust && int.TryParse(source.Value, out var value)
                ? Just(value)
                : Nothing<int>(),
            setter: (_, value) => value < 0 ? Nothing<string>() : Just(value.ToString())
        );
        var intPrism = Prism<string, int>.Of(
            optionalGetter: source => int.TryParse(source, out var value) ? Just(value) : Nothing<int>(),
            constructor: value => value.ToString()
        );
        var maybeIntStringOptional = Optional<Maybe<int>, string>.Of(
            optionalGetter: source => source.IsJust ? Just(source.Value.ToString()) : Nothing<string>(),
            setter: (_, value) => int.TryParse(value, out var parsed) ? Just(parsed) : Nothing<int>()
        );

        Assert.Multiple(() =>
        {
            var getter = identityGetter.Compose(maybeIntOptional, defaultValue: -1);
            var optionalGetter = optionalStringGetter.Compose(maybeIntOptional, defaultValue: -1);

            Assert.That(getter.Get("123"), Is.EqualTo(123));
            Assert.That(getter.Get("NaN"), Is.EqualTo(-1));
            Assert.That(optionalGetter.Get("123"), Is.EqualTo(123));
            Assert.That(optionalGetter.Get(""), Is.EqualTo(-1));
        });

        Assert.Multiple(() =>
        {
            var lens = identityLens.Compose(maybeIntOptional, defaultValue: -1);
            var iso = identityIso.Compose(maybeIntOptional, defaultValue: -1);
            var optional = optionalString.Compose(maybeIntOptional, defaultValue: -1);

            Assert.That(lens.Get("123"), Is.EqualTo(123));
            Assert.That(lens.Set("NaN", 456), Is.EqualTo("456"));
            Assert.That(iso.Get("123"), Is.EqualTo(123));
            Assert.That(iso.Set("NaN", 456), Is.EqualTo("456"));
            Assert.That(optional.Get("123"), Is.EqualTo(123));
            Assert.That(optional.Get(""), Is.EqualTo(-1));
            Assert.That(optional.Set("", 456), Is.EqualTo("456"));
        });

        Assert.Multiple(() =>
        {
            var specialLens = rejectingMaybeIntOptional.Compose(intIdentityLens, defaultValue: 0);
            var specialIso = maybeIntOptional.Compose(intStringIso, defaultValue: -1);
            var prism = intPrism.Compose(maybeIntStringOptional, defaultValue: "missing");

            Assert.That(specialLens.Get("123"), Is.EqualTo(123));
            Assert.That(specialLens.Get("NaN"), Is.EqualTo(0));
            Assert.That(specialLens.Set("123", -5), Is.EqualTo("123"));
            Assert.That(specialIso.Get("123"), Is.EqualTo("123"));
            Assert.That(specialIso.Get("NaN"), Is.EqualTo("-1"));
            Assert.That(specialIso.Set("123", "456"), Is.EqualTo("456"));
            Assert.That(prism.Get("123"), Is.EqualTo("123"));
            Assert.That(prism.Get("NaN"), Is.EqualTo("missing"));
            Assert.That(prism.Set("NaN", "456"), Is.EqualTo("456"));
            Assert.That(prism.Set("NaN", "oops"), Is.EqualTo("NaN"));
        });
    }
}
