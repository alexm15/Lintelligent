using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Lintelligent.Analyzers.Tests.Integration;

/// <summary>
/// Integration tests for edge cases like generated code, partial classes, and error handling.
/// </summary>
public class EdgeCaseTests
{
    [Fact]
    public async Task Analyze_GeneratedCodeFile_SkipsAnalysis()
    {
        // Arrange: Code with 30 statements (violates LNT001) but in .g.cs file
        const string source = @"
public class GeneratedClass
{
    public void LongMethod()
    {
        var x1 = 1; var x2 = 2; var x3 = 3; var x4 = 4; var x5 = 5;
        var x6 = 6; var x7 = 7; var x8 = 8; var x9 = 9; var x10 = 10;
        var x11 = 11; var x12 = 12; var x13 = 13; var x14 = 14; var x15 = 15;
        var x16 = 16; var x17 = 17; var x18 = 18; var x19 = 19; var x20 = 20;
        var x21 = 21; var x22 = 22; var x23 = 23; var x24 = 24; var x25 = 25;
        var x26 = 26; var x27 = 27; var x28 = 28; var x29 = 29; var x30 = 30;
    }
}";

        // Act: Parse with .g.cs file extension
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Generated.g.cs");
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Assert: No diagnostics should be reported for generated code
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Analyze_PartialClassAcrossFiles_AnalyzesIndependently()
    {
        // Arrange: Two files with partial class definition
        const string file1 = @"
public partial class MyClass
{
    public void Method1()
    {
        var x1 = 1; var x2 = 2; var x3 = 3; var x4 = 4; var x5 = 5;
        var x6 = 6; var x7 = 7; var x8 = 8; var x9 = 9; var x10 = 10;
        var x11 = 11; var x12 = 12; var x13 = 13; var x14 = 14; var x15 = 15;
        var x16 = 16; var x17 = 17; var x18 = 18; var x19 = 19; var x20 = 20;
        var x21 = 21; var x22 = 22; var x23 = 23; var x24 = 24; var x25 = 25;
        var x26 = 26;  // 26 statements - violates LNT001
    }
}";

        const string file2 = @"
public partial class MyClass
{
    public void Method2()
    {
        var y1 = 1; var y2 = 2; var y3 = 3; var y4 = 4; var y5 = 5;
        var y6 = 6; var y7 = 7; var y8 = 8; var y9 = 9; var y10 = 10;
        var y11 = 11; var y12 = 12; var y13 = 13; var y14 = 14; var y15 = 15;
        var y16 = 16; var y17 = 17; var y18 = 18; var y19 = 19; var y20 = 20;
        var y21 = 21; var y22 = 22; var y23 = 23; var y24 = 24; var y25 = 25;
        var y26 = 26;  // 26 statements - violates LNT001
    }
}";

        // Act: Parse both files
        var tree1 = CSharpSyntaxTree.ParseText(file1, path: "MyClass.Part1.cs");
        var tree2 = CSharpSyntaxTree.ParseText(file2, path: "MyClass.Part2.cs");
        
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree1, tree2);

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Assert: Should have 2 LNT001 diagnostics (one per file)
        var lnt001Diagnostics = diagnostics.Where(d => d.Id == "LNT001").ToArray();
        Assert.Equal(2, lnt001Diagnostics.Length);
        
        // Verify each diagnostic is in the correct file
        Assert.Contains(lnt001Diagnostics, d => d.Location.SourceTree == tree1);
        Assert.Contains(lnt001Diagnostics, d => d.Location.SourceTree == tree2);
    }

    [Fact]
    public async Task Analyze_EmptyFile_DoesNotCrash()
    {
        // Arrange: Empty file
        const string source = "";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Empty.cs");
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Assert: Should not crash, no diagnostics expected
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Analyze_SyntaxError_DoesNotCrash()
    {
        // Arrange: Code with syntax errors
        const string source = @"
public class Broken
{
    public void Invalid(
    // Missing closing parenthesis and brace
}";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Broken.cs");
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Assert: Should not crash, may or may not have analyzer diagnostics
        // (syntax errors are handled by compiler, not analyzer)
        var lnt999 = diagnostics.Where(d => d.Id == "LNT999").ToArray();
        Assert.Empty(lnt999);  // No internal errors
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, string path = "Test.cs")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: path);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
