using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules;

public class MagicNumberRuleTests
{
    private readonly MagicNumberRule _rule = new();

    private static SyntaxTree CreateSyntaxTree(string code)
    {
        return CSharpSyntaxTree.ParseText(code, path: "TestFile.cs");
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForZeroOneMinus1()
    {
        // Arrange
        var code = """
            public class TestClass
            {
                public void Method()
                {
                    int x = 0;
                    int y = 1;
                    int z = -1;
                }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("0, 1, and -1 are excluded from magic number detection");
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenLiteralIsConstDeclaration()
    {
        // Arrange
        var code = """
            public class TestClass
            {
                private const int MaxRetries = 3;
                
                public void Method()
                {
                    const int LocalConst = 100;
                }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("literals in const declarations should not be flagged");
    }

    [Fact]
    public void ReturnsDiagnostic_ForThreadSleep5000()
    {
        // Arrange
        var code = """
            public class TestClass
            {
                public void Method()
                {
                    Thread.Sleep(5000);
                }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT004",
                Severity = Severity.Info,
                Category = DiagnosticCategories.CodeSmell
            }, options => options.ExcludingMissingMembers());

        diagnostics.First().Message.Should().Contain("5000");
    }

    [Fact]
    public void ReturnsDiagnostic_ForFloatingPointLiteral()
    {
        // Arrange
        var code = """
            public class TestClass
            {
                public void Method()
                {
                    double pi = 3.14159;
                    float value = 2.5f;
                }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().HaveCount(2, "both floating-point literals should be flagged");
        diagnostics.First().Message.Should().Contain("3.14159");
        diagnostics.Last().Message.Should().Contain("2.5");
    }

    [Fact]
    public void ReturnsDiagnostic_ForAttributeArguments()
    {
        // Arrange
        var code = """
            using System;
            
            public class TestClass
            {
                [StringLength(50)]
                public string Name { get; set; }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle("attribute argument with magic number should be flagged");
        diagnostics.First().Message.Should().Contain("50");
    }

    [Fact]
    public void ReturnsDiagnostic_ForVariableAssignment()
    {
        // Arrange
        var code = """
            public class TestClass
            {
                public void Method()
                {
                    int timeout = 5000;
                }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics.First().Message.Should().Contain("5000")
            .And.Contain("named constant");
    }

    [Fact]
    public void VerifiesCorrectMetadata_RuleIdSeverityCategory()
    {
        // Arrange
        var code = """
            public class TestClass
            {
                public void Method()
                {
                    int value = 42;
                }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT004",
                Severity = Severity.Info,
                Category = DiagnosticCategories.CodeSmell
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
                public void Method()
                {
                    int value = 42;
                }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("generated code should be skipped");
    }
}
