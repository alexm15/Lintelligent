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

            var host = CreateHost(configure: services =>
            {
                services.AddSingleton<IAnalyzerRule, AlwaysReportRule>();
            });

            var manager = host.Services.GetRequiredService<AnalyzerManager>();
            manager.RegisterRules(host.Services.GetServices<IAnalyzerRule>());

            var command = host.Services.GetRequiredService<ScanCommand>();

            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            try
            {
                await command.ExecuteAsync(new[] { temp });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

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

            var host = CreateHost(configure: services =>
            {
                services.AddSingleton<IAnalyzerRule, AlwaysReportRule>();
            });

            var manager = host.Services.GetRequiredService<AnalyzerManager>();
            manager.RegisterRules(host.Services.GetServices<IAnalyzerRule>());

            var command = host.Services.GetRequiredService<ScanCommand>();

            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            try
            {
                await command.ExecuteAsync(new[] { temp });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

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
            var host = CreateHost();

            var manager = host.Services.GetRequiredService<AnalyzerManager>();
            manager.RegisterRules(host.Services.GetServices<IAnalyzerRule>());

            var command = host.Services.GetRequiredService<ScanCommand>();

            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            try
            {
                await command.ExecuteAsync(new[] { temp });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = sw.ToString();

            Assert.Contains("# Lintelligent Report", output);
            Assert.DoesNotContain("- **File:**", output);
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    [Fact]
    public async Task RealProjectDirectory_ScansAllCsFiles()
    {
        // Arrange
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            // Create a small project structure
            var subDir = Path.Combine(temp, "src");
            Directory.CreateDirectory(subDir);
            
            await File.WriteAllTextAsync(Path.Combine(temp, "Program.cs"), "class Program { static void Main() { } }");
            await File.WriteAllTextAsync(Path.Combine(subDir, "Helper.cs"), "class Helper { }");
            await File.WriteAllTextAsync(Path.Combine(temp, "readme.txt"), "Not a CS file");

            var host = CreateHost(configure: services =>
            {
                services.AddSingleton<IAnalyzerRule, AlwaysReportRule>();
            });

            var manager = host.Services.GetRequiredService<AnalyzerManager>();
            manager.RegisterRules(host.Services.GetServices<IAnalyzerRule>());

            var command = host.Services.GetRequiredService<ScanCommand>();

            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            try
            {
                // Act
                await command.ExecuteAsync(new[] { temp });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = sw.ToString();

            // Assert - should have analyzed 2 .cs files
            Assert.Contains("Program.cs", output);
            Assert.Contains("Helper.cs", output);
            Assert.DoesNotContain("readme.txt", output);
            Assert.Equal(2, output.Split(new[] { "TEST001" }, StringSplitOptions.None).Length - 1);
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    [Fact]
    public async Task BackwardCompatibility_ExistingBehaviorPreserved()
    {
        // Arrange
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(temp, "Test.cs"), "class Test { }");

            var host = CreateHost(configure: services =>
            {
                services.AddSingleton<IAnalyzerRule, AlwaysReportRule>();
            });

            var manager = host.Services.GetRequiredService<AnalyzerManager>();
            manager.RegisterRules(host.Services.GetServices<IAnalyzerRule>());

            var command = host.Services.GetRequiredService<ScanCommand>();

            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            try
            {
                // Act - should work exactly as before the refactor
                await command.ExecuteAsync(new[] { temp });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = sw.ToString();

            // Assert - output format unchanged
            Assert.Contains("# Lintelligent Report", output);
            Assert.Contains("Test.cs", output);
            Assert.Contains("TEST001", output);
            Assert.Contains("Always reports a diagnostic", output);
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    [Fact]
    public async Task LargeCodebase_NoMemoryExhaustion()
    {
        // Arrange
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            // Create 100 files (simulating a large codebase, scaled down for test speed)
            // In production, this pattern handles 1000+ files via streaming
            for (int i = 0; i < 100; i++)
            {
                var content = $"class Class{i} {{ }}";
                await File.WriteAllTextAsync(Path.Combine(temp, $"File{i}.cs"), content);
            }

            var host = CreateHost();

            var manager = host.Services.GetRequiredService<AnalyzerManager>();
            manager.RegisterRules(host.Services.GetServices<IAnalyzerRule>());

            var command = host.Services.GetRequiredService<ScanCommand>();

            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

            try
            {
                // Act
                await command.ExecuteAsync(new[] { temp });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
            var output = sw.ToString();

            // Assert
            Assert.Contains("# Lintelligent Report", output);
            
            // Memory growth should be reasonable (less than 50MB for 100 files)
            var memoryGrowth = memoryAfter - memoryBefore;
            Assert.True(memoryGrowth < 50_000_000, 
                $"Memory growth too high: {memoryGrowth:N0} bytes. Expected streaming to keep memory low.");
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }
}
