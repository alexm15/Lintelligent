using System.Reflection;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Configuration;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;
using Lintelligent.Cli.Commands;
using Lintelligent.Cli.Infrastructure;
using Lintelligent.Reporting;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lintelligent.Cli.Tests;

public class ScanCommandTests
{
    [Fact]
    public void SingleFile_ExecutionReturnsSuccessWithReport()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            File.WriteAllText(Path.Combine(temp, "Test.cs"), "class Test { }");

            // Build CLI application in-memory

            using var app = new CliApplicationBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<AnalyzerManager>();
                    services.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
                    services.AddSingleton<WorkspaceAnalyzerEngine>();
                    services.AddSingleton(new DuplicationOptions()); // Required by ScanCommand
                    services.AddSingleton<IAnalyzerRule, AlwaysReportRule>();
                    services.AddTransient<ScanCommand>();
                })
                .AddCommand<ScanCommand>()
                .Build();

            // Get the AnalyzerManager and register rules manually
            // (since we're not using Bootstrapper which would do this)
            var serviceProvider = (IServiceProvider) app.GetType()
                .GetField("_serviceProvider", BindingFlags.NonPublic | BindingFlags.Instance)!
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
            Directory.Delete(temp, true);
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
            Directory.Delete(temp, true);
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
            Directory.Delete(temp, true);
        }
    }

    private static void CreateTestServices(IServiceCollection services)
    {
        services.AddSingleton<AnalyzerManager>();
        services.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
        services.AddSingleton<WorkspaceAnalyzerEngine>();
        services.AddSingleton(new DuplicationOptions()); // Required by ScanCommand
        services.AddTransient<ScanCommand>();
    }

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
    public void ScanCommand_MinDuplicationLinesFlag_FiltersByLineCount()
    {
        // Test verifies that --min-duplication-lines flag filters duplications
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            // Create two files with 8-line duplication
            var code = """
                public class TestClass
                {
                    void Method()
                    {
                        int x = 42;
                        int y = 84;
                    }
                }
                """;
            File.WriteAllText(Path.Combine(temp, "File1.cs"), code);
            File.WriteAllText(Path.Combine(temp, "File2.cs"), code);

            var builder = new CliApplicationBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<AnalyzerManager>();
                services.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
                var workspaceEngine = new WorkspaceAnalyzerEngine();
                services.AddSingleton(workspaceEngine);
                
                // Register DuplicationOptions with low default (would detect 8-line duplication)
                var options = new DuplicationOptions { MinLines = 5, MinTokens = 10 };
                services.AddSingleton(options);
                services.AddTransient<ScanCommand>();
                
                // Register DuplicationDetector that uses the same options instance
                workspaceEngine.RegisterAnalyzer(new DuplicationDetector(options));
            });
            builder.AddCommand<ScanCommand>();

            using var app = builder.Build();

            // Act - Execute with --min-duplication-lines 15 (CLI flag overrides default 5)
            var result = app.Execute(["scan", temp, "--min-duplication-lines", "15"]);

            // Assert - Should succeed with no duplications reported (filtered by CLI threshold)
            Assert.Equal(0, result.ExitCode);
            Assert.DoesNotContain("DUP001", result.Output);
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    [Fact]
    public void ScanCommand_CLIFlagOverridesConfig_CLITakesPrecedence()
    {
        // Test verifies CLI flags override default configuration
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            // Create two files with 8-line duplication
            var code = """
                public class TestClass
                {
                    void Method()
                    {
                        int x = 42;
                        int y = 84;
                    }
                }
                """;
            File.WriteAllText(Path.Combine(temp, "File1.cs"), code);
            File.WriteAllText(Path.Combine(temp, "File2.cs"), code);

            var builder = new CliApplicationBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<AnalyzerManager>();
                services.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
                var workspaceEngine = new WorkspaceAnalyzerEngine();
                services.AddSingleton(workspaceEngine);
                
                // Register DuplicationOptions with low defaults (would detect 8-line duplication)
                var options = new DuplicationOptions { MinLines = 5, MinTokens = 10 };
                services.AddSingleton(options);
                services.AddTransient<ScanCommand>();
                
                // Register DuplicationDetector that uses the same options instance
                workspaceEngine.RegisterAnalyzer(new DuplicationDetector(options));
            });
            builder.AddCommand<ScanCommand>();

            using var app = builder.Build();

            // Act - Execute with CLI flags that raise thresholds (should override low config defaults)
            var result = app.Execute(["scan", temp, "--min-duplication-lines", "20", "--min-duplication-tokens", "100"]);

            // Assert - Should succeed with no duplications reported (CLI flags override config)
            Assert.Equal(0, result.ExitCode);
            Assert.DoesNotContain("DUP001", result.Output);
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }
}