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
                .Let(Person.Lens.Name.Set, "Bob")
                .Let(Person.Lens.Age.Set, 35);

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
}
