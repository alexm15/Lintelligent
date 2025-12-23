using System.Linq;
using Lintelligent.AnalyzerEngine;
using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.Reporting;

public class ReportGenerator
{
    public string GenerateMarkdown(IEnumerable<DiagnosticResult> results)
    {
        // Compose final document using a raw interpolated string and concatenate entries
        return $"""
                # Lintelligent Report

                {CollectOutput(results)}
                """;
    }

    private static string CollectOutput(IEnumerable<DiagnosticResult> results)
    {
        return string.Concat(results.Select(result => $"""
                                                       - **File:** {result.FilePath}
                                                         - **Rule:** {result.RuleId}
                                                         - **Severity:** {result.Severity}
                                                         - **Category:** {result.Category}
                                                         - **Message:** {result.Message}
                                                         - **Line:** {result.LineNumber}

                                                       """
        ));
    }

    public string GenerateMarkdownGroupedByCategory(IEnumerable<DiagnosticResult> results)
    {
        var grouped = results.GroupBy(r => r.Category)
                            .OrderBy(g => g.Key);

        var output = "# Lintelligent Report\n\n";

        foreach (var group in grouped)
        {
            output += $"## {group.Key}\n\n";
            output += CollectOutput(group);
        }

        return output;
    }
}