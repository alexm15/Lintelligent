using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests;

/// <summary>
///     Tests for AnalyzerManager validation and rule registration.
/// </summary>
public class AnalyzerManagerTests
{
    [Fact]
    public void RegisterRule_WithEmptyId_ThrowsArgumentException()
    {
        // Arrange
        var manager = new AnalyzerManager();
        var rule = new InvalidRuleEmptyId();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => manager.RegisterRule(rule));
        exception.Message.Should().Contain("Id");
    }

    [Fact]
    public void RegisterRule_WithNullId_ThrowsArgumentException()
    {
        // Arrange
        var manager = new AnalyzerManager();
        var rule = new InvalidRuleNullId();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => manager.RegisterRule(rule));
        exception.ParamName.Should().Be("Id");
    }

    [Fact]
    public void RegisterRule_WithUndefinedSeverity_ThrowsArgumentException()
    {
        // Arrange
        var manager = new AnalyzerManager();
        var rule = new InvalidRuleUndefinedSeverity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => manager.RegisterRule(rule));
        exception.Message.Should().Contain("severity");
        exception.Message.Should().Contain("undefined");
    }

    [Fact]
    public void RegisterRule_WithEmptyCategory_ThrowsArgumentException()
    {
        // Arrange
        var manager = new AnalyzerManager();
        var rule = new InvalidRuleEmptyCategory();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => manager.RegisterRule(rule));
        exception.Message.Should().Contain("Category");
    }

    [Fact]
    public void RegisterRule_WithNullCategory_ThrowsArgumentException()
    {
        // Arrange
        var manager = new AnalyzerManager();
        var rule = new InvalidRuleNullCategory();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => manager.RegisterRule(rule));
        exception.ParamName.Should().Be("Category");
    }

    [Fact]
    public void RegisterRule_WithValidRule_Succeeds()
    {
        // Arrange
        var manager = new AnalyzerManager();
        var rule = new ValidRule();

        // Act
        manager.RegisterRule(rule);

        // Assert
        manager.Rules.Should().Contain(rule);
        manager.Rules.Should().HaveCount(1);
    }

    [Fact]
    public void RegisterRule_WithNullRule_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new AnalyzerManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.RegisterRule(null!));
    }

    // Test helper rules with invalid configurations
    private sealed class InvalidRuleEmptyId : IAnalyzerRule
    {
        public string Id => "";
        public string Description => "Invalid rule with empty ID";
        public Severity Severity => Severity.Warning;
        public string Category => DiagnosticCategories.General;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            return Enumerable.Empty<DiagnosticResult>();
        }
    }

    private sealed class InvalidRuleNullId : IAnalyzerRule
    {
        public string Id => null!;
        public string Description => "Invalid rule with null ID";
        public Severity Severity => Severity.Warning;
        public string Category => DiagnosticCategories.General;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            return Enumerable.Empty<DiagnosticResult>();
        }
    }

    private sealed class InvalidRuleUndefinedSeverity : IAnalyzerRule
    {
        public string Id => "INVALID001";
        public string Description => "Invalid rule with undefined severity";
        public Severity Severity => unchecked((Severity) 999);
        public string Category => DiagnosticCategories.General;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            return Enumerable.Empty<DiagnosticResult>();
        }
    }

    private sealed class InvalidRuleEmptyCategory : IAnalyzerRule
    {
        public string Id => "INVALID002";
        public string Description => "Invalid rule with empty category";
        public Severity Severity => Severity.Warning;
        public string Category => "";

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            return Enumerable.Empty<DiagnosticResult>();
        }
    }

    private sealed class InvalidRuleNullCategory : IAnalyzerRule
    {
        public string Id => "INVALID003";
        public string Description => "Invalid rule with null category";
        public Severity Severity => Severity.Warning;
        public string Category => null!;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            return Enumerable.Empty<DiagnosticResult>();
        }
    }

    private sealed class ValidRule : IAnalyzerRule
    {
        public string Id => "VALID001";
        public string Description => "Valid rule";
        public Severity Severity => Severity.Info;
        public string Category => DiagnosticCategories.Style;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            return Enumerable.Empty<DiagnosticResult>();
        }
    }
}