namespace Lintelligent.Reporting.Tests.Formatters;

using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Xunit;

public class MarkdownFormatterTests
{
    [Fact]
    public void MarkdownFormatter_DuplicationResults_IncludesCodeSnippets()
    {
        // Arrange
        var results = new[]
        {
            CreateDuplicationResult(
                "src/ProjectA/Calculator.cs",
                25,
                "Code duplicated in 3 files (15 lines, 200 tokens): ProjectA/Calculator.cs, ProjectB/Math.cs, ProjectC/Utils.cs"),
            CreateDuplicationResult(
                "src/ProjectB/DataProcessor.cs",
                10,
                "Code duplicated in 2 files (8 lines, 95 tokens): ProjectB/DataProcessor.cs, ProjectC/Handler.cs")
        };

        // Act
        var markdown = Reporting.ReportGenerator.GenerateMarkdown(results);

        // Assert
        markdown.Should().NotBeNullOrEmpty();
        markdown.Should().Contain("# Lintelligent Report");
        markdown.Should().Contain("DUP001");
        markdown.Should().Contain("Calculator.cs");
        markdown.Should().Contain("DataProcessor.cs");
        markdown.Should().Contain("15 lines");
        markdown.Should().Contain("200 tokens");
        markdown.Should().Contain("3 files");
        markdown.Should().Contain("2 files");
    }

    [Fact]
    public void MarkdownFormatter_GroupedByCategory_ShowsDuplicationCategory()
    {
        // Arrange
        var results = new[]
        {
            CreateDuplicationResult("File1.cs", 1, "Duplication 1"),
            CreateDuplicationResult("File2.cs", 1, "Duplication 2"),
            new DiagnosticResult("File3.cs", "LINT001", "Other issue", 1, Severity.Warning, "Maintainability")
        };

        // Act
        var markdown = Reporting.ReportGenerator.GenerateMarkdownGroupedByCategory(results);

        // Assert
        markdown.Should().Contain("## Code Quality", "because duplications are in Code Quality category");
        markdown.Should().Contain("## Maintainability");
    }

    [Fact]
    public void MarkdownFormatter_EmptyResults_ShowsHeader()
    {
        // Arrange
        var results = Array.Empty<DiagnosticResult>();

        // Act
        var markdown = Reporting.ReportGenerator.GenerateMarkdown(results);

        // Assert
        markdown.Should().Contain("# Lintelligent Report");
    }

    private static DiagnosticResult CreateDuplicationResult(string filePath, int lineNumber, string message)
    {
        return new DiagnosticResult(filePath, "DUP001", message, lineNumber, Severity.Warning, "Code Quality");
    }
}
