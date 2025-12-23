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
/// 4. Generate and display report
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

        // Create provider to discover files from file system
        var codeProvider = new FileSystemCodeProvider(path);
        var syntaxTrees = codeProvider.GetSyntaxTrees();

        // Analyze syntax trees (no IO in engine)
        var results = engine.Analyze(syntaxTrees);
        var report = reporter.GenerateMarkdown(results);

        Console.WriteLine(report);

        await Task.CompletedTask;
    }
}