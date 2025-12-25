namespace Lintelligent.Reporting.Tests.Formatters;

using System.Text.Json;
using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.Reporting.Formatters;
using Xunit;

public class JsonFormatterTests
{
    private readonly JsonFormatter _formatter = new();

    [Fact]
    public void Format_WithValidResults_ProducesValidJson()
    {
        // Arrange
        var results = new[]
        {
            CreateDiagnosticResult("File1.cs", Severity.Error, "Complexity"),
            CreateDiagnosticResult("File2.cs", Severity.Warning, "Naming")
        };

        // Act
        var json = _formatter.Format(results);

        // Assert
        var parsed = JsonDocument.Parse(json); // Should not throw
        parsed.Should().NotBeNull();
    }

    [Fact]
    public void Format_WithEmptyResults_ReturnsSuccessWithZeroViolations()
    {
        // Arrange
        var results = Array.Empty<DiagnosticResult>();

        // Act
        var json = _formatter.Format(results);

        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("success");
        root.GetProperty("summary").GetProperty("total").GetInt32().Should().Be(0);
        root.GetProperty("violations").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void Format_WithSingleViolation_ContainsAllFields()
    {
        // Arrange
        var result = CreateDiagnosticResult("Test.cs", Severity.Warning, "Maintainability", 42, "LINT001", "Method too long");

        // Act
        var json = _formatter.Format(new[] { result });

        // Assert
        using var doc = JsonDocument.Parse(json);
        var violation = doc.RootElement.GetProperty("violations")[0];

        violation.GetProperty("filePath").GetString().Should().Be("Test.cs");
        violation.GetProperty("lineNumber").GetInt32().Should().Be(42);
        violation.GetProperty("ruleId").GetString().Should().Be("LINT001");
        violation.GetProperty("severity").GetString().Should().Be("warning");
        violation.GetProperty("category").GetString().Should().Be("Maintainability");
        violation.GetProperty("message").GetString().Should().Be("Method too long");
    }

    [Fact]
    public void Format_WithMultipleSeverities_GroupsBySeverity()
    {
        // Arrange
        var results = new[]
        {
            CreateDiagnosticResult("File1.cs", Severity.Error, "Complexity"),
            CreateDiagnosticResult("File2.cs", Severity.Error, "Complexity"),
            CreateDiagnosticResult("File3.cs", Severity.Warning, "Naming"),
            CreateDiagnosticResult("File4.cs", Severity.Info, "Documentation"),
            CreateDiagnosticResult("File5.cs", Severity.Warning, "Naming")
        };

        // Act
        var json = _formatter.Format(results);

        // Assert
        using var doc = JsonDocument.Parse(json);
        var summary = doc.RootElement.GetProperty("summary");
        var bySeverity = summary.GetProperty("bySeverity");

        summary.GetProperty("total").GetInt32().Should().Be(5);
        bySeverity.GetProperty("error").GetInt32().Should().Be(2);
        bySeverity.GetProperty("warning").GetInt32().Should().Be(2);
        bySeverity.GetProperty("info").GetInt32().Should().Be(1);
    }

    [Fact]
    public void Format_WithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var result = CreateDiagnosticResult(
            "File.cs",
            Severity.Error,
            "Test",
            1,
            "LINT001",
            "Message with \"quotes\" and\nnewline and unicode: 你好"
        );

        // Act
        var json = _formatter.Format(new[] { result });

        // Assert
        using var doc = JsonDocument.Parse(json);
        var message = doc.RootElement.GetProperty("violations")[0].GetProperty("message").GetString();

        message.Should().Contain("\"quotes\"");
        message.Should().Contain("\n");
        message.Should().Contain("你好");
    }

    [Fact]
    public void Format_WithCamelCaseConvention_UsesCorrectPropertyNames()
    {
        // Arrange
        var result = CreateDiagnosticResult("File.cs", Severity.Error, "Test");

        // Act
        var json = _formatter.Format(new[] { result });

        // Assert
        json.Should().Contain("\"status\"");
        json.Should().Contain("\"summary\"");
        json.Should().Contain("\"total\"");
        json.Should().Contain("\"bySeverity\"");
        json.Should().Contain("\"violations\"");
        json.Should().Contain("\"filePath\"");
        json.Should().Contain("\"lineNumber\"");
        json.Should().Contain("\"ruleId\"");
        json.Should().Contain("\"severity\"");
        json.Should().Contain("\"category\"");
        json.Should().Contain("\"message\"");
    }

    [Fact]
    public void Format_WithSameInput_ProducesDeterministicOutput()
    {
        // Arrange
        var results = new[]
        {
            CreateDiagnosticResult("File1.cs", Severity.Error, "Complexity"),
            CreateDiagnosticResult("File2.cs", Severity.Warning, "Naming")
        };

        // Act
        var json1 = _formatter.Format(results);
        var json2 = _formatter.Format(results);

        // Assert
        json1.Should().Be(json2);
    }

    [Fact]
    public void Format_WithAllCategories_HandlesEightDistinctCategories()
    {
        // Arrange (8 categories from Feature 019 as mentioned in spec)
        var results = new[]
        {
            CreateDiagnosticResult("File1.cs", Severity.Error, "Complexity"),
            CreateDiagnosticResult("File2.cs", Severity.Warning, "Naming"),
            CreateDiagnosticResult("File3.cs", Severity.Info, "Documentation"),
            CreateDiagnosticResult("File4.cs", Severity.Error, "Maintainability"),
            CreateDiagnosticResult("File5.cs", Severity.Warning, "Performance"),
            CreateDiagnosticResult("File6.cs", Severity.Info, "Security"),
            CreateDiagnosticResult("File7.cs", Severity.Error, "Readability"),
            CreateDiagnosticResult("File8.cs", Severity.Warning, "BestPractices")
        };

        // Act
        var json = _formatter.Format(results);

        // Assert
        using var doc = JsonDocument.Parse(json);
        var violations = doc.RootElement.GetProperty("violations");

        violations.GetArrayLength().Should().Be(8);
        doc.RootElement.GetProperty("summary").GetProperty("total").GetInt32().Should().Be(8);
    }

    [Fact]
    public void Format_WithStatusField_ReturnsSuccess()
    {
        // Arrange
        var results = new[] { CreateDiagnosticResult("File.cs", Severity.Error, "Test") };

        // Act
        var json = _formatter.Format(results);

        // Assert
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("success");
    }

    [Fact]
    public void Format_WithSummaryTotals_MatchesViolationCount()
    {
        // Arrange
        var results = new[]
        {
            CreateDiagnosticResult("File1.cs", Severity.Error, "Complexity"),
            CreateDiagnosticResult("File2.cs", Severity.Warning, "Naming"),
            CreateDiagnosticResult("File3.cs", Severity.Info, "Documentation")
        };

        // Act
        var json = _formatter.Format(results);

        // Assert
        using var doc = JsonDocument.Parse(json);
        var summary = doc.RootElement.GetProperty("summary");
        var violations = doc.RootElement.GetProperty("violations");

        summary.GetProperty("total").GetInt32().Should().Be(violations.GetArrayLength());
        summary.GetProperty("total").GetInt32().Should().Be(3);
    }

    [Fact]
    public void FormatName_ReturnsJson()
    {
        // Act & Assert
        _formatter.FormatName.Should().Be("json");
    }

    // Helper method
    private static DiagnosticResult CreateDiagnosticResult(
        string filePath,
        Severity severity,
        string category,
        int lineNumber = 1,
        string ruleId = "LINT001",
        string message = "Test violation")
    {
        return new DiagnosticResult(
            filePath,
            ruleId,
            message,
            lineNumber,
            severity,
            category
        );
    }
}
