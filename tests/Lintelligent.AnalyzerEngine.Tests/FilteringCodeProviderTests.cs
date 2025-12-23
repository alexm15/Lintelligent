using Xunit;
using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Tests.TestUtilities;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;

namespace Lintelligent.AnalyzerEngine.Tests;

public class FilteringCodeProviderTests
{
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

    [Fact]
    public void Constructor_NullInnerProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new FilteringCodeProvider(null!, _ => true));
        exception.ParamName.Should().Be("innerProvider");
    }

    [Fact]
    public void Constructor_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var sources = new Dictionary<string, string> { ["Test.cs"] = "class Test { }" };
        var innerProvider = new InMemoryCodeProvider(sources);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new FilteringCodeProvider(innerProvider, null!));
        exception.ParamName.Should().Be("predicate");
    }

    [Fact]
    public void GetSyntaxTrees_PredicateAlwaysTrue_ReturnsAllTrees()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["File1.cs"] = "class File1 { }",
            ["File2.cs"] = "class File2 { }",
            ["File3.cs"] = "class File3 { }"
        };
        var innerProvider = new InMemoryCodeProvider(sources);
        var filter = new FilteringCodeProvider(innerProvider, _ => true);

        // Act
        var trees = filter.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().HaveCount(3);
    }

    [Fact]
    public void GetSyntaxTrees_PredicateAlwaysFalse_ReturnsEmpty()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["File1.cs"] = "class File1 { }",
            ["File2.cs"] = "class File2 { }"
        };
        var innerProvider = new InMemoryCodeProvider(sources);
        var filter = new FilteringCodeProvider(innerProvider, _ => false);

        // Act
        var trees = filter.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().BeEmpty();
    }

    [Fact]
    public void GetSyntaxTrees_FilterByFilePath_ReturnsMatchingOnly()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["src/Controllers/HomeController.cs"] = "class HomeController { }",
            ["src/Models/User.cs"] = "class User { }",
            ["tests/ControllerTests.cs"] = "class ControllerTests { }"
        };
        var innerProvider = new InMemoryCodeProvider(sources);
        
        // Filter: only files in Controllers directory
        var filter = new FilteringCodeProvider(
            innerProvider,
            tree => tree.FilePath.Contains("Controllers"));

        // Act
        var trees = filter.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().ContainSingle();
        trees[0].FilePath.Should().Contain("Controllers");
    }

    [Fact]
    public void GetSyntaxTrees_FilterByContent_ReturnsMatchingOnly()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["File1.cs"] = "public class PublicClass { }",
            ["File2.cs"] = "internal class InternalClass { }",
            ["File3.cs"] = "public interface IPublic { }"
        };
        var innerProvider = new InMemoryCodeProvider(sources);
        
        // Filter: only files containing "public class"
        var filter = new FilteringCodeProvider(
            innerProvider,
            tree => tree.ToString().Contains("public class"));

        // Act
        var trees = filter.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().ContainSingle();
        trees[0].ToString().Should().Contain("PublicClass");
    }

    [Fact]
    public void GetSyntaxTrees_NestedFiltering_AppliesMultipleFilters()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["src/Controllers/HomeController.cs"] = "public class HomeController { }",
            ["src/Controllers/ApiController.cs"] = "internal class ApiController { }",
            ["src/Models/User.cs"] = "public class User { }",
            ["tests/UnitTest1.cs"] = "public class UnitTest1 { }"
        };
        var innerProvider = new InMemoryCodeProvider(sources);
        
        // First filter: only Controllers
        var filter1 = new FilteringCodeProvider(
            innerProvider,
            tree => tree.FilePath.Contains("Controllers"));
        
        // Second filter: only public classes
        var filter2 = new FilteringCodeProvider(
            filter1,
            tree => tree.ToString().Contains("public class"));

        // Act
        var trees = filter2.GetSyntaxTrees().ToList();

        // Assert - Should only get HomeController (in Controllers AND public)
        trees.Should().ContainSingle();
        trees[0].FilePath.Should().Contain("HomeController");
    }

    [Fact]
    public void GetSyntaxTrees_WithAnalyzerEngine_OnlyAnalyzesFilteredFiles()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Include1.cs"] = "class Include1 { }",
            ["Exclude.cs"] = "class Exclude { }",
            ["Include2.cs"] = "class Include2 { }"
        };
        var innerProvider = new InMemoryCodeProvider(sources);
        
        // Filter: exclude files with "Exclude" in name
        var filter = new FilteringCodeProvider(
            innerProvider,
            tree => !tree.FilePath.Contains("Exclude"));

        var manager = new AnalyzerManager();
        manager.RegisterRule(new AlwaysReportRule());
        var engine = new Lintelligent.AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var results = engine.Analyze(filter.GetSyntaxTrees()).ToList();

        // Assert - Should only have results for Include1 and Include2
        results.Should().HaveCount(2);
        results.Select(r => r.FilePath).Should().BeEquivalentTo("Include1.cs", "Include2.cs");
        results.Should().NotContain(r => r.FilePath.Contains("Exclude"));
    }

    [Fact]
    public void GetSyntaxTrees_LazyEvaluation_InnerProviderNotEnumeratedUntilNeeded()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Test.cs"] = "class Test { }"
        };
        var innerProvider = new InMemoryCodeProvider(sources);
        var filter = new FilteringCodeProvider(innerProvider, _ => true);

        // Act - Call GetSyntaxTrees but don't enumerate
        var enumerable = filter.GetSyntaxTrees();

        // Assert - Should not throw
        enumerable.Should().NotBeNull();
        
        // Now materialize
        var trees = enumerable.ToList();
        trees.Should().ContainSingle();
    }

    [Fact]
    public void GetSyntaxTrees_MultipleEnumerations_EvaluatesSeparately()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Test1.cs"] = "class Test1 { }",
            ["Test2.cs"] = "class Test2 { }"
        };
        var innerProvider = new InMemoryCodeProvider(sources);
        var filter = new FilteringCodeProvider(innerProvider, tree => tree.FilePath.Contains("Test1"));

        // Act - Enumerate twice
        var trees1 = filter.GetSyntaxTrees().ToList();
        var trees2 = filter.GetSyntaxTrees().ToList();

        // Assert - Both should return same filtered results
        trees1.Should().ContainSingle();
        trees2.Should().ContainSingle();
        trees1[0].FilePath.Should().Be("Test1.cs");
        trees2[0].FilePath.Should().Be("Test1.cs");
    }

    [Fact]
    public void GetSyntaxTrees_ComplexPredicate_FiltersCorrectly()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["a_Test.cs"] = "class ATest { }",
            ["b_Test.cs"] = "class BTest { }",
            ["c_Prod.cs"] = "class CProd { }",
            ["d_Test.cs"] = "class DTest { }"
        };
        var innerProvider = new InMemoryCodeProvider(sources);
        
        // Complex predicate: starts with 'a' or 'b' AND ends with 'Test.cs'
        var filter = new FilteringCodeProvider(
            innerProvider,
            tree =>
            {
                var name = Path.GetFileName(tree.FilePath);
                return (name.StartsWith("a") || name.StartsWith("b")) && name.EndsWith("Test.cs");
            });

        // Act
        var trees = filter.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().HaveCount(2);
        trees.Select(t => Path.GetFileName(t.FilePath))
            .Should().BeEquivalentTo("a_Test.cs", "b_Test.cs");
    }
}
