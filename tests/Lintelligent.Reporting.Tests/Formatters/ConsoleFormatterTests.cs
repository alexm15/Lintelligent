using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.Reporting.Formatters;
using Xunit;

namespace Lintelligent.Reporting.Tests.Formatters;

public class ConsoleFormatterTests
{
    [Fact]
    public void ConsoleFormatter_TenDuplications_GroupedByHash()
    {
        // Arrange - Create 10 duplication results
        var results = new List<DiagnosticResult>();
        for (var i = 1; i <= 10; i++)
        {
            var message =
                $"Code duplicated in 3 files (15 lines, 200 tokens): ProjectA/File{i}.cs, ProjectB/File{i}.cs, ProjectC/File{i}.cs";
            results.Add(new DiagnosticResult(
                $"src/ProjectA/File{i}.cs",
                "DUP001",
                message,
                10,
                Severity.Warning,
                "Code Quality"));
        }

        // Act
        var formatter = new ConsoleFormatter();
        var output = formatter.Format(results);

        // Assert
        output.Should().NotBeNullOrEmpty();
        output.Should().Contain("10"); // Should show total count
        output.Should().Contain("DUP001"); // Should show rule ID
        output.Should().Contain("duplicated"); // Should indicate duplication

        // Should list all affected files
        for (var i = 1; i <= 10; i++) output.Should().Contain($"File{i}.cs");
    }

    [Fact]
    public void ConsoleFormatter_EmptyResults_ShowsSuccessMessage()
    {
        // Arrange
        DiagnosticResult[] results = Array.Empty<DiagnosticResult>();

        // Act
        var formatter = new ConsoleFormatter();
        var output = formatter.Format(results);

        // Assert
        output.Should().NotBeNullOrEmpty();
        output.Should().Contain("No issues found", "because empty results should show success message");
    }

    [Fact]
    public void ConsoleFormatter_MixedSeverities_GroupsBySeverity()
    {
        // Arrange
        DiagnosticResult[] results = new[]
        {
            CreateDuplicationResult("File1.cs", Severity.Error),
            CreateDuplicationResult("File2.cs", Severity.Warning),
            CreateDuplicationResult("File3.cs", Severity.Warning),
            CreateDuplicationResult("File4.cs", Severity.Info)
        };

        // Act
        var formatter = new ConsoleFormatter();
        var output = formatter.Format(results);

        // Assert
        output.Should().Contain("Error", "should show error severity");
        output.Should().Contain("Warning", "should show warning severity");
        output.Should().Contain("Info", "should show info severity");
    }

    private static DiagnosticResult CreateDuplicationResult(string filePath, Severity severity)
    {
        var message = $"Code duplicated in 2 files (10 lines, 100 tokens): {filePath}, Other.cs";
        return new DiagnosticResult(filePath, "DUP001", message, 1, severity, "Code Quality");
    }
}