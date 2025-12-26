using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules;

public class ComplexConditionalRuleTests
{
    private readonly ComplexConditionalRule _rule = new();

    private static SyntaxTree CreateSyntaxTree(string code, string path = "TestFile.cs")
    {
        return CSharpSyntaxTree.ParseText(code, path: path);
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenNestingDepthIs3()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                           if (a)
                           {
                               if (b)
                               {
                                   if (c)
                                   {
                                       // Depth 3 - OK
                                   }
                               }
                           }
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
    public void ReturnsDiagnostic_WhenNestingDepthIs4()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                           if (a)
                           {
                               if (b)
                               {
                                   if (c)
                                   {
                                       if (d)
                                       {
                                           // Depth 4 - VIOLATION
                                       }
                                   }
                               }
                           }
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
                RuleId = "LNT003",
                Severity = Severity.Warning,
                Category = DiagnosticCategories.CodeSmell
            }, options => options.ExcludingMissingMembers());

        diagnostics.First().Message.Should().Contain("depth is 4");
        diagnostics.First().Message.Should().Contain("max: 3");
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForIfElseIfChains_SameLevel()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                           if (a)
                           {
                           }
                           else if (b)
                           {
                           }
                           else if (c)
                           {
                           }
                           else if (d)
                           {
                           }
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("else-if chains are at the same level, not nested");
    }

    [Fact]
    public void CountsSwitchStatements_InNestingDepth()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                           if (a)
                           {
                               switch (x)
                               {
                                   case 1:
                                       if (b)
                                       {
                                           if (c)
                                           {
                                               // if=1, switch=2, if=3, if=4 - VIOLATION
                                           }
                                       }
                                       break;
                               }
                           }
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle("switch statements should count toward nesting depth");
        diagnostics.First().Message.Should().Contain("depth is 4");
    }

    [Fact]
    public void VerifiesCorrectMetadata_RuleIdSeverityCategory()
    {
        // Assert
        _rule.Id.Should().Be("LNT003");
        _rule.Description.Should().Be("Conditional statements should not be nested more than 3 levels deep");
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
                       public void Method()
                       {
                           if (a)
                           {
                               if (b)
                               {
                                   if (c)
                                   {
                                       if (d)
                                       {
                                           // Depth 4 - but in generated file
                                       }
                                   }
                               }
                           }
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