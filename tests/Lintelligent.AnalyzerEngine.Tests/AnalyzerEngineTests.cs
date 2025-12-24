using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using EngineClass = Lintelligent.AnalyzerEngine.Analysis.AnalyzerEngine;
using EngineManager = Lintelligent.AnalyzerEngine.Analysis.AnalyzerManager;

namespace Lintelligent.AnalyzerEngine.Tests;

/// <summary>
///     Tests verifying AnalyzerEngine correctly aggregates findings from multiple rules.
/// </summary>
public class AnalyzerEngineTests
{
    [Fact]
    public void AnalyzerEngine_WithMultipleRules_AggregatesAllFindings()
    {
        // Arrange
        var manager = new EngineManager();
        manager.RegisterRule(new FirstRule());
        manager.RegisterRule(new SecondRule());
        manager.RegisterRule(new ThirdRule());

        var engine = new EngineClass(manager);
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");
        var trees = new[] {tree};

        // Act
        var results = engine.Analyze(trees).ToList();

        // Assert
        results.Should().HaveCount(3, "should aggregate findings from all rules (1 + 2 + 0)");
        results.Should().Contain(r => r.RuleId == "FIRST001");
        results.Should().Contain(r => r.RuleId == "SECOND001");
        results.Where(r => r.RuleId == "SECOND001").Should().HaveCount(2, "second rule emits 2 findings");
    }

    [Fact]
    public void AnalyzerEngine_WithMultipleTrees_ProcessesAllTrees()
    {
        // Arrange
        var manager = new EngineManager();
        manager.RegisterRule(new FirstRule());

        var engine = new EngineClass(manager);
        var tree1 = CSharpSyntaxTree.ParseText("class A { }", path: "A.cs");
        var tree2 = CSharpSyntaxTree.ParseText("class B { }", path: "B.cs");
        var trees = new[] {tree1, tree2};

        // Act
        var results = engine.Analyze(trees).ToList();

        // Assert
        results.Should().HaveCount(2, "should analyze both trees");
        results.Select(r => r.FilePath).Should().BeEquivalentTo("A.cs", "B.cs");
    }

    [Fact]
    public void AnalyzerEngine_WithNoRules_ReturnsEmpty()
    {
        // Arrange
        var manager = new EngineManager();
        var engine = new EngineClass(manager);
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act
        var results = engine.Analyze([tree]).ToList();

        // Assert
        results.Should().BeEmpty("no rules registered should produce no findings");
    }

    [Fact]
    public void AnalyzerEngine_StreamingBehavior_UsesLazyEvaluation()
    {
        // Arrange
        var manager = new EngineManager();
        manager.RegisterRule(new SecondRule());

        var engine = new EngineClass(manager);
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act - Get enumerable but don't materialize
        var results = engine.Analyze([tree]);

        // Assert - Should return IEnumerable (lazy)
        results.Should().BeAssignableTo<IEnumerable<DiagnosticResult>>();

        // Act - Materialize results
        var materialized = results.ToList();

        // Assert - Should have findings
        materialized.Should().HaveCount(2);
    }

    [Fact]
    public void AnalyzerEngine_WithRuleEmittingMultipleFindings_PreservesAllFindings()
    {
        // Arrange - Rule that emits 5 findings for same file
        var manager = new EngineManager();
        manager.RegisterRule(new SecondRule()); // Emits 2 findings
        manager.RegisterRule(new FirstRule()); // Emits 1 finding

        var engine = new EngineClass(manager);
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act
        var results = engine.Analyze([tree]).ToList();

        // Assert
        results.Should().HaveCount(3, "all findings from all rules should be preserved");
        results.Should().OnlyContain(r => r.FilePath == "Test.cs");
    }

    [Fact]
    public void AnalyzerEngine_WithRuleThrowingException_ContinuesWithOtherRules()
    {
        // Arrange
        var manager = new EngineManager();
        manager.RegisterRule(new FirstRule()); // Works correctly
        manager.RegisterRule(new ThrowingRule()); // Throws exception
        manager.RegisterRule(new SecondRule()); // Works correctly

        var engine = new EngineClass(manager);
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act
        var results = engine.Analyze([tree]).ToList();

        // Assert - Should have findings from non-throwing rules
        results.Should().HaveCount(3, "should get findings from FirstRule (1) and SecondRule (2)");
        results.Should().Contain(r => r.RuleId == "FIRST001");
        results.Should().Contain(r => r.RuleId == "SECOND001");
        results.Should().NotContain(r => r.RuleId == "THROW001");

        // Assert - Exception should be recorded
        engine.Exceptions.Should().HaveCount(1);
        engine.Exceptions[0].RuleId.Should().Be("THROW001");
        engine.Exceptions[0].FilePath.Should().Be("Test.cs");
        engine.Exceptions[0].Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void AnalyzerEngine_AfterMultipleAnalyses_ClearsExceptionsFromPreviousRun()
    {
        // Arrange
        var manager = new EngineManager();
        manager.RegisterRule(new ThrowingRule());

        var engine = new EngineClass(manager);
        var tree = CSharpSyntaxTree.ParseText("class C { }", path: "Test.cs");

        // Act - First analysis
        engine.Analyze([tree]).ToList();
        var firstExceptionCount = engine.Exceptions.Count;

        // Act - Second analysis
        engine.Analyze([tree]).ToList();
        var secondExceptionCount = engine.Exceptions.Count;

        // Assert - Should not accumulate exceptions
        firstExceptionCount.Should().Be(1);
        secondExceptionCount.Should().Be(1, "exceptions from previous run should be cleared");
    }

    private sealed class FirstRule : IAnalyzerRule
    {
        public string Id => "FIRST001";
        public string Description => "First test rule";
        public Severity Severity => Severity.Error;
        public string Category => DiagnosticCategories.Security;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            yield return new DiagnosticResult(tree.FilePath, Id, "Finding from first rule", 1, Severity, Category);
        }
    }

    private sealed class SecondRule : IAnalyzerRule
    {
        public string Id => "SECOND001";
        public string Description => "Second test rule";
        public Severity Severity => Severity.Warning;
        public string Category => DiagnosticCategories.Performance;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            yield return new DiagnosticResult(tree.FilePath, Id, "Finding 1 from second rule", 2, Severity, Category);
            yield return new DiagnosticResult(tree.FilePath, Id, "Finding 2 from second rule", 3, Severity, Category);
        }
    }

    private sealed class ThirdRule : IAnalyzerRule
    {
        public string Id => "THIRD001";
        public string Description => "Third test rule with no findings";
        public Severity Severity => Severity.Info;
        public string Category => DiagnosticCategories.Style;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            return [];
        }
    }

    // Test helper rule that throws exception
    private sealed class ThrowingRule : IAnalyzerRule
    {
        public string Id => "THROW001";
        public string Description => "Rule that throws exception";
        public Severity Severity => Severity.Error;
        public string Category => DiagnosticCategories.General;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            throw new InvalidOperationException("Simulated rule failure");
        }
    }
}