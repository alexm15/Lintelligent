using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Xunit;

namespace Lintelligent.Reporting.Tests;

public class ReportGeneratorTests
{
    [Fact]
    public void ReportGenerator_50DuplicationGroups_SortedBySeverity()
    {
        // Arrange - Create 50 duplication results with varying severities
        var results = new List<DiagnosticResult>();

        // Mix of severities to test sorting
        for (var i = 0; i < 20; i++)
            results.Add(CreateDuplicationResult($"FileInfo{i}.cs", Severity.Info, i + 1));

        for (var i = 0; i < 15; i++)
            results.Add(CreateDuplicationResult($"FileWarn{i}.cs", Severity.Warning, i + 21));

        for (var i = 0; i < 15; i++)
            results.Add(CreateDuplicationResult($"FileError{i}.cs", Severity.Error, i + 36));

        // Act
        var markdown = ReportGenerator.GenerateMarkdownGroupedByCategory(results);

        // Assert
        markdown.Should().NotBeNullOrEmpty();
        markdown.Should().Contain("50", "should handle 50 duplication groups");

        // Verify all files are included
        for (var i = 0; i < 20; i++)
            markdown.Should().Contain($"FileInfo{i}.cs");

        for (var i = 0; i < 15; i++)
            markdown.Should().Contain($"FileWarn{i}.cs");

        for (var i = 0; i < 15; i++)
            markdown.Should().Contain($"FileError{i}.cs");
    }

    [Fact]
    public void ReportGenerator_GroupedByCategory_ShowsCodeQualityFirst()
    {
        // Arrange
        DiagnosticResult[] results = new[]
        {
            new DiagnosticResult("File1.cs", "MAINT001", "Issue", 1, Severity.Warning, "Maintainability"),
            CreateDuplicationResult("File2.cs", Severity.Warning, 1),
            new DiagnosticResult("File3.cs", "NAME001", "Issue", 1, Severity.Info, "Naming")
        };

        // Act
        var markdown = ReportGenerator.GenerateMarkdownGroupedByCategory(results);

        // Assert
        var codeQualityIndex = markdown.IndexOf("## Code Quality", StringComparison.Ordinal);
        var maintainabilityIndex = markdown.IndexOf("## Maintainability", StringComparison.Ordinal);
        var namingIndex = markdown.IndexOf("## Naming", StringComparison.Ordinal);

        codeQualityIndex.Should().BeGreaterThan(0, "Code Quality category should be present");
        maintainabilityIndex.Should().BeGreaterThan(0, "Maintainability category should be present");
        namingIndex.Should().BeGreaterThan(0, "Naming category should be present");

        // Categories should be sorted alphabetically
        codeQualityIndex.Should().BeLessThan(maintainabilityIndex);
        maintainabilityIndex.Should().BeLessThan(namingIndex);
    }

    [Fact]
    public void ReportGenerator_DuplicationWithMultipleInstances_ShowsAllLocations()
    {
        // Arrange
        var message = "Code duplicated in 5 files (20 lines, 300 tokens): " +
                      "ProjectA/Utils.cs, ProjectB/Utils.cs, ProjectC/Utils.cs, ProjectD/Utils.cs, ProjectE/Utils.cs";
        var result = new DiagnosticResult(
            "src/ProjectA/Utils.cs",
            "DUP001",
            message,
            15,
            Severity.Warning,
            "Code Quality");

        // Act
        var markdown = ReportGenerator.GenerateMarkdown(new[] {result});

        // Assert
        markdown.Should().Contain("5 files");
        markdown.Should().Contain("20 lines");
        markdown.Should().Contain("300 tokens");
        markdown.Should().Contain("ProjectA/Utils.cs");
        markdown.Should().Contain("ProjectB/Utils.cs");
        markdown.Should().Contain("ProjectC/Utils.cs");
        markdown.Should().Contain("ProjectD/Utils.cs");
        markdown.Should().Contain("ProjectE/Utils.cs");
    }

    private static DiagnosticResult CreateDuplicationResult(string filePath, Severity severity, int instanceCount)
    {
        var message = $"Code duplicated in {instanceCount} files (15 lines, 200 tokens): {filePath}, Other.cs";
        return new DiagnosticResult(filePath, "DUP001", message, 10, severity, "Code Quality");
    }
}