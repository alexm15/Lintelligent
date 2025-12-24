using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.Cli.Commands;
using Lintelligent.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace Lintelligent.Cli;

public static class Bootstrapper
{
    public static void Configure(IServiceCollection services)
    {
        // Core services used by both Program and tests. Rules are intentionally not registered here
        // so callers (Program or tests) can register explicit, deterministic rules as needed.
        services.AddSingleton<AnalyzerManager>();
        services.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
        services.AddSingleton<ReportGenerator>();
        
        // Commands (transient lifetime - new instance per execution to avoid state leakage)
        services.AddTransient<ScanCommand>();

        // Rules (explicit, deterministic)
        services.AddSingleton<IAnalyzerRule, LongMethodRule>();
        // services.AddSingleton<IAnalyzerRule, AvoidEmptyCatchRule>();
    }
}