using Macaron.Optics.Generator;

namespace Macaron.Optics.Tests;

[TestFixture]
public class GenerationModelComparerTests
{
    [Test]
    public void TypeComparer_When_ModelsAreStructurallyEqual_Should_EqualAndHaveSameHashCode()
    {
        var x = CreatePersonType();
        var y = CreatePersonType();
        var comparer = TypeGenerationModelComparer.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(comparer.Equals(x, x), Is.True);
            Assert.That(comparer.Equals(x, y), Is.True);
            Assert.That(comparer.GetHashCode(x), Is.EqualTo(comparer.GetHashCode(y)));
        });
    }

    [Test]
    public void TypeComparer_When_OutputRelevantValuesDiffer_Should_NotEqual()
    {
        var baseline = CreatePersonType();
        var comparer = TypeGenerationModelComparer.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(
                comparer.Equals(
                    baseline,
                    baseline with { FullyQualifiedName = "global::Example.OtherPerson" }
                ),
                Is.False
            );
            Assert.That(
                comparer.Equals(
                    baseline,
                    baseline with
                    {
                        Members = [new MemberGenerationModel(
                            Name: "Name",
                            TypeName: "global::System.String",
                            IsNullable: true
                        )],
                    }
                ),
                Is.False
            );
            Assert.That(comparer.Equals(baseline, null), Is.False);
        });
    }

    [Test]
    public void OfComparer_When_ModelsAreStructurallyEqual_Should_EqualAndHaveSameHashCode()
    {
        var x = new OfGenerationModel(
            LensTypes: [CreatePersonType()],
            OptionalTypes: [CreateAddressType()]
        );
        var y = new OfGenerationModel(
            LensTypes: [CreatePersonType()],
            OptionalTypes: [CreateAddressType()]
        );
        var comparer = OfGenerationModelComparer.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(comparer.Equals(x, x), Is.True);
            Assert.That(comparer.Equals(x, y), Is.True);
            Assert.That(comparer.GetHashCode(x), Is.EqualTo(comparer.GetHashCode(y)));
        });
    }

    [Test]
    public void OfComparer_When_OutputRelevantValuesDiffer_Should_NotEqual()
    {
        var baseline = new OfGenerationModel(
            LensTypes: [CreatePersonType()],
            OptionalTypes: [CreateAddressType()]
        );
        var changedFullyQualifiedName = baseline with
        {
            LensTypes = [CreatePersonType() with
            {
                FullyQualifiedName = "global::Example.OtherPerson",
            }],
        };
        var changedMember = baseline with
        {
            LensTypes = [
                CreatePersonType() with
                {
                    Members = [new MemberGenerationModel(
                        Name: "Name",
                        TypeName: "global::System.String",
                        IsNullable: true
                    )],
                },
            ],
        };
        var changedKind = new OfGenerationModel(
            LensTypes: baseline.OptionalTypes,
            OptionalTypes: baseline.LensTypes
        );
        var comparer = OfGenerationModelComparer.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(comparer.Equals(baseline, changedFullyQualifiedName), Is.False);
            Assert.That(comparer.Equals(baseline, changedMember), Is.False);
            Assert.That(comparer.Equals(baseline, changedKind), Is.False);
            Assert.That(comparer.Equals(baseline, null), Is.False);
        });
    }

    [Test]
    public void AttributeComparer_When_ModelsAreStructurallyEqual_Should_EqualAndHaveSameHashCode()
    {
        var x = CreateAttributeModel();
        var y = CreateAttributeModel();
        var comparer = AttributeGenerationModelComparer.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(comparer.Equals(x, x), Is.True);
            Assert.That(comparer.Equals(x, y), Is.True);
            Assert.That(comparer.GetHashCode(x), Is.EqualTo(comparer.GetHashCode(y)));
        });
    }

    [Test]
    public void AttributeComparer_When_OutputRelevantValuesDiffer_Should_NotEqual()
    {
        var baseline = CreateAttributeModel();
        var comparer = AttributeGenerationModelComparer.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(
                comparer.Equals(baseline, baseline with { Kind = OpticsKind.Optional }),
                Is.False
            );
            Assert.That(
                comparer.Equals(baseline, baseline with { NamespaceName = "Example.Other" }),
                Is.False
            );
            Assert.That(
                comparer.Equals(
                    baseline,
                    baseline with { TypeDeclarations = ["partial class OtherLens"] }
                ),
                Is.False
            );
            Assert.That(
                comparer.Equals(baseline, baseline with { HintName = "OtherLens.g.cs" }),
                Is.False
            );
            Assert.That(
                comparer.Equals(baseline, baseline with { TargetType = CreateAddressType() }),
                Is.False
            );
            Assert.That(comparer.Equals(baseline, null), Is.False);
        });
    }

    private static AttributeGenerationModel CreateAttributeModel()
    {
        return new AttributeGenerationModel(
            Kind: OpticsKind.Lens,
            NamespaceName: "Example",
            TypeDeclarations: ["partial class PersonLens"],
            HintName: "PersonLens.12345678.g.cs",
            TargetType: CreatePersonType()
        );
    }

    private static TypeGenerationModel CreatePersonType()
    {
        return new TypeGenerationModel(
            Name: "Person",
            Arity: 0,
            FullyQualifiedName: "global::Example.Person",
            Members: [new MemberGenerationModel("Name", "global::System.String", false)]
        );
    }

    private static TypeGenerationModel CreateAddressType()
    {
        return new TypeGenerationModel(
            Name: "Address",
            Arity: 0,
            FullyQualifiedName: "global::Example.Address",
            Members: [new MemberGenerationModel("City", "global::System.String", false)]
        );
    }
}
