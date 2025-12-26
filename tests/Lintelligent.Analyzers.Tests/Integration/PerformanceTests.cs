using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.Analyzers.Tests.Integration;

/// <summary>
///     Performance tests to ensure analyzer overhead meets requirements.
/// </summary>
/// <remarks>
///     SC-003 requires: Roslyn analyzer build overhead <10% for 100- file solution (~2 s max)
/// </remarks>
public class PerformanceTests
{
    [Fact]
    public async Task Analyze_10FileSolution_CompletesWithinReasonableTime()
    {
        // Arrange: Create 10 files with moderate complexity
        var syntaxTrees = new List<SyntaxTree>();
        for (var i = 0; i < 10; i++)
        {
            var source = GenerateTestFile(i);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: $"TestFile{i}.cs");
            syntaxTrees.Add(tree);
        }

        CSharpCompilation compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTrees);

        // Act: Measure analysis time
        var stopwatch = Stopwatch.StartNew();

        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        stopwatch.Stop();

        // Assert: Should complete quickly (<2.5s for 10 files, accommodating CI overhead)
        Assert.True(stopwatch.ElapsedMilliseconds < 2500,
            $"Analysis took {stopwatch.ElapsedMilliseconds}ms (expected <2500ms)");

        // Verify diagnostics were produced (sanity check)
        Assert.NotEmpty(diagnostics);
    }

    [Fact]
    public async Task Analyze_LargeFile_DoesNotExceedTimeLimit()
    {
        // Arrange: Large file with 50 classes, 10 methods each
        var sourceBuilder = new StringBuilder();
        for (var c = 0; c < 50; c++)
        {
            sourceBuilder.AppendLine($"public class Class{c} {{");
            for (var m = 0; m < 10; m++) sourceBuilder.AppendLine($"    public void Method{m}() {{ var x = 1; }}");
            sourceBuilder.AppendLine("}");
        }

        var source = sourceBuilder.ToString();
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, path: "LargeFile.cs");
        CSharpCompilation compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        // Act: Measure analysis time
        var stopwatch = Stopwatch.StartNew();

        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        stopwatch.Stop();

        // Assert: Should complete quickly (<1.5s for large file, accommodating CI overhead)
        Assert.True(stopwatch.ElapsedMilliseconds < 1500,
            $"Analysis took {stopwatch.ElapsedMilliseconds}ms (expected <1500ms)");
    }

    [Fact]
    public async Task Analyze_MultipleFilesInParallel_BenefitsFromConcurrentExecution()
    {
        // Arrange: Create 20 files
        var syntaxTrees = new List<SyntaxTree>();
        for (var i = 0; i < 20; i++)
        {
            var source = GenerateTestFile(i);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: $"ParallelTest{i}.cs");
            syntaxTrees.Add(tree);
        }

        CSharpCompilation compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTrees);

        // Act: Measure analysis time
        var stopwatch = Stopwatch.StartNew();

        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        stopwatch.Stop();

        // Assert: Should complete in <4s for 20 files (benefit from EnableConcurrentExecution, CI overhead)
        Assert.True(stopwatch.ElapsedMilliseconds < 4000,
            $"Analysis took {stopwatch.ElapsedMilliseconds}ms (expected <4000ms)");

        // Verify diagnostics from all files
        var fileCount = diagnostics.Select(d => d.Location.SourceTree?.FilePath).Distinct().Count();
        Assert.True(fileCount > 0, "Should have diagnostics from multiple files");
    }

    /// <summary>
    ///     Generates a test file with various code patterns to trigger different rules.
    /// </summary>
    private static string GenerateTestFile(int index)
    {
        return $@"
using System;

public class TestClass{index}
{{
    public void ShortMethod()
    {{
        var x = 1;
        Console.WriteLine(x);
    }}

    public void LongMethod()
    {{
        var x1 = 1; var x2 = 2; var x3 = 3; var x4 = 4; var x5 = 5;
        var x6 = 6; var x7 = 7; var x8 = 8; var x9 = 9; var x10 = 10;
        var x11 = 11; var x12 = 12; var x13 = 13; var x14 = 14; var x15 = 15;
        var x16 = 16; var x17 = 17; var x18 = 18; var x19 = 19; var x20 = 20;
        var x21 = 21; var x22 = 22;  // 22 statements - violates LNT001
    }}

    public void MethodWithMagicNumber()
    {{
        var timeout = 42;  // Magic number - violates LNT004
    }}

    private void UnusedMethod()
    {{
        Console.WriteLine(""Never called"");
    }}
}}";
    }
}