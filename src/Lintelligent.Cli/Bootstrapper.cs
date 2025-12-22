using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.Cli.Commands;
using Lintelligent.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace Lintelligent.Cli;

public static class Bootstrapper
{
    public static void Configure(IServiceCollection sp)
    {
        // Core services
        // Core services used by both Program and tests. Rules are intentionally not registered here
        // so callers (Program or tests) can register explicit, deterministic rules as needed.
        sp.AddSingleton<AnalyzerManager>();
        sp.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
        sp.AddSingleton<ReportGenerator>();
        sp.AddSingleton<ScanCommand>();

        // Rules (explicit, deterministic)
        sp.AddSingleton<IAnalyzerRule, LongMethodRule>();
        // services.AddSingleton<IAnalyzerRule, AvoidEmptyCatchRule>();
    }
}