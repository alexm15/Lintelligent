using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules.Monad;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules.Monad;

/// <summary>
///     Tests for LNT200: Nullable to Option&lt;T&gt; detection rule.
/// </summary>
public class NullableToOptionRuleTests
{
    private readonly NullableToOptionRule _rule = new();
    private const string TestFilePath = "Test.cs";

    [Fact]
    public void Analyze_MethodWithNullableReturnAnd3NullChecks_ProducesLNT200()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string? FindUser(int id)
    {
        if (id < 0) return null;
        var user = Database.Find(id);
        if (user == null) return null;
        if (user.Name == null) return null;
        return user.Name;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: "Test.cs");

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].RuleId.Should().Be("LNT200");
        results[0].Severity.Should().Be(Severity.Info);
        results[0].Category.Should().Be(DiagnosticCategories.Functional);
        results[0].Message.Should().Contain("Option<string>");
        results[0].Message.Should().Contain("nullable");
    }

    [Fact]
    public void Analyze_MethodWithNullableReturnButLessThan3NullOps_NoResultProduct()
    {
        // Arrange: Only 2 null operations (below threshold)
        var testCode = @"
class TestClass
{
    string? FindUser(int id)
    {
        if (id < 0) return null;
        var user = Database.Find(id);
        return user?.Name;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_MethodAlreadyReturningOptionT_NoDiagnostic()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    Option<string> FindUser(int id)
    {
        if (id < 0) return Option<string>.None;
        var user = Database.Find(id);
        return user != null ? Option<string>.Some(user.Name) : Option<string>.None;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_NonNullableMethod_NoDiagnostic()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string GetUser(int id)
    {
        if (id < 0) return string.Empty;
        return Database.Find(id).Name ?? string.Empty;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_NullableWithNullConditionalOperators_CountsComplexity()
    {
        // Arrange: 3 null-conditional operators (?.)
        var testCode = @"
class TestClass
{
    string? GetAddress(int id)
    {
        var user = Database.Find(id);
        var city = user?.Address?.City?.Name;
        return city;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert: 3 null-conditional operators = complexity 3
        results.Should().HaveCount(1);
        results[0].RuleId.Should().Be("LNT200");
    }

    [Fact]
    public void Analyze_NullableWithNullCoalescingOperators_CountsComplexity()
    {
        // Arrange: 3 null-coalescing operators (??)
        var testCode = @"
class TestClass
{
    string? GetName(int id)
    {
        var user = Database.Find(id);
        var name = user?.FirstName ?? user?.LastName ?? string.Empty;
        return name ?? null;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert: 1 null literal + 3 ?? operators = complexity 4
        results.Should().HaveCount(1);
    }

    [Fact]
    public void Analyze_AsyncMethodReturningTaskOfNullable_NoDetectionYet()
    {
        // Arrange: Task<string?> detection requires semantic analysis (beyond MVP scope)
        // Future enhancement: Implement semantic analysis for generic nullable type arguments
        var testCode = @"
using System.Threading.Tasks;
class TestClass
{
    async Task<string?> FindUserAsync(int id)
    {
        if (id < 0) return null;
        var user = await Database.FindAsync(id);
        if (user == null) return null;
        if (user.Name == null) return null;
        return user.Name;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert: Current implementation doesn't detect nullable inside generic types
        results.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_DiagnosticMessageIncludesEducationalContent()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string? FindUser(int id)
    {
        if (id < 0) return null;
        if (id > 1000) return null;
        return null;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert - Short actionable message format
        results[0].Message.Should().Contain("Option<string>");
        results[0].Message.Should().Contain("nullable");
        results[0].Message.Should().Contain("null reference exceptions");
    }

    [Fact]
    public void RuleProperties_HaveCorrectValues()
    {
        // Assert
        _rule.Id.Should().Be("LNT200");
        _rule.Description.Should().Be("Consider using Option<T> for nullable return type");
        _rule.Severity.Should().Be(Severity.Info);
        _rule.Category.Should().Be(DiagnosticCategories.Functional);
    }

    [Fact]
    public void Analyze_DiagnosticProperties_ContainMonadTypeAndComplexityScore()
    {
        // Arrange
        var testCode = @"
class TestClass
{
    string? FindUser(int id)
    {
        if (id < 0) return null;
        var user = Database.Find(id);
        if (user == null) return null;
        if (user.Name == null) return null;
        return user.Name;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(testCode, path: TestFilePath);

        // Act
        var results = _rule.Analyze(tree).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].Properties.Should().ContainKey("MonadType");
        results[0].Properties["MonadType"].Should().Be("Option");
        results[0].Properties.Should().ContainKey("ComplexityScore");
        results[0].Properties["ComplexityScore"].Should().Be("5"); // 3 return null + 2 null comparisons
    }}