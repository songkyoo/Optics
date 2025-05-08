namespace Macaron.Optics.Tests;

public class LensOfTests
{
    public sealed record Person(string Name, int Age);

    [Fact]
    public void LensOf_WithRecordType_GenerateLensExtensionMethodsOfRecord()
    {
        var lensOfPerson = Lens.Of<Person>();
        var person = new Person("Alice", 7);

        Assert.Equal("Alice", lensOfPerson.Name().Get(person));
        Assert.Equal(7, lensOfPerson.Age().Get(person));
    }
}
