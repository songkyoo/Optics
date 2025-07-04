﻿using System.Collections.Immutable;
using System.Reflection;
using Macaron.Optics.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Macaron.Optics.Tests;

[TestFixture]
public class LensGeneratorTests
{
    private static void AssertGeneratedCode(string sourceCode, string expected)
    {
        var (_, generatedCode) = CompileAndGetResults<LensGenerator>(
            sourceCode,
            skipGeneratedCodeCount: 0,
            additionalAssemblies: [typeof(LensOf<>).Assembly]
        );

        Assert.That(generatedCode.ReplaceLineEndings(), Is.EqualTo(expected.ReplaceLineEndings()));
    }

    private static void AssertDiagnostic(string sourceCode, string expectedDiagnosticId)
    {
        var (diagnostics, _) = CompileAndGetResults<LensGenerator>(
            sourceCode,
            skipGeneratedCodeCount: 0,
            additionalAssemblies: [typeof(LensOf<>).Assembly]
        );

        var actualDiagnosticIds = diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.Id)
            .ToArray();

        Assert.That(actualDiagnosticIds, Has.Some.Matches(expectedDiagnosticId));
    }

    private static (ImmutableArray<Diagnostic> diagnostics, string generatedCode) CompileAndGetResults<T>(
        string sourceCode,
        int skipGeneratedCodeCount,
        Assembly[]? additionalAssemblies = null
    ) where T : IIncrementalGenerator, new()
    {
        var references = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Concat(additionalAssemblies ?? [])
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .ToImmutableArray();

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Macaron.Optics.Tests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable
            )
        );

        var generator = new T();
        var driver = CSharpGeneratorDriver.Create(generator);

        var result = driver.RunGenerators(compilation).GetRunResult().Results.Single();
        var generatedSources = result.GeneratedSources;
        var generatedCode = generatedSources.Length > skipGeneratedCodeCount
            ? generatedSources[skipGeneratedCodeCount].SourceText.ToString()
            : "";

        var allDiagnostics = compilation.GetDiagnostics()
            .Concat(result.Diagnostics)
            .ToImmutableArray();

        return (allDiagnostics, generatedCode);
    }

    [Test]
    public void When_LensOfTypeUsed_Should_GenerateExtensionMethods()
    {
        AssertGeneratedCode(
            sourceCode:
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name, int Age);

            public class TestClass
            {
                public void TestMethod()
                {
                    var lensOf = Lens.Of<Person>();
                }
            }
            """,
            expected:
            """
            // <auto-generated />
            #nullable enable

            namespace Macaron.Optics
            {
                internal static class LensOfExtensions
                {
                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.String> Name(
                        this LensOf<global::Macaron.Optics.Tests.Person> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.String>.Of(
                            getter: static source => source.Name,
                            setter: static (source, value) => source with
                            {
                                Name = value,
                            }
                        );
                    }

                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.Int32> Age(
                        this LensOf<global::Macaron.Optics.Tests.Person> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.Int32>.Of(
                            getter: static source => source.Age,
                            setter: static (source, value) => source with
                            {
                                Age = value,
                            }
                        );
                    }
                }
            }

            """
        );
    }

    [Test]
    public void When_OptionalOfTypeUsed_Should_GenerateExtensionMethods()
    {
        AssertGeneratedCode(
            sourceCode:
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name, int Age);

            public class TestClass
            {
                public void TestMethod()
                {
                    var optionalOf = Optional.Of<Person>();
                }
            }
            """,
            expected:
            """
            // <auto-generated />
            #nullable enable

            namespace Macaron.Optics
            {
                internal static class OptionalOfExtensions
                {
                    public static global::Macaron.Optics.Optional<global::Macaron.Functional.Maybe<global::Macaron.Optics.Tests.Person>, global::System.String> Name(
                        this OptionalOf<global::Macaron.Optics.Tests.Person> optionalOf
                    )
                    {
                        return global::Macaron.Optics.Optional<global::Macaron.Functional.Maybe<global::Macaron.Optics.Tests.Person>, global::System.String>.Of(
                            optionalGetter: static source => source.IsJust
                                ? global::Macaron.Functional.Maybe.Just(source.Value.Name)
                                : global::Macaron.Functional.Maybe.Nothing<global::System.String>(),
                            setter: static (source, value) => source.IsJust
                                ? global::Macaron.Functional.Maybe.Just(source.Value with
                                {
                                    Name = value,
                                })
                                : global::Macaron.Functional.Maybe.Nothing<global::Macaron.Optics.Tests.Person>()
                        );
                    }

                    public static global::Macaron.Optics.Optional<global::Macaron.Functional.Maybe<global::Macaron.Optics.Tests.Person>, global::System.Int32> Age(
                        this OptionalOf<global::Macaron.Optics.Tests.Person> optionalOf
                    )
                    {
                        return global::Macaron.Optics.Optional<global::Macaron.Functional.Maybe<global::Macaron.Optics.Tests.Person>, global::System.Int32>.Of(
                            optionalGetter: static source => source.IsJust
                                ? global::Macaron.Functional.Maybe.Just(source.Value.Age)
                                : global::Macaron.Functional.Maybe.Nothing<global::System.Int32>(),
                            setter: static (source, value) => source.IsJust
                                ? global::Macaron.Functional.Maybe.Just(source.Value with
                                {
                                    Age = value,
                                })
                                : global::Macaron.Functional.Maybe.Nothing<global::Macaron.Optics.Tests.Person>()
                        );
                    }
                }
            }

            """
        );
    }

    [Test]
    public void When_MultipleTypesUsed_Should_GenerateForAllTypes()
    {
        AssertGeneratedCode(
            sourceCode:
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name, int Age);
            public partial record Address(string Street, string City);

            public class TestClass
            {
                public void TestMethod()
                {
                    var personLens = Lens.Of<Person>();
                    var addressLens = Lens.Of<Address>();
                }
            }
            """,
            expected:
            """
            // <auto-generated />
            #nullable enable

            namespace Macaron.Optics
            {
                internal static class LensOfExtensions
                {
                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.String> Name(
                        this LensOf<global::Macaron.Optics.Tests.Person> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.String>.Of(
                            getter: static source => source.Name,
                            setter: static (source, value) => source with
                            {
                                Name = value,
                            }
                        );
                    }

                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.Int32> Age(
                        this LensOf<global::Macaron.Optics.Tests.Person> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.Int32>.Of(
                            getter: static source => source.Age,
                            setter: static (source, value) => source with
                            {
                                Age = value,
                            }
                        );
                    }

                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Address, global::System.String> Street(
                        this LensOf<global::Macaron.Optics.Tests.Address> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Address, global::System.String>.Of(
                            getter: static source => source.Street,
                            setter: static (source, value) => source with
                            {
                                Street = value,
                            }
                        );
                    }

                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Address, global::System.String> City(
                        this LensOf<global::Macaron.Optics.Tests.Address> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Address, global::System.String>.Of(
                            getter: static source => source.City,
                            setter: static (source, value) => source with
                            {
                                City = value,
                            }
                        );
                    }
                }
            }

            """
        );
    }

    [Test]
    public void When_NullableTypeUsed_Should_ReportError()
    {
        AssertDiagnostic(
            sourceCode:
            """
            namespace Macaron.Optics.Tests;

            public class TestClass
            {
                public void TestMethod()
                {
                    var lensOf = Lens.Of<string?>();
                }
            }
            """,
            expectedDiagnosticId: "MOPT0001"
        );
    }

    [Test]
    public void When_InterfaceTypeUsed_Should_ReportError()
    {
        AssertDiagnostic(
            sourceCode:
            """
            namespace Macaron.Optics.Tests;

            public interface IPerson
            {
                string Name { get; }
            }

            public class TestClass
            {
                public void TestMethod()
                {
                    var lensOf = Lens.Of<IPerson>();
                }
            }
            """,
            expectedDiagnosticId: "MOPT0002"
        );
    }

    [Test]
    public void When_NormalClassTypeUsed_Should_ReportError()
    {
        AssertDiagnostic(
            sourceCode:
            """
            namespace Macaron.Optics.Tests;

            public class Person
            {
                public string Name { get; set; }
            }

            public class TestClass
            {
                public void TestMethod()
                {
                    var lensOf = Lens.Of<Person>();
                }
            }
            """,
            expectedDiagnosticId: "MOPT0002"
        );
    }

    [Test]
    public void When_NoValidLensOfCalls_Should_GenerateNothing()
    {
        AssertGeneratedCode(
            sourceCode:
            """
            namespace Macaron.Optics.Tests;

            public class TestClass
            {
                public void TestMethod()
                {
                    // No Lens.Of<T>() calls
                    var someValue = 42;
                }
            }
            """,
            expected: ""
        );
    }

    [Test]
    public void When_DuplicateTypesUsed_Should_GenerateOnlyOnce()
    {
        AssertGeneratedCode(
            sourceCode:
            """
            namespace Macaron.Optics.Tests;

            public partial record Person(string Name, int Age);

            public class TestClass
            {
                public void TestMethod1()
                {
                    var lensOf1 = Lens.Of<Person>();
                }

                public void TestMethod2()
                {
                    var lensOf2 = Lens.Of<Person>();
                }
            }
            """,
            expected:
            """
            // <auto-generated />
            #nullable enable

            namespace Macaron.Optics
            {
                internal static class LensOfExtensions
                {
                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.String> Name(
                        this LensOf<global::Macaron.Optics.Tests.Person> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.String>.Of(
                            getter: static source => source.Name,
                            setter: static (source, value) => source with
                            {
                                Name = value,
                            }
                        );
                    }

                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.Int32> Age(
                        this LensOf<global::Macaron.Optics.Tests.Person> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Person, global::System.Int32>.Of(
                            getter: static source => source.Age,
                            setter: static (source, value) => source with
                            {
                                Age = value,
                            }
                        );
                    }
                }
            }

            """
        );
    }

    [Test]
    public void When_StructWithNullableProperties_Should_GenerateWithMaybe()
    {
        AssertGeneratedCode(
            sourceCode:
            """
            namespace Macaron.Optics.Tests;

            public partial struct Point
            {
                public int X { get; set; }
                public int? Y { get; set; }
                public string? Label { get; set; }
            }

            public class TestClass
            {
                public void TestMethod()
                {
                    var lensOf = Lens.Of<Point>();
                }
            }
            """,
            expected:
            """
            // <auto-generated />
            #nullable enable

            namespace Macaron.Optics
            {
                internal static class LensOfExtensions
                {
                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Point, global::System.Int32> X(
                        this LensOf<global::Macaron.Optics.Tests.Point> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Point, global::System.Int32>.Of(
                            getter: static source => source.X,
                            setter: static (source, value) => source with
                            {
                                X = value,
                            }
                        );
                    }

                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Point, global::Macaron.Functional.Maybe<global::System.Int32>> Y(
                        this LensOf<global::Macaron.Optics.Tests.Point> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Point, global::Macaron.Functional.Maybe<global::System.Int32>>.Of(
                            getter: static source => source is { Y: { } value }
                                ? global::Macaron.Functional.Maybe.Just(value)
                                : global::Macaron.Functional.Maybe.Nothing<global::System.Int32>(),
                            setter: static (source, value) => source with
                            {
                                Y = value is { IsJust: true, Value: var value2 } ? value2 : null,
                            }
                        );
                    }

                    public static global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Point, global::Macaron.Functional.Maybe<global::System.String>> Label(
                        this LensOf<global::Macaron.Optics.Tests.Point> lensOf
                    )
                    {
                        return global::Macaron.Optics.Lens<global::Macaron.Optics.Tests.Point, global::Macaron.Functional.Maybe<global::System.String>>.Of(
                            getter: static source => source is { Label: { } value }
                                ? global::Macaron.Functional.Maybe.Just(value)
                                : global::Macaron.Functional.Maybe.Nothing<global::System.String>(),
                            setter: static (source, value) => source with
                            {
                                Label = value is { IsJust: true, Value: var value2 } ? value2 : null,
                            }
                        );
                    }
                }
            }

            """
        );
    }
}
