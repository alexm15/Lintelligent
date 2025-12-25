using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.Cli.Infrastructure;
using Lintelligent.Cli.Providers;
using Lintelligent.Reporting;

#pragma warning disable MA0006 // Use string.Equals instead of == operator
#pragma warning disable MA0026 // TODO comment detected
#pragma warning disable S1135 // TODO comment detected
#pragma warning disable MA0015 // ArgumentException parameter name
#pragma warning disable MA0004 // ConfigureAwait(false) not needed in CLI

namespace Lintelligent.Cli.Commands;

/// <summary>
///     CLI command that scans a project directory and analyzes C# source files.
/// </summary>
/// <remarks>
///     This command orchestrates the analysis workflow:
///     1. Create FileSystemCodeProvider to discover .cs files
///     2. Get syntax trees from provider
///     3. Pass trees to AnalyzerEngine for analysis
///     4. Filter results by severity (if --severity specified)
///     5. Generate and display report
///     The command is responsible for file system access (via FileSystemCodeProvider).
///     The AnalyzerEngine core performs no IO operations, maintaining constitutional compliance.
/// </remarks>
public sealed class ScanCommand(AnalyzerEngine.Analysis.AnalyzerEngine engine) : IAsyncCommand
{
    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        try
        {
            var path = args.Length > 1 ? args[1] : ".";
            var severityFilter = ParseSeverityFilter(args);
            var groupBy = ParseGroupByOption(args);
            _ = ParseFormatOption(args); // TODO: Use format option when implementing output formatting
            _ = ParseOutputOption(args); // TODO: Use output path when implementing file output

            // Create provider to discover files from file system
            var codeProvider = new FileSystemCodeProvider(path);
            var syntaxTrees = codeProvider.GetSyntaxTrees();

            // Analyze syntax trees (no IO in engine)
            var results = engine.Analyze(syntaxTrees);

            // Filter by severity if specified
            if (severityFilter.HasValue) results = results.Where(r => r.Severity == severityFilter.Value);

            // Materialize results before passing to report generator
            var materializedResults = results.ToList();

            // Generate report with optional grouping
            var report = groupBy switch
            {
                "category" => ReportGenerator.GenerateMarkdownGroupedByCategory(materializedResults),
                _ => ReportGenerator.GenerateMarkdown(materializedResults)
            };

            // Return success with report in Output
            return await Task.FromResult(CommandResult.Success(report));
        }
        catch (ArgumentException ex)
        {
            // ArgumentException and derived types → exit code 2
            return CommandResult.Failure(2, ex.Message);
        }
        catch (Exception ex)
        {
            // All other exceptions → exit code 1
            return CommandResult.Failure(1, ex.Message);
        }
    }

    private static Severity? ParseSeverityFilter(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--severity")
                return args[i + 1].ToLowerInvariant() switch
                {
                    "error" => Severity.Error,
                    "warning" => Severity.Warning,
                    "info" => Severity.Info,
                    _ => null
                };

        return null;
    }

    private static string? ParseGroupByOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--group-by")
            {
                var value = args[i + 1].ToLowerInvariant();
                return value == "category" ? value : null;
            }

        return null;
    }
    
    private static string ParseFormatOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--format")
            {
                var value = args[i + 1].ToLowerInvariant();
                var validFormats = new[] { "json", "sarif", "markdown" };
                if (!validFormats.Contains(value))
                {
                    throw new ArgumentException(
                        $"Invalid format '{value}'. Valid formats: {string.Join(", ", validFormats)}");
                }
                return value;
            }

        return "markdown"; // Default format
    }
    
    private static string? ParseOutputOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--output")
                return args[i + 1];

        return null; // Default: stdout
    }
}