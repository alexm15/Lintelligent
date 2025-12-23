using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.Cli.Providers;
using Lintelligent.Reporting;

namespace Lintelligent.Cli.Commands;

/// <summary>
/// CLI command that scans a project directory and analyzes C# source files.
/// </summary>
/// <remarks>
/// This command orchestrates the analysis workflow:
/// 1. Create FileSystemCodeProvider to discover .cs files
/// 2. Get syntax trees from provider
/// 3. Pass trees to AnalyzerEngine for analysis
/// 4. Filter results by severity (if --severity specified)
/// 5. Generate and display report
/// 
/// The command is responsible for file system access (via FileSystemCodeProvider).
/// The AnalyzerEngine core performs no IO operations, maintaining constitutional compliance.
/// </remarks>
public sealed class ScanCommand(
    AnalyzerEngine.Analysis.AnalyzerEngine engine,
    ReportGenerator reporter)
{
    public async Task ExecuteAsync(string[] args)
    {
        var path = args.FirstOrDefault() ?? ".";
        var severityFilter = ParseSeverityFilter(args);
        var groupBy = ParseGroupByOption(args);

        // Create provider to discover files from file system
        var codeProvider = new FileSystemCodeProvider(path);
        var syntaxTrees = codeProvider.GetSyntaxTrees();

        // Analyze syntax trees (no IO in engine)
        var results = engine.Analyze(syntaxTrees);

        // Filter by severity if specified
        if (severityFilter.HasValue)
        {
            results = results.Where(r => r.Severity == severityFilter.Value);
        }

        // Materialize results before passing to report generator
        var materializedResults = results.ToList();

        // Generate report with optional grouping
        var report = groupBy switch
        {
            "category" => reporter.GenerateMarkdownGroupedByCategory(materializedResults),
            _ => reporter.GenerateMarkdown(materializedResults)
        };

        Console.WriteLine(report);

        await Task.CompletedTask;
    }

    private static Severity? ParseSeverityFilter(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--severity")
            {
                return args[i + 1].ToLowerInvariant() switch
                {
                    "error" => Severity.Error,
                    "warning" => Severity.Warning,
                    "info" => Severity.Info,
                    _ => null
                };
            }
        }
        return null;
    }

    private static string? ParseGroupByOption(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--group-by")
            {
                var value = args[i + 1].ToLowerInvariant();
                return value == "category" ? value : null;
            }
        }
        return null;
    }
}