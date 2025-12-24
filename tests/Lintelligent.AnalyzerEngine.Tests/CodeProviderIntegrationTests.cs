using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Tests.TestUtilities;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests;

/// <summary>
///     Integration tests demonstrating that AnalyzerEngine behavior is consistent
///     regardless of which ICodeProvider implementation is used.
/// </summary>
public class CodeProviderIntegrationTests
{
    [Fact]
    public void AnalyzerEngine_WithDifferentProviders_ProducesSameResults()
    {
        // Arrange - Same source code via different providers
        const string sourceCode = "class Problem { }";
        const string filePath = "Test.cs";

        // Provider 1: InMemoryCodeProvider
        var inMemoryProvider = new InMemoryCodeProvider(new Dictionary<string, string>
        {
            [filePath] = sourceCode
        });

        // Provider 2: Another InMemoryCodeProvider (simulating different source)
        var anotherProvider = new InMemoryCodeProvider(new Dictionary<string, string>
        {
            [filePath] = sourceCode
        });

        var manager = new AnalyzerManager();
        manager.RegisterRule(new TestRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var results1 = engine.Analyze(inMemoryProvider.GetSyntaxTrees()).ToList();
        var results2 = engine.Analyze(anotherProvider.GetSyntaxTrees()).ToList();

        // Assert - Results should be identical
        results1.Should().HaveCount(1);
        results2.Should().HaveCount(1);

        results1[0].RuleId.Should().Be(results2[0].RuleId);
        results1[0].FilePath.Should().Be(results2[0].FilePath);
        results1[0].Message.Should().Be(results2[0].Message);
        results1[0].LineNumber.Should().Be(results2[0].LineNumber);
    }

    [Fact]
    public void AnalyzerEngine_FilteredProvider_OnlyAnalyzesIncludedFiles()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Problem1.cs"] = "class Problem { }", // Should be analyzed
            ["Excluded.cs"] = "class Problem { }", // Should be excluded
            ["Problem2.cs"] = "class Problem { }" // Should be analyzed
        };

        var baseProvider = new InMemoryCodeProvider(sources);
        var filteredProvider = new FilteringCodeProvider(
            baseProvider,
            tree => !tree.FilePath.Contains("Excluded"));

        var manager = new AnalyzerManager();
        manager.RegisterRule(new TestRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var results = engine.Analyze(filteredProvider.GetSyntaxTrees()).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Select(r => r.FilePath).Should().BeEquivalentTo("Problem1.cs", "Problem2.cs");
        results.Should().NotContain(r => r.FilePath.Contains("Excluded"));
    }

    [Fact]
    public void AnalyzerEngine_ComposedProviders_WorkTogether()
    {
        // Arrange - Demonstrate provider composition
        var sources = new Dictionary<string, string>
        {
            ["src/Controllers/HomeController.cs"] = "class Problem { }",
            ["src/Controllers/ApiController.cs"] = "class Safe { }",
            ["src/Models/User.cs"] = "class Problem { }",
            ["tests/UnitTest1.cs"] = "class Problem { }"
        };

        var baseProvider = new InMemoryCodeProvider(sources);

        // Filter 1: Only src/ directory
        var srcOnly = new FilteringCodeProvider(
            baseProvider,
            tree => tree.FilePath.StartsWith("src/"));

        // Filter 2: Only Controllers
        var controllersOnly = new FilteringCodeProvider(
            srcOnly,
            tree => tree.FilePath.Contains("Controllers"));

        var manager = new AnalyzerManager();
        manager.RegisterRule(new TestRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var results = engine.Analyze(controllersOnly.GetSyntaxTrees()).ToList();

        // Assert - Should only analyze src/Controllers files
        results.Should().ContainSingle();
        results[0].FilePath.Should().Be("src/Controllers/HomeController.cs");
    }

    [Fact]
    public void AnalyzerEngine_EmptyProvider_ProducesNoResults()
    {
        // Arrange
        var emptyProvider = new InMemoryCodeProvider(new Dictionary<string, string>());

        var manager = new AnalyzerManager();
        manager.RegisterRule(new TestRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var results = engine.Analyze(emptyProvider.GetSyntaxTrees()).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void AnalyzerEngine_ProviderSwapping_AllowsDifferentSources()
    {
        // Arrange - Simulate IDE scenario where we analyze different content sources
        var savedFileProvider = new InMemoryCodeProvider(new Dictionary<string, string>
        {
            ["File.cs"] = "class OldVersion { }"
        });

        var editorBufferProvider = new InMemoryCodeProvider(new Dictionary<string, string>
        {
            ["File.cs"] = "class Problem { }" // Modified in editor, not saved
        });

        var manager = new AnalyzerManager();
        manager.RegisterRule(new TestRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var savedResults = engine.Analyze(savedFileProvider.GetSyntaxTrees()).ToList();
        var editorResults = engine.Analyze(editorBufferProvider.GetSyntaxTrees()).ToList();

        // Assert - Saved file has no problems, editor buffer does
        savedResults.Should().BeEmpty();
        editorResults.Should().ContainSingle();
        editorResults[0].RuleId.Should().Be("INTEGRATION001");
    }

    [Fact]
    public void AnalyzerEngine_MultipleProviderTypes_ProduceConsistentBehavior()
    {
        // Arrange
        const string sourceCode = """

                                  class GoodClass
                                  {
                                      void Method1() { }
                                      void Method2() { }
                                  }
                                  """;

        var provider1 = new InMemoryCodeProvider(new Dictionary<string, string>
        {
            ["Test.cs"] = sourceCode
        });

        var provider2 = new InMemoryCodeProvider(new Dictionary<string, string>
        {
            ["Different.cs"] = sourceCode
        });

        var filteredProvider = new FilteringCodeProvider(
            provider2,
            tree => true); // Filter that includes everything

        var manager = new AnalyzerManager();
        manager.RegisterRule(new TestRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var results1 = engine.Analyze(provider1.GetSyntaxTrees()).ToList();
        var results2 = engine.Analyze(filteredProvider.GetSyntaxTrees()).ToList();

        // Assert - Both should produce no results (no "Problem" class)
        results1.Should().BeEmpty();
        results2.Should().BeEmpty();
    }

    [Fact]
    public void AnalyzerEngine_ProviderReuse_AllowsMultipleAnalyses()
    {
        // Arrange
        var provider = new InMemoryCodeProvider(new Dictionary<string, string>
        {
            ["Test.cs"] = "class Problem { }"
        });

        var manager = new AnalyzerManager();
        manager.RegisterRule(new TestRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act - Analyze same provider multiple times
        var results1 = engine.Analyze(provider.GetSyntaxTrees()).ToList();
        var results2 = engine.Analyze(provider.GetSyntaxTrees()).ToList();
        var results3 = engine.Analyze(provider.GetSyntaxTrees()).ToList();

        // Assert - All analyses should produce same results
        results1.Should().HaveCount(1);
        results2.Should().HaveCount(1);
        results3.Should().HaveCount(1);

        results1[0].RuleId.Should().Be(results2[0].RuleId);
        results2[0].RuleId.Should().Be(results3[0].RuleId);
    }

    private sealed class TestRule : IAnalyzerRule
    {
        public string Id => "INTEGRATION001";
        public string Description => "Test rule for integration testing";
        public Severity Severity => Severity.Warning;
        public string Category => DiagnosticCategories.General;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            // Report diagnostic for any class named "Problem"
            if (tree.ToString().Contains("class Problem"))
                yield return new DiagnosticResult(tree.FilePath, Id, Description, 1, Severity, Category);
        }
    }
}