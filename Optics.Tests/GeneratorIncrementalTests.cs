using System.Collections.Immutable;
using System.Reflection;
using Macaron.Optics.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Macaron.Optics.Tests;

[TestFixture]
public class GeneratorIncrementalTests
{
    [Test]
    public void LensGenerator_When_UnrelatedInvocationIsAdded_Should_CacheOutput()
    {
        const string sourceCode =
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name);

            public static class Usage
            {
                public static void Use()
                {
                    _ = Lens.Of<Person>();
                }
            }
            """;
        const string unrelatedSourceCode =
            """

            public static class Unrelated
            {
                public static void Call()
                {
                    _ = new object().ToString();
                }
            }
            """;

        var result = RunAfterAppendingSource<LensGenerator>(
            sourceCode,
            unrelatedSourceCode,
            typeof(LensOf<>).Assembly
        );

        AssertOutputWasCached(result);
    }

    [Test]
    public void LensGenerator_When_DuplicateRequestIsAdded_Should_CacheOutput()
    {
        const string sourceCode =
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name);

            public static class Usage
            {
                public static void Use()
                {
                    _ = Lens.Of<Person>();
                }
            }
            """;
        const string duplicateRequestSourceCode =
            """

            public static class AnotherUsage
            {
                public static void Use()
                {
                    _ = Lens.Of<Person>();
                }
            }
            """;

        var result = RunAfterAppendingSource<LensGenerator>(
            sourceCode,
            duplicateRequestSourceCode,
            typeof(LensOf<>).Assembly
        );

        AssertOutputWasCached(result);
    }

    [Test]
    public void LensGenerator_When_RequestOrderChanges_Should_CacheOutput()
    {
        const string sourceCode =
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name);
            public partial record Address(string City);

            public static class Usage
            {
                public static void Use()
                {
                    _ = Lens.Of<Person>();
                    _ = Lens.Of<Address>();
                }
            }
            """;
        const string updatedSourceCode =
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name);
            public partial record Address(string City);

            public static class Usage
            {
                public static void Use()
                {
                    _ = Lens.Of<Address>();
                    _ = Lens.Of<Person>();
                }
            }
            """;

        var result = RunAfterSourceChange<LensGenerator>(
            sourceCode,
            updatedSourceCode,
            typeof(LensOf<>).Assembly
        );

        AssertOutputWasCached(result);
    }

    [Test]
    public void LensGenerator_When_TargetMemberIsAdded_Should_RegenerateOutput()
    {
        const string sourceCode =
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name);

            public static class Usage
            {
                public static void Use()
                {
                    _ = Lens.Of<Person>();
                }
            }
            """;
        const string addedMemberSourceCode =
            """

            public partial record Person
            {
                public int Age { get; init; }
            }
            """;

        var result = RunAfterAppendingSource<LensGenerator>(
            sourceCode,
            addedMemberSourceCode,
            typeof(LensOf<>).Assembly
        );
        var outputReasons = GetOutputReasons(result);
        var generatedCode = result.GeneratedSources.Single().SourceText.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Exception, Is.Null);
            Assert.That(outputReasons, Has.Some.Not.EqualTo(IncrementalStepRunReason.Cached));
            Assert.That(generatedCode, Does.Contain(" Age("));
        });
    }

    [Test]
    public void LensGenerator_When_OneTargetChanges_Should_CacheOtherTypeModel()
    {
        const string sourceCode =
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name);
            public partial record Address(string City);

            public static class Usage
            {
                public static void Use()
                {
                    _ = Lens.Of<Person>();
                    _ = Lens.Of<Address>();
                }
            }
            """;
        const string addedMemberSourceCode =
            """

            public partial record Person
            {
                public int Age { get; init; }
            }
            """;

        var result = RunAfterAppendingSource<LensGenerator>(
            sourceCode,
            addedMemberSourceCode,
            typeof(LensOf<>).Assembly
        );
        var typeOutputs = result
            .TrackedSteps["LensTypeGenerationModel"]
            .SelectMany(static step => step.Outputs)
            .Select(static output => (
                Model: (TypeGenerationModel)output.Value,
                output.Reason
            ))
            .ToImmutableArray();
        var addressOutput = typeOutputs.Single(static output =>
            output.Model.FullyQualifiedName.EndsWith(".Address", StringComparison.Ordinal)
        );
        var personOutput = typeOutputs.Single(static output =>
            output.Model.FullyQualifiedName.EndsWith(".Person", StringComparison.Ordinal)
        );
        var addressSource = result.GeneratedSources.Single(static source =>
            source.SourceText.ToString().Contains("Address")
        );
        var personSource = result.GeneratedSources.Single(static source =>
            source.SourceText.ToString().Contains("Person")
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Exception, Is.Null);
            Assert.That(result.GeneratedSources, Has.Length.EqualTo(2));
            Assert.That(addressOutput.Reason, Is.EqualTo(IncrementalStepRunReason.Unchanged));
            Assert.That(personOutput.Reason, Is.EqualTo(IncrementalStepRunReason.Modified));
            Assert.That(
                addressSource.HintName,
                Does.StartWith("LensOfExtensions.Address_0.").And.EndWith(".g.cs")
            );
            Assert.That(
                personSource.HintName,
                Does.StartWith("LensOfExtensions.Person_0.").And.EndWith(".g.cs")
            );
            Assert.That(personSource.SourceText.ToString(), Does.Contain(" Age("));
        });
    }

    [Test]
    public void LensGenerator_When_SimpleTypeNamesMatch_Should_GenerateDistinctHintNames()
    {
        const string sourceCode =
            """
            using Macaron.Optics;

            namespace First
            {
                public record Person(string Name);
            }

            namespace Second
            {
                public record Person(string Name);
            }

            public static class Usage
            {
                public static void Use()
                {
                    _ = Lens.Of<First.Person>();
                    _ = Lens.Of<Second.Person>();
                }
            }
            """;

        var result = RunAfterSourceChange<LensGenerator>(
            sourceCode,
            sourceCode,
            typeof(LensOf<>).Assembly
        );
        var hintNames = result.GeneratedSources
            .Select(static source => source.HintName)
            .ToImmutableArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Exception, Is.Null);
            Assert.That(hintNames, Has.Length.EqualTo(2));
            Assert.That(hintNames, Is.Unique);
            Assert.That(
                hintNames,
                Is.All.StartsWith("LensOfExtensions.Person_0.")
                    .And.All.EndsWith(".g.cs")
            );
        });
    }

    [Test]
    public void LensOfGenerator_When_UnrelatedClassIsAdded_Should_CacheOutput()
    {
        const string sourceCode =
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name)
            {
                [LensOf]
                public static partial class Lens;
            }
            """;
        const string unrelatedSourceCode =
            """

            public class Unrelated;
            """;

        var result = RunAfterAppendingSource<LensOfGenerator>(
            sourceCode,
            unrelatedSourceCode,
            typeof(LensOf<>).Assembly,
            typeof(LensOfAttribute).Assembly
        );

        AssertOutputWasCached(result);
    }

    private static void AssertOutputWasCached(GeneratorRunResult result)
    {
        var outputReasons = GetOutputReasons(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.Exception, Is.Null);
            Assert.That(result.GeneratedSources, Is.Not.Empty);
            Assert.That(outputReasons, Is.Not.Empty);
            Assert.That(
                outputReasons,
                Is.All.EqualTo(IncrementalStepRunReason.Cached),
                $"Expected cached generator output, but observed: {string.Join(", ", outputReasons)}"
            );
        });
    }

    private static ImmutableArray<IncrementalStepRunReason> GetOutputReasons(GeneratorRunResult result)
    {
        return result
            .TrackedOutputSteps
            .SelectMany(static pair => pair.Value)
            .SelectMany(static step => step.Outputs)
            .Select(static output => output.Reason)
            .ToImmutableArray();
    }

    private static GeneratorRunResult RunAfterAppendingSource<TGenerator>(
        string sourceCode,
        string appendedSourceCode,
        params Assembly[] additionalAssemblies
    ) where TGenerator : IIncrementalGenerator, new()
    {
        return RunAfterSourceChange<TGenerator>(
            sourceCode,
            sourceCode + appendedSourceCode,
            additionalAssemblies
        );
    }

    private static GeneratorRunResult RunAfterSourceChange<TGenerator>(
        string sourceCode,
        string updatedSourceCode,
        params Assembly[] additionalAssemblies
    ) where TGenerator : IIncrementalGenerator, new()
    {
        var references = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Concat(additionalAssemblies)
            .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .ToImmutableArray();
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Macaron.Optics.IncrementalTests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable
            )
        );
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new TGenerator().AsSourceGenerator()],
            additionalTexts: [],
            parseOptions: null,
            optionsProvider: null,
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        );

        driver = driver.RunGenerators(compilation);

        var updatedSyntaxTree = syntaxTree.WithChangedText(SourceText.From(updatedSourceCode));
        var updatedCompilation = compilation.ReplaceSyntaxTree(syntaxTree, updatedSyntaxTree);

        driver = driver.RunGenerators(updatedCompilation);

        return driver.GetRunResult().Results.Single();
    }
}
