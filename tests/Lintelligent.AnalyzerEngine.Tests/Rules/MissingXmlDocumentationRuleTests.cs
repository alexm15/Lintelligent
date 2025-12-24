using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules;

public class MissingXmlDocumentationRuleTests
{
    private readonly MissingXmlDocumentationRule _rule = new();

    private static SyntaxTree CreateSyntaxTree(string code)
    {
        return CSharpSyntaxTree.ParseText(code, path: "TestFile.cs");
    }

    [Fact]
    public void ReturnsDiagnostic_ForPublicClassWithoutDocumentation()
    {
        // Arrange
        var code = """
            public class TestClass
            {
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT008",
                Severity = Severity.Info,
                Category = DiagnosticCategories.Documentation
            }, options => options.ExcludingMissingMembers());

        diagnostics.First().Message.Should().Contain("TestClass");
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForPublicClassWithSummaryDocumentation()
    {
        // Arrange
        var code = """
            /// <summary>
            /// Test class description
            /// </summary>
            public class TestClass
            {
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("class has XML documentation");
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForPublicMethodWithInheritdoc()
    {
        // Arrange
        var code = """
            public class TestClass
            {
                /// <inheritdoc />
                public override string ToString()
                {
                    return "test";
                }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert - only the class should be flagged, not the method with inheritdoc
        diagnostics.Should().ContainSingle();
        diagnostics.First().Message.Should().Contain("TestClass")
            .And.NotContain("ToString");
    }

    [Fact]
    public void ReturnsDiagnostic_ForPublicMethodWithoutDocumentation()
    {
        // Arrange
        var code = """
            /// <summary>Test class</summary>
            public class TestClass
            {
                public void PublicMethod()
                {
                }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics.First().Message.Should().Contain("PublicMethod");
    }

    [Fact]
    public void ReturnsDiagnostic_ForPublicPropertyWithoutDocumentation()
    {
        // Arrange
        var code = """
            /// <summary>Test class</summary>
            public class TestClass
            {
                public string Name { get; set; }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics.First().Message.Should().Contain("Name");
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForPrivateMembers()
    {
        // Arrange
        var code = """
            public class TestClass
            {
                private void PrivateMethod() { }
                private string _field;
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert - only the class itself should be flagged
        diagnostics.Should().ContainSingle();
        diagnostics.First().Message.Should().Contain("TestClass");
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForInternalMembers()
    {
        // Arrange
        var code = """
            internal class TestClass
            {
                internal void Method() { }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("internal members are not checked");
    }

    [Fact]
    public void ReturnsDiagnostic_ForProtectedMembers()
    {
        // Arrange
        var code = """
            /// <summary>Test class</summary>
            public class TestClass
            {
                protected void ProtectedMethod() { }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics.First().Message.Should().Contain("ProtectedMethod");
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenRegularCommentUsed()
    {
        // Arrange
        var code = """
            // Regular comment (not XML doc)
            public class TestClass
            {
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle("regular comments are not valid XML documentation");
    }

    [Fact]
    public void VerifiesCorrectMetadata_RuleIdSeverityCategory()
    {
        // Arrange
        var code = """
            public class TestClass
            {
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT008",
                Severity = Severity.Info,
                Category = DiagnosticCategories.Documentation
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
                public void Method() { }
            }
            """;
        var tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("generated code should be skipped");
    }
}
