using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules;

public class ExceptionSwallowingRuleTests
{
    private readonly ExceptionSwallowingRule _rule = new();

    private static SyntaxTree CreateSyntaxTree(string code, string path = "TestFile.cs")
    {
        return CSharpSyntaxTree.ParseText(code, path: path);
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenCatchBlockHasThrowStatement()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                           try
                           {
                               DoSomething();
                           }
                           catch
                           {
                               throw;
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
    public void ReturnsDiagnostic_WhenCatchBlockIsEmpty()
    {
        // Arrange
        const string code = """
                            public class TestClass
                            {
                                public void Method()
                                {
                                    try
                                    {
                                        DoSomething();
                                    }
                                    catch
                                    {
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
                RuleId = "LNT007",
                Severity = Severity.Warning,
                Category = DiagnosticCategories.CodeSmell,
            }, options => options.ExcludingMissingMembers());

        diagnostics.First().Message.Should().Contain("Empty catch block suppresses exceptions");
    }

    [Fact]
    public void ReturnsDiagnostic_WhenCatchBlockHasOnlyComments()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                           try
                           {
                               DoSomething();
                           }
                           catch
                           {
                               // This is a comment, not executable code
                           }
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle();
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenCatchBlockHasLogging()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                           try
                           {
                               DoSomething();
                           }
                           catch (Exception ex)
                           {
                               Logger.Error(ex);
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
    public void ReturnsDiagnostic_OnlyForOuterCatch_WhenNestedTryCatch()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                           try
                           {
                               try
                               {
                                   DoSomething();
                               }
                               catch (Exception ex)
                               {
                                   Logger.Error(ex);
                               }
                           }
                           catch
                           {
                           }
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle("only the outer catch block is empty");
    }

    [Fact]
    public void VerifiesCorrectMetadata_RuleIdSeverityCategory()
    {
        // Assert
        _rule.Id.Should().Be("LNT007");
        _rule.Description.Should().Be("Catch blocks should not be empty");
        _rule.Severity.Should().Be(Severity.Warning);
        _rule.Category.Should().Be(DiagnosticCategories.CodeSmell);
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForGeneratedCode_DesignerFile()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       public void Method()
                       {
                           try { }
                           catch { }
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code, "Form1.Designer.cs");

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("generated code files should be skipped");
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForGeneratedCode_AutoGeneratedComment()
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
                           try { }
                           catch { }
                       }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("files with auto-generated comment should be skipped");
    }
}