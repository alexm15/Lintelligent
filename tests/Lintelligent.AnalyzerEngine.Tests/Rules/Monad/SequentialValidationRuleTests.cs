using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules.Monad;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules.Monad;

/// <summary>
///     Tests for LNT202: Sequential Validation to Validation&lt;T&gt; detection rule.
/// </summary>
public class SequentialValidationRuleTests
{
    private readonly SequentialValidationRule _rule = new();
    private const string TestFilePath = "Test.cs";

    [Fact]
    public void Analyze_TwoOrMoreSequentialValidations_ProducesLNT202()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ValidateUser(User user)
    {
        if (user.Name == null)
            return ""Name is required"";
        
        if (user.Email == null)
            return ""Email is required"";
        
        if (user.Age < 18)
            return ""Must be 18 or older"";
        
        return ""OK"";
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].RuleId.Should().Be("LNT202");
        results[0].Severity.Should().Be(Severity.Info);
        results[0].Category.Should().Be(DiagnosticCategories.Functional);
    }

    [Fact]
    public void Analyze_SingleValidationCheck_NoWarning()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ValidateUser(User user)
    {
        if (user.Name == null)
            return ""Name is required"";
        
        return ""OK"";
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_IfStatementsWithoutReturns_NoWarning()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    void ProcessUser(User user)
    {
        if (user.Name == null)
        {
            Logger.Log(""Missing name"");
        }
        
        if (user.Email == null)
        {
            Logger.Log(""Missing email"");
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
    public void Analyze_MethodAlreadyReturningValidation_NoWarning()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    Validation<User> ValidateUser(User user)
    {
        if (user.Name == null)
            return Fail(""Name required"");
        
        if (user.Email == null)
            return Fail(""Email required"");
        
        return Success(user);
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_BlockStatementWithSingleReturn_DetectedAsValidation()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ValidateUser(User user)
    {
        if (user.Name == null)
        {
            return ""Name is required"";
        }
        
        if (user.Email == null)
        {
            return ""Email is required"";
        }
        
        return ""OK"";
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public void Analyze_DiagnosticMessage_ExplainsErrorAccumulation()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ValidateUser(User user)
    {
        if (user.Name == null)
            return ""Name required"";
        if (user.Email == null)
            return ""Email required"";
        return ""OK"";
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].Message.Should().Contain("Validation<T>");
    }

    [Fact]
    public void Analyze_DiagnosticProperties_ContainMonadTypeAndComplexityScore()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string ValidateUser(User user)
    {
        if (user.Name == null) return ""error1"";
        if (user.Email == null) return ""error2"";
        if (user.Age < 0) return ""error3"";
        return ""OK"";
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].Properties.Should().ContainKey("MonadType");
        results[0].Properties["MonadType"].Should().Be("Validation");
        results[0].Properties.Should().ContainKey("ComplexityScore");
        results[0].Properties["ComplexityScore"].Should().Be("3");
    }

    [Fact]
    public void RuleProperties_HaveCorrectValues()
    {
        // Assert
        _rule.Id.Should().Be("LNT202");
        _rule.Description.Should().Be("Consider using Validation<T> to accumulate validation errors");
        _rule.Severity.Should().Be(Severity.Info);
        _rule.Category.Should().Be(DiagnosticCategories.Functional);
    }
}
