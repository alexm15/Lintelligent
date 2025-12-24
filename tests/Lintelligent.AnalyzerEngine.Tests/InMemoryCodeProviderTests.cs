using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Tests.TestUtilities;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests;

public class InMemoryCodeProviderTests
{
    [Fact]
    public void Constructor_NullSources_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new InMemoryCodeProvider(null!));
        exception.ParamName.Should().Be("sources");
    }

    [Fact]
    public void GetSyntaxTrees_EmptyDictionary_ReturnsEmpty()
    {
        // Arrange
        var sources = new Dictionary<string, string>();
        var provider = new InMemoryCodeProvider(sources);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().BeEmpty();
    }

    [Fact]
    public void GetSyntaxTrees_SingleSource_ReturnsSingleTree()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Test.cs"] = "class Test { }"
        };
        var provider = new InMemoryCodeProvider(sources);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().ContainSingle();
        trees[0].FilePath.Should().Be("Test.cs");
        trees[0].ToString().Should().Contain("class Test");
    }

    [Fact]
    public void GetSyntaxTrees_MultipleSources_ReturnsAllTrees()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["File1.cs"] = "class File1 { }",
            ["File2.cs"] = "class File2 { }",
            ["File3.cs"] = "class File3 { }"
        };
        var provider = new InMemoryCodeProvider(sources);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().HaveCount(3);
        trees.Select(t => t.FilePath).Should().BeEquivalentTo("File1.cs", "File2.cs", "File3.cs");
    }

    [Fact]
    public void GetSyntaxTrees_PreservesFilePaths()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["src/Controllers/HomeController.cs"] = "class HomeController { }",
            ["tests/UnitTest1.cs"] = "class UnitTest1 { }"
        };
        var provider = new InMemoryCodeProvider(sources);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees[0].FilePath.Should().Be("src/Controllers/HomeController.cs");
        trees[1].FilePath.Should().Be("tests/UnitTest1.cs");
    }

    [Fact]
    public void GetSyntaxTrees_ParsesValidCSharp()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Complex.cs"] = """

                             using System;

                             namespace TestNamespace
                             {
                                 public class ComplexClass
                                 {
                                     public void Method()
                                     {
                                         Console.WriteLine("Hello");
                                     }
                                 }
                             }
                             """
        };
        var provider = new InMemoryCodeProvider(sources);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().ContainSingle();
        var tree = trees[0];
        tree.GetRoot().Should().NotBeNull();
        tree.ToString().Should().Contain("ComplexClass");
        tree.ToString().Should().Contain("Method");
    }

    [Fact]
    public void GetSyntaxTrees_WithAnalyzerEngine_ProducesResults()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Test1.cs"] = "class Test1 { }",
            ["Test2.cs"] = "class Test2 { }"
        };
        var provider = new InMemoryCodeProvider(sources);

        var manager = new AnalyzerManager();
        manager.RegisterRule(new AlwaysReportRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var results = engine.Analyze(provider.GetSyntaxTrees()).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.RuleId == "TEST001");
        results.Select(r => r.FilePath).Should().BeEquivalentTo("Test1.cs", "Test2.cs");
    }

    [Fact]
    public void GetSyntaxTrees_InMemoryContentDiffersFromDisk_AnalyzesInMemoryContent()
    {
        // Arrange - Simulate IDE scenario where editor buffer differs from saved file
        var sources = new Dictionary<string, string>
        {
            // Pretend this represents unsaved editor content
            ["Unsaved.cs"] = "class ModifiedInEditor { void NewMethod() { } }"
        };
        var provider = new InMemoryCodeProvider(sources);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert - Verify we get the in-memory content, not disk content
        trees.Should().ContainSingle();
        trees[0].ToString().Should().Contain("ModifiedInEditor");
        trees[0].ToString().Should().Contain("NewMethod");
        trees[0].FilePath.Should().Be("Unsaved.cs");
    }

    [Fact]
    public void GetSyntaxTrees_MultipleEnumerations_ReturnsFreshTrees()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Test.cs"] = "class Test { }"
        };
        var provider = new InMemoryCodeProvider(sources);

        // Act - Enumerate twice
        var trees1 = provider.GetSyntaxTrees().ToList();
        var trees2 = provider.GetSyntaxTrees().ToList();

        // Assert - Both enumerations should succeed
        trees1.Should().ContainSingle();
        trees2.Should().ContainSingle();

        // Trees should be separate instances (not cached)
        trees1[0].Should().NotBeSameAs(trees2[0]);
    }

    [Fact]
    public void GetSyntaxTrees_LazyEvaluation_DoesNotParseUntilEnumerated()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Test.cs"] = "class Test { }"
        };
        var provider = new InMemoryCodeProvider(sources);

        // Act - Call GetSyntaxTrees but don't enumerate
        var enumerable = provider.GetSyntaxTrees();

        // Assert - Should not throw even if not yet materialized
        enumerable.Should().NotBeNull();

        // Now materialize and verify
        var trees = enumerable.ToList();
        trees.Should().ContainSingle();
    }

    private sealed class AlwaysReportRule : IAnalyzerRule
    {
        public string Id => "TEST001";
        public string Description => "Always reports for testing";
        public Severity Severity => Severity.Warning;
        public string Category => DiagnosticCategories.General;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            yield return new DiagnosticResult(tree.FilePath, Id, Description, 1, Severity, Category);
        }
    }
}