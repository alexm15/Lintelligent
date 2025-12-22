using Lintelligent.Reporting;

namespace Lintelligent.Cli.Commands;

public sealed class ScanCommand(
    AnalyzerEngine.Analysis.AnalyzerEngine engine,
    ReportGenerator reporter)
{
    public async Task ExecuteAsync(string[] args)
    {
        var path = args.FirstOrDefault() ?? ".";

        var results = engine.Analyze(path);
        var report = reporter.GenerateMarkdown(results);

        Console.WriteLine(report);

        await Task.CompletedTask;
    }
}