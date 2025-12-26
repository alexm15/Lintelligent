using System.Text;
using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules;

public class GodClassRuleTests
{
    private readonly GodClassRule _rule = new();

    private static SyntaxTree CreateSyntaxTree(string code)
    {
        return CSharpSyntaxTree.ParseText(code, path: "TestFile.cs");
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenClassHasExactly500LinesAnd10Methods()
    {
        // Arrange - create a class under both thresholds
        var classContent = new StringBuilder();
        classContent.AppendLine("public class TestClass");
        classContent.AppendLine("{");

        // Add 10 methods (under the 15 method limit)
        for (var i = 0; i < 10; i++) classContent.AppendLine($"    public void Method{i}() {{ }}");

        classContent.AppendLine("}");

        var code = classContent.ToString();
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert - should be under both thresholds, no diagnostic
        diagnostics.Should().BeEmpty("class with 10 methods and minimal lines should not trigger");
    }

    [Fact]
    public void ReturnsDiagnostic_WhenClassHas501Lines()
    {
        // Arrange - create a class with >500 lines
        var classContent = new StringBuilder();
        classContent.AppendLine("public class TestClass");
        classContent.AppendLine("{");

        // Add enough lines to definitely exceed 500
        for (var i = 0; i < 510; i++) classContent.AppendLine($"    // Line {i}");

        classContent.AppendLine("}");

        var code = classContent.ToString();
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT005",
                Severity = Severity.Warning,
                Category = DiagnosticCategories.Design
            }, options => options.ExcludingMissingMembers());

        diagnostics.First().Message.Should().Contain("lines")
            .And.Contain("max: 500");
    }

    [Fact]
    public void ReturnsDiagnostic_WhenClassHas300LinesAnd16Methods()
    {
        // Arrange
        var classContent = new StringBuilder();
        classContent.AppendLine("public class TestClass");
        classContent.AppendLine("{");

        // Add 16 methods
        for (var i = 0; i < 16; i++)
        {
            classContent.AppendLine($"    public void Method{i}()");
            classContent.AppendLine("    {");
            classContent.AppendLine("        // Method body");
            classContent.AppendLine("    }");
        }

        classContent.AppendLine("}");

        var code = classContent.ToString();
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT005",
                Severity = Severity.Warning,
                Category = DiagnosticCategories.Design
            }, options => options.ExcludingMissingMembers());

        diagnostics.First().Message.Should().Contain("16 methods");
    }

    [Fact]
    public void ReturnsNoDiagnostic_WhenClassHas250LinesAnd14Methods()
    {
        // Arrange
        var classContent = new StringBuilder();
        classContent.AppendLine("public class TestClass");
        classContent.AppendLine("{");

        // Add 14 methods (within limit)
        for (var i = 0; i < 14; i++)
        {
            classContent.AppendLine($"    public void Method{i}()");
            classContent.AppendLine("    {");
            classContent.AppendLine("        // Method body");
            classContent.AppendLine("    }");
        }

        classContent.AppendLine("}");

        var code = classContent.ToString();
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("class within both limits should not trigger");
    }

    [Fact]
    public void CountsOnlyActualMethods_NotAutoProperties()
    {
        // Arrange
        var code = """
                   public class TestClass
                   {
                       // 20 auto-properties (should NOT count as methods)
                       public string Prop1 { get; set; }
                       public string Prop2 { get; set; }
                       public string Prop3 { get; set; }
                       public string Prop4 { get; set; }
                       public string Prop5 { get; set; }
                       public string Prop6 { get; set; }
                       public string Prop7 { get; set; }
                       public string Prop8 { get; set; }
                       public string Prop9 { get; set; }
                       public string Prop10 { get; set; }
                       public string Prop11 { get; set; }
                       public string Prop12 { get; set; }
                       public string Prop13 { get; set; }
                       public string Prop14 { get; set; }
                       public string Prop15 { get; set; }
                       public string Prop16 { get; set; }
                       public string Prop17 { get; set; }
                       public string Prop18 { get; set; }
                       public string Prop19 { get; set; }
                       public string Prop20 { get; set; }
                       
                       // Only 5 actual methods (should count)
                       public void Method1() { }
                       public void Method2() { }
                       public void Method3() { }
                       public void Method4() { }
                       public void Method5() { }
                   }
                   """;
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("auto-properties should not count toward method limit");
    }

    [Fact]
    public void ReturnsDiagnostic_WhenBothThresholdsViolated()
    {
        // Arrange - create a class with both >500 lines AND >15 methods
        var classContent = new StringBuilder();
        classContent.AppendLine("public class TestClass");
        classContent.AppendLine("{");

        // Add 16 methods with enough content to exceed 500 lines
        for (var i = 0; i < 16; i++)
        {
            classContent.AppendLine($"    public void Method{i}()");
            classContent.AppendLine("    {");
            for (var j = 0; j < 30; j++) classContent.AppendLine($"        // Line {j}");
            classContent.AppendLine("    }");
        }

        classContent.AppendLine("}");

        var code = classContent.ToString();
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics.First().Message.Should().Contain("lines")
            .And.Contain("methods", "message should mention both violations");
    }

    [Fact]
    public void VerifiesCorrectMetadata_RuleIdSeverityCategory()
    {
        // Arrange
        var classContent = new StringBuilder();
        classContent.AppendLine("public class TestClass");
        classContent.AppendLine("{");

        // Create a class that violates the line count
        for (var i = 0; i < 500; i++) classContent.AppendLine($"    // Line {i}");

        classContent.AppendLine("}");

        var code = classContent.ToString();
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RuleId = "LNT005",
                Severity = Severity.Warning,
                Category = DiagnosticCategories.Design
            }, options => options.ExcludingMissingMembers());
    }

    [Fact]
    public void ReturnsNoDiagnostic_ForGeneratedCode()
    {
        // Arrange
        var classContent = new StringBuilder();
        classContent.AppendLine("// <auto-generated>");
        classContent.AppendLine("//     This code was generated by a tool.");
        classContent.AppendLine("// </auto-generated>");
        classContent.AppendLine("public class TestClass");
        classContent.AppendLine("{");

        // Add enough lines to exceed threshold
        for (var i = 0; i < 500; i++) classContent.AppendLine($"    // Line {i}");

        classContent.AppendLine("}");

        var code = classContent.ToString();
        SyntaxTree tree = CreateSyntaxTree(code);

        // Act
        var diagnostics = _rule.Analyze(tree).ToList();

        // Assert
        diagnostics.Should().BeEmpty("generated code should be skipped");
    }
}