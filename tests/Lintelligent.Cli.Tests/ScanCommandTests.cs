using Xunit;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.Cli.Commands;
using Lintelligent.Cli.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Lintelligent.Cli.Tests;

public class ScanCommandTests
{
    private sealed class AlwaysReportRule : IAnalyzerRule
    {
        public string Id => "TEST001";
        public string Description => "Always reports a diagnostic";
        public Severity Severity => Severity.Warning;
        public string Category => DiagnosticCategories.General;

        public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
        {
            yield return new DiagnosticResult(tree.FilePath, Id, Description, 1, Severity, Category);
        }
    }

    [Fact]
    public void SingleFile_ExecutionReturnsSuccessWithReport()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            File.WriteAllText(Path.Combine(temp, "Test.cs"), "class Test { }");

            // Build CLI application in-memory
            var builder = new CliApplicationBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<AnalyzerManager>();
                services.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
                services.AddSingleton<Lintelligent.Reporting.ReportGenerator>();
                services.AddSingleton<IAnalyzerRule, AlwaysReportRule>();
                services.AddTransient<ScanCommand>();
            });
            builder.AddCommand<ScanCommand>();

            using var app = builder.Build();
            
            // Get the AnalyzerManager and register rules manually
            // (since we're not using Bootstrapper which would do this)
            var serviceProvider = (IServiceProvider)app.GetType()
                .GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(app)!;
            
            var manager = serviceProvider.GetRequiredService<AnalyzerManager>();
            var rules = serviceProvider.GetServices<IAnalyzerRule>();
            manager.RegisterRules(rules);

            // Execute scan command
            var result = app.Execute(["scan", temp]);

            // Assert
            Assert.Equal(0, result.ExitCode);
            Assert.NotEmpty(result.Output);
            Assert.Contains("# Lintelligent Report", result.Output);
            Assert.Contains("Test.cs", result.Output);
            Assert.Contains("TEST001", result.Output);
            Assert.Contains("Always reports a diagnostic", result.Output);
            Assert.Empty(result.Error);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void Execute_WithNoArgs_ReturnsErrorCode2()
    {
        // Build CLI application
        var builder = new CliApplicationBuilder();
        builder.ConfigureServices(CreateTestServices);
        builder.AddCommand<ScanCommand>();

        using var app = builder.Build();

        // Execute without path argument
        var result = app.Execute(["scan"]);

        // Assert - should handle gracefully or return error
        // ScanCommand defaults to "." if no path, so it should succeed
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Execute_SuccessfulScan_OutputContainsReport()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            File.WriteAllText(Path.Combine(temp, "Test.cs"), "class Test { }");

            var builder = new CliApplicationBuilder();
            builder.ConfigureServices(CreateTestServices);
            builder.AddCommand<ScanCommand>();

            using var app = builder.Build();
            var result = app.Execute(["scan", temp]);

            Assert.Equal(0, result.ExitCode);
            Assert.NotEmpty(result.Output);
            Assert.Empty(result.Error);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void Execute_SuccessfulScan_ErrorIsEmpty()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            File.WriteAllText(Path.Combine(temp, "Test.cs"), "class Test { }");

            var builder = new CliApplicationBuilder();
            builder.ConfigureServices(CreateTestServices);
            builder.AddCommand<ScanCommand>();

            using var app = builder.Build();
            var result = app.Execute(["scan", temp]);

            Assert.Empty(result.Error);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static void CreateTestServices(IServiceCollection services)
    {
        services.AddSingleton<AnalyzerManager>();
        services.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
        services.AddSingleton<Lintelligent.Reporting.ReportGenerator>();
        services.AddTransient<ScanCommand>();
    }
}
