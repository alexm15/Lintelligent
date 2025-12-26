using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules;

public class LongParameterListRuleTests
{
    private readonly LongParameterListRule _rule = new();

    private static SyntaxTree CreateSyntaxTree(string code, string path = "TestFile.cs")
    {
        return CSharpSyntaxTree.ParseText(code, path: path);
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenMethodHasExactly5Parameters()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method(int a, int b, int c, int d, int e)
                       {
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsDiagnostic_WhenMethodHas6Parameters()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method(int a, int b, int c, int d, int e, int f)
                       {
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT002",
                Severity = Severity.Warning,
                Category = DiagnosticCategories.CodeSmell
            }, options => options.ExcludingMissingMembers());

        diagnostics.First().Message.Should().Contain("Method");
        diagnostics.First().Message.Should().Contain("6 parameters");
        diagnostics.First().Message.Should().Contain("max: 5");
    }

    [Fact]
    public void ReturnsDiagnostic_WhenConstructorHas8Parameters()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public TestClass(int a, int b, int c, int d, int e, int f, int g, int h)
                       {
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics.First().Message.Should().Contain("TestClass");
        diagnostics.First().Message.Should().Contain("8 parameters");
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenMethodHas0Parameters()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void ExcludesThisParameter_ForExtensionMethods()
    {
        // Arrange - Extension method with 'this' parameter + 5 others = 6 total, but should count as 5
        var code = """
                   public static class Extensions
                   {
                       public static void ExtMethod(this string str, int a, int b, int c, int d, int e)
                       {
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("the 'this' parameter should not count toward the limit");
    }

    [Fact]
    public void ReturnsDiagnostic_ForExtensionMethodExceedingLimit()
    {
        // Arrange - Extension method with 'this' parameter + 6 others = 7 total, should count as 6
        var code = """
                   public static class Extensions
                   {
                       public static void ExtMethod(this string str, int a, int b, int c, int d, int e, int f)
                       {
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics.First().Message.Should().Contain("6 parameters");
    }

    [Fact]
    public void VerifiesCorrectMetadata_RuleIdSeverityCategory()
    {
        // Assert
        _rule.Id.Should().Be("LNT002");
        _rule.Description.Should().Be("Methods should not have more than 5 parameters");
        _rule.Severity.Should().Be(Severity.Warning);
        _rule.Category.Should().Be(DiagnosticCategories.CodeSmell);
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForGeneratedCode()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method(int a, int b, int c, int d, int e, int f, int g, int h)
                       {
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code, "Form1.Designer.cs");

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("generated code should be skipped");
    }
}