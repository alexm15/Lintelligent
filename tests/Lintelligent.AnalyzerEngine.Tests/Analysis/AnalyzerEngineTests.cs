using Lintelligent.AnalyzerEngine.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Analysis;

/// <summary>
/// Unit tests for AnalyzerEngine demonstrating in-memory testing capability.
/// These tests prove Constitution Principle I compliance: zero file system dependencies.
/// </summary>
public class AnalyzerEngineTests
{
    [Fact]
    public void Analyze_WithInMemorySyntaxTree_ReturnsDiagnosticsWithoutFileSystem()
    {
        // Arrange: Create in-memory syntax tree with a long method (>20 lines)
        var sourceCode = """

                         class TestClass
                         {
                             void LongMethod()
                             {
                                 var line1 = 1;
                                 var line2 = 2;
                                 var line3 = 3;
                                 var line4 = 4;
                                 var line5 = 5;
                                 var line6 = 6;
                                 var line7 = 7;
                                 var line8 = 8;
                                 var line9 = 9;
                                 var line10 = 10;
                                 var line11 = 11;
                                 var line12 = 12;
                                 var line13 = 13;
                                 var line14 = 14;
                                 var line15 = 15;
                                 var line16 = 16;
                                 var line17 = 17;
                                 var line18 = 18;
                                 var line19 = 19;
                                 var line20 = 20;
                                 var line21 = 21;
                             }
                         }
                         """;
        var tree = CSharpSyntaxTree.ParseText(sourceCode, path: "InMemoryTest.cs");
        var trees = new[] { tree };

        var rule = new Rules.LongMethodRule();
        var manager = new AnalyzerManager();
        manager.RegisterRule(rule);
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act: Analyze in-memory tree (NO file system IO)
        var results = engine.Analyze(trees).ToList();

        // Assert: Verify diagnostic was produced without touching file system
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.FilePath == "InMemoryTest.cs");
    }

    [Fact]
    public void Analyze_WithEmptyCollection_ReturnsEmptyResults()
    {
        // Arrange: Empty syntax tree collection
        var trees = Array.Empty<SyntaxTree>();
        var manager = new AnalyzerManager();
        var engine = new Lintelligent.AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var results = engine.Analyze(trees).ToList();

        // Assert: No errors, just empty results
        Assert.Empty(results);
    }

    [Fact]
    public void Analyze_WithSameSyntaxTrees_ProducesDeterministicResults()
    {
        // Arrange: Create identical in-memory trees
        var sourceCode = "class Test { void Method() { } }";
        var tree1 = CSharpSyntaxTree.ParseText(sourceCode, path: "Test1.cs");
        var tree2 = CSharpSyntaxTree.ParseText(sourceCode, path: "Test2.cs");

        var rule = new Rules.LongMethodRule();
        var manager = new AnalyzerManager();
        manager.RegisterRule(rule);
        var engine = new Lintelligent.AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act: Analyze same source code multiple times
        var results1 = engine.Analyze([tree1]).ToList();
        var results2 = engine.Analyze([tree2]).ToList();

        // Assert: Same code produces same number of diagnostics (deterministic)
        Assert.Equal(results1.Count, results2.Count);
    }

    [Fact]
    public void Analyze_WithMultipleTrees_YieldsResultsForEach()
    {
        // Arrange: Multiple in-memory trees with violations
        var source1 = GenerateLongMethodSource("Class1", "Method1");
        var source2 = GenerateLongMethodSource("Class2", "Method2");
        
        var tree1 = CSharpSyntaxTree.ParseText(source1, path: "File1.cs");
        var tree2 = CSharpSyntaxTree.ParseText(source2, path: "File2.cs");

        var rule = new Rules.LongMethodRule();
        var manager = new AnalyzerManager();
        manager.RegisterRule(rule);
        var engine = new Lintelligent.AnalyzerEngine.Analysis.AnalyzerEngine(manager);

        // Act
        var results = engine.Analyze([tree1, tree2]).ToList();

        // Assert: Both trees were analyzed
        Assert.True(results.Count >= 2); // At least one diagnostic per tree
        Assert.Contains(results, r => r.FilePath == "File1.cs");
        Assert.Contains(results, r => r.FilePath == "File2.cs");
    }

    private static string GenerateLongMethodSource(string className, string methodName)
    {
        return $$"""

                 class {{className}}
                 {
                     void {{methodName}}()
                     {
                         {{string.Join("\n        ", Enumerable.Range(1, 25).Select(i => $"var line{i} = {i};"))}}
                     }
                 }
                 """;
    }
}
