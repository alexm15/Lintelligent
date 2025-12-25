namespace Lintelligent.Reporting.Formatters;

using System.Text.Encodings.Web;
using System.Text.Json;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.Reporting.Formatters.Models;

/// <summary>
/// Formats diagnostic results as JSON for CI/CD pipeline integration.
/// Constitutional Compliance: Principle I (Layered Architecture), Principle VII (Testability).
/// FR-001: JSON format support for machine-readable output.
/// FR-012: Proper escaping of special characters (quotes, newlines, Unicode).
/// FR-014: Graceful handling of empty result sets.
/// </summary>
public class JsonFormatter : IReportFormatter
{
    // FR-012: Cached JsonSerializerOptions to avoid recreating on each call (CA1869)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public string FormatName => "json";

    /// <inheritdoc />
    public string Format(IEnumerable<DiagnosticResult> results)
    {
        var diagnostics = results.ToList();

        // FR-014: Handle empty result sets gracefully
        var violations = diagnostics.Select(MapToViolationModel).ToList();

        var summary = new SummaryModel
        {
            Total = violations.Count,
            BySeverity = violations
                .GroupBy(v => v.Severity)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        var output = new JsonOutputModel
        {
            Status = "success",
            Summary = summary,
            Violations = violations
        };

        // FR-012: Special character escaping via JavaScriptEncoder
        // SC-003: camelCase naming convention via JsonPropertyName attributes
        return JsonSerializer.Serialize(output, JsonOptions);
    }

    private static ViolationModel MapToViolationModel(DiagnosticResult result)
    {
        return new ViolationModel
        {
            FilePath = result.FilePath,
            LineNumber = result.LineNumber,
            RuleId = result.RuleId,
            Severity = result.Severity.ToString().ToLowerInvariant(), // Error → "error"
            Category = result.Category,
            Message = result.Message
        };
    }
}
