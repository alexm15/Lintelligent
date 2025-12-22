using Xunit;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.Cli.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Lintelligent.Cli.Tests;

public class ScanCommandTests : TestBase
{
    private sealed class AlwaysReportRule : IAnalyzerRule
    {
        public string Id => "TEST001";
        public string Description => "Always reports a diagnostic";

        public DiagnosticResult Analyze(SyntaxTree tree)
        {
            return new DiagnosticResult(tree.FilePath, Id, Description, 1);
        }
    }

    [Fact]
    public async Task SingleFile_ExecutionWritesReportToConsole()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var filePath = Path.Combine(temp, "One.cs");
            await File.WriteAllTextAsync(filePath, "class C { }\n");

            using var host = CreateHost(configure: services =>
            {
                services.AddSingleton<IAnalyzerRule, AlwaysReportRule>();
            });

            var manager = host.Services.GetRequiredService<AnalyzerManager>();
            manager.RegisterRules(host.Services.GetServices<IAnalyzerRule>());

            var command = host.Services.GetRequiredService<ScanCommand>();

            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            await command.ExecuteAsync(new[] { temp });

            Console.SetOut(originalOut);
            var output = sw.ToString();

            Assert.Contains("# Lintelligent Report", output);
            Assert.Contains("One.cs", output);
            Assert.Contains("TEST001", output);
            Assert.Contains("Always reports a diagnostic", output);
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    [Fact]
    public async Task MultipleFiles_ExecutionWritesAllEntries()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(temp, "A.cs"), "class A { }\n");
            await File.WriteAllTextAsync(Path.Combine(temp, "B.cs"), "class B { }\n");

            using var host = CreateHost();

            var manager = host.Services.GetRequiredService<AnalyzerManager>();
            manager.RegisterRules(host.Services.GetServices<IAnalyzerRule>());

            var command = host.Services.GetRequiredService<ScanCommand>();

            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            await command.ExecuteAsync(new[] { temp });

            Console.SetOut(originalOut);
            var output = sw.ToString();

            Assert.Equal(2, output.Split(new[] { "- **File:**" }, StringSplitOptions.None).Length - 1);
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    [Fact]
    public async Task NoCsFiles_ExecutionWritesOnlyHeader()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            using var host = CreateHost();

            var command = host.Services.GetRequiredService<ScanCommand>();

            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            await command.ExecuteAsync(new[] { temp });

            Console.SetOut(originalOut);
            var output = sw.ToString();

            Assert.Contains("# Lintelligent Report", output);
            Assert.DoesNotContain("- **File:**", output);
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }
}
