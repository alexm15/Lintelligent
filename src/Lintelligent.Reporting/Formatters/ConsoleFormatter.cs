using System.Globalization;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.Reporting.Formatters;

/// <summary>
///     Formats diagnostic results for console output with color coding and grouping.
/// </summary>
public sealed class ConsoleFormatter : IReportFormatter
{
    /// <inheritdoc />
    public string FormatName => "console";

    /// <inheritdoc />
    public string Format(IEnumerable<DiagnosticResult> results)
    {
        var resultList = results.ToList();

        if (resultList.Count == 0) return "✓ No issues found - All checks passed!";

        var output = new StringBuilder();
        output.AppendLine("Lintelligent Analysis Report");
        output.AppendLine("===========================");
        output.AppendLine();

        // Group by severity
        IOrderedEnumerable<IGrouping<Severity, DiagnosticResult>> bySeverity = resultList.GroupBy(r => r.Severity)
            .OrderByDescending(g => g.Key); // Error, Warning, Info

        foreach (IGrouping<Severity, DiagnosticResult> severityGroup in bySeverity)
        {
            output.AppendLine(CultureInfo.InvariantCulture,
                $"{GetSeverityIcon(severityGroup.Key)} {severityGroup.Key.ToString().ToUpperInvariant()} ({severityGroup.Count()})");
            output.AppendLine(new string('-', 50));

            foreach (DiagnosticResult result in severityGroup)
            {
                output.AppendLine(CultureInfo.InvariantCulture, $"  [{result.RuleId}] {result.Category}");
                output.AppendLine(CultureInfo.InvariantCulture, $"  File: {result.FilePath}:{result.LineNumber}");
                output.AppendLine(CultureInfo.InvariantCulture, $"  {result.Message}");
                output.AppendLine();
            }
        }

        // Summary
        output.AppendLine("Summary");
        output.AppendLine("=======");
        output.AppendLine(CultureInfo.InvariantCulture, $"Total issues: {resultList.Count}");

        var errorCount = resultList.Count(r => r.Severity == Severity.Error);
        var warningCount = resultList.Count(r => r.Severity == Severity.Warning);
        var infoCount = resultList.Count(r => r.Severity == Severity.Info);

        if (errorCount > 0)
            output.AppendLine(CultureInfo.InvariantCulture, $"  Errors: {errorCount}");
        if (warningCount > 0)
            output.AppendLine(CultureInfo.InvariantCulture, $"  Warnings: {warningCount}");
        if (infoCount > 0)
            output.AppendLine(CultureInfo.InvariantCulture, $"  Info: {infoCount}");

        return output.ToString();
    }

    private static string GetSeverityIcon(Severity severity)
    {
        return severity switch
        {
            Severity.Error => "✗",
            Severity.Warning => "⚠",
            Severity.Info => "ℹ",
            _ => "?"
        };
    }
}
