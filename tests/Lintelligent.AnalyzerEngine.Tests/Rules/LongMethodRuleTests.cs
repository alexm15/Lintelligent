using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules;

public class LongMethodRuleTests
{
    private readonly LongMethodRule _rule = new();

    [Fact]
    public void Analyze_MethodExceeds20Lines_ReturnsDiagnostic()
    {
        // Arrange - Create method with 21 statements (exceeds threshold)
        var sourceCode = """
                         class TestClass
                         {
                             void LongMethod()
                             {
                                 var line1 = 1;
                                 var line2 = 2;
                                 var line3 = 3;
                                 var line4 = 4;
                                 var line5 = 5;
                                 var line6 = 6;
                                 var line7 = 7;
                                 var line8 = 8;
                                 var line9 = 9;
                                 var line10 = 10;
                                 var line11 = 11;
                                 var line12 = 12;
                                 var line13 = 13;
                                 var line14 = 14;
                                 var line15 = 15;
                                 var line16 = 16;
                                 var line17 = 17;
                                 var line18 = 18;
                                 var line19 = 19;
                                 var line20 = 20;
                                 var line21 = 21;
                             }
                         }
                         """;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: "Test.cs");

        // Act
        var diagnostics = _rule.Analyze(syntaxTree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT001",
                Severity = Severity.Warning,
                Category = DiagnosticCategories.CodeSmell
            }, options => options.ExcludingMissingMembers());

        // Verify enhanced message format includes method name and statement count
        diagnostics.First().Message.Should().Contain("LongMethod");
        diagnostics.First().Message.Should().Contain("21 statements");
        diagnostics.First().Message.Should().Contain("Consider extracting logical blocks into separate methods");
    }

    [Fact]
    public void Analyze_MethodUnder20Lines_ReturnsNoDiagnostics()
    {
        // Arrange - Create method with 19 statements (under threshold)
        var sourceCode = """
                         class TestClass
                         {
                             void ShortMethod()
                             {
                                 var line1 = 1;
                                 var line2 = 2;
                                 var line3 = 3;
                                 var line4 = 4;
                                 var line5 = 5;
                                 var line6 = 6;
                                 var line7 = 7;
                                 var line8 = 8;
                                 var line9 = 9;
                                 var line10 = 10;
                                 var line11 = 11;
                                 var line12 = 12;
                                 var line13 = 13;
                                 var line14 = 14;
                                 var line15 = 15;
                                 var line16 = 16;
                                 var line17 = 17;
                                 var line18 = 18;
                                 var line19 = 19;
                             }
                         }
                         """;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Act
        var diagnostics = _rule.Analyze(syntaxTree).ToList();

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_EmptyMethodBody_ReturnsNoDiagnostics()
    {
        // Arrange - Method with no statements
        var sourceCode = """
                         class TestClass
                         {
                             void EmptyMethod()
                             {
                             }
                         }
                         """;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Act
        var diagnostics = _rule.Analyze(syntaxTree).ToList();

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_NullMethodBody_ReturnsNoDiagnostics()
    {
        // Arrange - Abstract method with no body
        var sourceCode = """
                         abstract class TestClass
                         {
                             public abstract void AbstractMethod();
                         }
                         """;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Act
        var diagnostics = _rule.Analyze(syntaxTree).ToList();

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_BoundaryCase_Exactly20Lines_ReturnsNoDiagnostics()
    {
        // Arrange - Create method with exactly 20 statements (at threshold)
        var sourceCode = """
                         class TestClass
                         {
                             void BoundaryMethod()
                             {
                                 var line1 = 1;
                                 var line2 = 2;
                                 var line3 = 3;
                                 var line4 = 4;
                                 var line5 = 5;
                                 var line6 = 6;
                                 var line7 = 7;
                                 var line8 = 8;
                                 var line9 = 9;
                                 var line10 = 10;
                                 var line11 = 11;
                                 var line12 = 12;
                                 var line13 = 13;
                                 var line14 = 14;
                                 var line15 = 15;
                                 var line16 = 16;
                                 var line17 = 17;
                                 var line18 = 18;
                                 var line19 = 19;
                                 var line20 = 20;
                             }
                         }
                         """;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Act
        var diagnostics = _rule.Analyze(syntaxTree).ToList();

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_BoundaryCase_Exactly21Lines_ReturnsDiagnostic()
    {
        // Arrange - Create method with exactly 21 statements (just over threshold)
        var sourceCode = """
                         class TestClass
                         {
                             void JustOverBoundaryMethod()
                             {
                                 var line1 = 1;
                                 var line2 = 2;
                                 var line3 = 3;
                                 var line4 = 4;
                                 var line5 = 5;
                                 var line6 = 6;
                                 var line7 = 7;
                                 var line8 = 8;
                                 var line9 = 9;
                                 var line10 = 10;
                                 var line11 = 11;
                                 var line12 = 12;
                                 var line13 = 13;
                                 var line14 = 14;
                                 var line15 = 15;
                                 var line16 = 16;
                                 var line17 = 17;
                                 var line18 = 18;
                                 var line19 = 19;
                                 var line20 = 20;
                                 var line21 = 21;
                             }
                         }
                         """;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: "Test.cs");

        // Act
        var diagnostics = _rule.Analyze(syntaxTree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.RuleId.Should().Be("LNT001");
    }

    [Fact]
    public void RuleMetadata_ReturnsExpectedProperties()
    {
        // Assert
        _rule.Id.Should().Be("LNT001");
        _rule.Description.Should().Be("Method exceeds recommended length");
        _rule.Severity.Should().Be(Severity.Warning);
        _rule.Category.Should().Be(DiagnosticCategories.CodeSmell);
    }

    [Fact]
    public void Analyze_MultipleMethodsExceedThreshold_ReturnsMultipleDiagnostics()
    {
        // Arrange - Create class with 2 long methods
        var sourceCode = """
                         class TestClass
                         {
                             void LongMethod1()
                             {
                                 var line1 = 1;
                                 var line2 = 2;
                                 var line3 = 3;
                                 var line4 = 4;
                                 var line5 = 5;
                                 var line6 = 6;
                                 var line7 = 7;
                                 var line8 = 8;
                                 var line9 = 9;
                                 var line10 = 10;
                                 var line11 = 11;
                                 var line12 = 12;
                                 var line13 = 13;
                                 var line14 = 14;
                                 var line15 = 15;
                                 var line16 = 16;
                                 var line17 = 17;
                                 var line18 = 18;
                                 var line19 = 19;
                                 var line20 = 20;
                                 var line21 = 21;
                             }

                             void LongMethod2()
                             {
                                 var line1 = 1;
                                 var line2 = 2;
                                 var line3 = 3;
                                 var line4 = 4;
                                 var line5 = 5;
                                 var line6 = 6;
                                 var line7 = 7;
                                 var line8 = 8;
                                 var line9 = 9;
                                 var line10 = 10;
                                 var line11 = 11;
                                 var line12 = 12;
                                 var line13 = 13;
                                 var line14 = 14;
                                 var line15 = 15;
                                 var line16 = 16;
                                 var line17 = 17;
                                 var line18 = 18;
                                 var line19 = 19;
                                 var line20 = 20;
                                 var line21 = 21;
                             }
                         }
                         """;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: "Test.cs");

        // Act
        var diagnostics = _rule.Analyze(syntaxTree).ToList();

        // Assert
        diagnostics.Should().HaveCount(2);
        diagnostics.Should().AllSatisfy(d => d.RuleId.Should().Be("LNT001"));
    }
}