using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules;

public class DeadCodeRuleTests
{
    private readonly DeadCodeRule _rule = new();

    private static SyntaxTree CreateSyntaxTree(string code)
    {
        return CSharpSyntaxTree.ParseText(code, path: "TestFile.cs");
    }

    [Fact]
    public void ReturnsDiagnostic_ForUnusedPrivateMethod()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       private void UnusedMethod()
                       {
                           // Never called
                       }
                       
                       public void PublicMethod()
                       {
                           // Does not call UnusedMethod
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
                RuleId = "LNT006",
                Severity = Severity.Info,
                Category = DiagnosticCategories.Maintainability
            }, options => options.ExcludingMissingMembers());

        diagnostics.First().Message.Should().Contain("UnusedMethod");
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenPrivateMethodIsUsed()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       private void HelperMethod()
                       {
                           // Implementation
                       }
                       
                       public void PublicMethod()
                       {
                           HelperMethod(); // Called here
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("private method is used");
    }

    [Fact]
    public void ReturnsDiagnostic_ForUnusedPrivateField()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       private int _unusedField;
                       
                       public void Method()
                       {
                           // Field is never referenced
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
                RuleId = "LNT006",
                Severity = Severity.Info,
                Category = DiagnosticCategories.Maintainability
            }, options => options.ExcludingMissingMembers());

        diagnostics.First().Message.Should().Contain("_unusedField");
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenPrivateFieldIsUsed()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       private int _count;
                       
                       public int GetCount()
                       {
                           return _count; // Field is referenced
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("private field is used");
    }

    [Fact]
    public void ReturnsDiagnostic_WhenFieldOnlyUsedInInitializer()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       private int _count = 0;
                       
                       public void Method()
                       {
                           // _count is never read, only initialized
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle("field only used in initializer should be flagged");
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForPublicMembers()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void PublicMethod() { }
                       public int PublicField;
                       
                       // Neither are called/used within this class, but that's OK for public members
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("public members are not checked for usage");
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenClassImplementsInterface()
    {
        // Arrange
        var code = """
                   public interface IService
                   {
                       void DoWork();
                   }

                   public class TestClass : IService
                   {
                       private void DoWork()
                       {
                           // Explicit interface implementation (heuristic: has base list)
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("heuristic excludes private members when class has interfaces");
    }

    [Fact]
    public void VerifiesCorrectMetadata_RuleIdSeverityCategory()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       private void UnusedMethod() { }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT006",
                Severity = Severity.Info,
                Category = DiagnosticCategories.Maintainability
            }, options => options.ExcludingMissingMembers());
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForGeneratedCode()
    {
        // Arrange
        var code = """
                   // <auto-generated>
                   //     This code was generated by a tool.
                   // </auto-generated>

                   public class TestClass
                   {
                       private void UnusedMethod() { }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("generated code should be skipped");
    }
}