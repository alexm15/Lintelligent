using Xunit;
using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lintelligent.AnalyzerEngine.Tests;

/// <summary>
/// Tests verifying the IAnalyzerRule contract for IEnumerable return type.
/// Validates that rules correctly implement multiple findings behavior.
/// </summary>
public class RuleContractTests
{
    private sealed class ZeroFindingsRule : IAnalyzerRule
    {
        public string Id => "ZERO001";
        public string Description => "Never reports any findings";
        public Severity Severity => Severity.Info;
        public string Category => DiagnosticCategories.General;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            // Return empty enumerable - no findings
            return Enumerable.Empty<DiagnosticResult>();
        }
    }

    private sealed class MultipleFindingsRule(int findingCount) : IAnalyzerRule
    {
        public string Id => "MULTI001";
        public string Description => "Reports multiple findings";
        public Severity Severity => Severity.Warning;
        public string Category => DiagnosticCategories.Maintainability;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            for (var i = 1; i <= findingCount; i++)
            {
                yield return new DiagnosticResult(
                    tree.FilePath,
                    Id,
                    $"Finding {i}",
                    i,
                    Severity,
                    Category
                );
            }
        }
    }

    private sealed class LazyEvaluationRule : IAnalyzerRule
    {
        public int EvaluationCount { get; private set; }

        public string Id => "LAZY001";
        public string Description => "Tracks evaluation count";
        public Severity Severity => Severity.Info;
        public string Category => DiagnosticCategories.General;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            for (var i = 1; i <= 3; i++)
            {
                EvaluationCount++;
                yield return new DiagnosticResult(
                    tree.FilePath,
                    Id,
                    $"Finding {i}",
                    i,
                    Severity,
                    Category
                );
            }
        }
    }

    [Fact]
    public void Rule_EmittingZeroFindings_ReturnsEmptyEnumerable()
    {
        // Arrange
        var rule = new ZeroFindingsRule();
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act
        var results = rule.Analyze(tree);

        // Assert
        results.Should().NotBeNull("rules must never return null");
        results.Should().BeEmpty("rule should return empty enumerable when no violations found");
    }

    [Fact]
    public void Rule_EmittingMultipleFindings_ReturnsAllFindings()
    {
        // Arrange
        var rule = new MultipleFindingsRule(5);
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act
        var results = rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(5, "rule should emit 5 findings");
        results.Select(r => r.Message).Should().BeEquivalentTo(
            new[] { "Finding 1", "Finding 2", "Finding 3", "Finding 4", "Finding 5" },
            "all findings should be returned");
    }

    [Fact]
    public void Rule_EmittingManyFindings_HandlesLargeResultSets()
    {
        // Arrange - Test with 100+ findings to verify memory efficiency
        var rule = new MultipleFindingsRule(100);
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act
        var results = rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(100, "rule should handle large result sets");
        results.Should().OnlyContain(r => r.RuleId == "MULTI001");
        results.Should().OnlyContain(r => r.Severity == Severity.Warning);
    }

    [Fact]
    public void Rule_UsingYieldReturn_DoesNotEagerlyMaterialize()
    {
        // Arrange
        var rule = new LazyEvaluationRule();
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act - Get enumerable but don't enumerate yet
        var results = rule.Analyze(tree);

        // Assert - Should not have evaluated any items yet
        rule.EvaluationCount.Should().Be(0, "yield return should use lazy evaluation");

        // Act - Enumerate first item only
        var firstItem = results.First();

        // Assert - Should only have evaluated first item
        rule.EvaluationCount.Should().Be(1, "should only evaluate items as they're consumed");

        // Act - Enumerate all remaining items
        var allItems = results.ToList();

        // Assert - Now all should be evaluated (3 total, but First() already consumed 1, so enumeration restarted)
        rule.EvaluationCount.Should().BeGreaterOrEqualTo(3, "all items should eventually be evaluated");
    }

    [Fact]
    public void Rule_MultipleEnumeration_AllowsReenumeration()
    {
        // Arrange
        var rule = new MultipleFindingsRule(3);
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act - Enumerate twice
        var results = rule.Analyze(tree);
        var firstPass = results.ToList();
        var secondPass = results.ToList();

        // Assert - Both enumerations should produce same results
        firstPass.Should().HaveCount(3);
        secondPass.Should().HaveCount(3);
        firstPass.Should().BeEquivalentTo(secondPass, "re-enumeration should produce same results");
    }

    [Fact]
    public void Rule_WithNoViolations_ReturnsEmptyNotNull()
    {
        // Arrange
        var rule = new ZeroFindingsRule();
        var tree = CSharpSyntaxTree.ParseText("class Perfect { }", path: "Test.cs");

        // Act
        var results = rule.Analyze(tree);

        // Assert
        results.Should().NotBeNull("null is not a valid return value");
        results.Should().BeAssignableTo<IEnumerable<DiagnosticResult>>("must return IEnumerable");
        results.Any().Should().BeFalse("should have no findings");
    }
}
