using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules.Monad;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules.Monad;

/// <summary>
///     Tests for LNT201: Try/Catch to Either&lt;L, R&gt; detection rule.
/// </summary>
public class TryCatchToEitherRuleTests
{
    private readonly TryCatchToEitherRule _rule = new();
    private const string TestFilePath = "Test.cs";

    [Fact]
    public void Analyze_TryCatchWithReturnsInBothBranches_ProducesLNT201()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ProcessData(string input)
    {
        try
        {
            var result = Parse(input);
            return result.Value;
        }
        catch (Exception ex)
        {
            return ""error: "" + ex.Message;
        }
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].RuleId.Should().Be("LNT201");
        results[0].Severity.Should().Be(Severity.Info);
        results[0].Category.Should().Be(DiagnosticCategories.Functional);
    }

    [Fact]
    public void Analyze_TryCatchThatOnlyRethrows_NoWarning()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ProcessData(string input)
    {
        try
        {
            return Parse(input);
        }
        catch (Exception ex)
        {
            Logger.Log(ex);
            throw;
        }
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_TryCatchWithOnlyLogging_NoWarning()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ProcessData(string input)
    {
        try
        {
            return Parse(input);
        }
        catch (Exception ex)
        {
            Logger.Log(""Error: "" + ex.Message);
        }
        return null;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_MethodAlreadyReturningEither_NoWarning()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    Either<Error, string> ProcessData(string input)
    {
        try
        {
            var result = Parse(input);
            return Right(result.Value);
        }
        catch (Exception ex)
        {
            return Left(new Error(ex.Message));
        }
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_NestedTryCatchBlocks_ProducesLNT201ForEach()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ProcessData(string input)
    {
        try
        {
            var step1 = ParseStep1(input);
            try
            {
                var step2 = ParseStep2(step1);
                return step2;
            }
            catch (Exception ex2)
            {
                return ""step2 error: "" + ex2.Message;
            }
        }
        catch (Exception ex1)
        {
            return ""step1 error: "" + ex1.Message;
        }
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.All(r => r.RuleId == "LNT201").Should().BeTrue();
    }

    [Fact]
    public void Analyze_DiagnosticMessage_ContainsRailwayOrientedProgramming()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ProcessData(string input)
    {
        try
        {
            return Parse(input);
        }
        catch (Exception ex)
        {
            return ""error: "" + ex.Message;
        }
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert - Short message uses specific type parameters
        results.Should().HaveCount(1);
        results[0].Message.Should().Contain("Either<Error, T>");
    }

    [Fact]
    public void Analyze_DiagnosticProperties_ContainMonadTypeAndPattern()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ProcessData(string input)
    {
        try
        {
            return Parse(input);
        }
        catch (Exception ex)
        {
            return ""error"";
        }
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].Properties.Should().ContainKey("MonadType");
        results[0].Properties["MonadType"].Should().Be("Either");
        results[0].Properties.Should().ContainKey("PatternName");
        results[0].Properties["PatternName"].Should().Be("try-catch-control-flow");
    }

    [Fact]
    public void RuleProperties_HaveCorrectValues()
    {
        // Assert
        _rule.Id.Should().Be("LNT201");
        _rule.Description.Should().Be("Consider using Either<L, R> for error handling instead of try/catch");
        _rule.Severity.Should().Be(Severity.Info);
        _rule.Category.Should().Be(DiagnosticCategories.Functional);
    }
}
