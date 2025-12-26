using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.Reporting.Formatters;

/// <summary>
/// Formats diagnostic results for console output with color coding and grouping.
/// </summary>
public sealed class ConsoleFormatter : IReportFormatter
{
    /// <inheritdoc />
    public string FormatName => "console";

    /// <inheritdoc />
    public string Format(IEnumerable<DiagnosticResult> results)
    {
        var resultList = results.ToList();
        
        if (resultList.Count == 0)
        {
            return "✓ No issues found - All checks passed!";
        }

        var output = new System.Text.StringBuilder();
        output.AppendLine("Lintelligent Analysis Report");
        output.AppendLine("===========================");
        output.AppendLine();

        // Group by severity
        var bySeverity = resultList.GroupBy(r => r.Severity)
            .OrderByDescending(g => g.Key); // Error, Warning, Info

        foreach (var severityGroup in bySeverity)
        {
            output.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{GetSeverityIcon(severityGroup.Key)} {severityGroup.Key.ToString().ToUpperInvariant()} ({severityGroup.Count()})");
            output.AppendLine(new string('-', 50));

            foreach (var result in severityGroup)
            {
                output.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  [{result.RuleId}] {result.Category}");
                output.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  File: {result.FilePath}:{result.LineNumber}");
                output.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  {result.Message}");
                output.AppendLine();
            }
        }

        // Summary
        output.AppendLine("Summary");
        output.AppendLine("=======");
        output.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Total issues: {resultList.Count}");
        
        var errorCount = resultList.Count(r => r.Severity == AnalyzerEngine.Abstractions.Severity.Error);
        var warningCount = resultList.Count(r => r.Severity == AnalyzerEngine.Abstractions.Severity.Warning);
        var infoCount = resultList.Count(r => r.Severity == AnalyzerEngine.Abstractions.Severity.Info);
        
        if (errorCount > 0)
            output.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  Errors: {errorCount}");
        if (warningCount > 0)
            output.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  Warnings: {warningCount}");
        if (infoCount > 0)
            output.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  Info: {infoCount}");

        return output.ToString();
    }

    private static string GetSeverityIcon(AnalyzerEngine.Abstractions.Severity severity)
    {
        return severity switch
        {
            AnalyzerEngine.Abstractions.Severity.Error => "✗",
            AnalyzerEngine.Abstractions.Severity.Warning => "⚠",
            AnalyzerEngine.Abstractions.Severity.Info => "ℹ",
            _ => "?"
        };
    }
}
