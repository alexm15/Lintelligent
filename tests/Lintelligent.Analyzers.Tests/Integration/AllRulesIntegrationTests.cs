using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Lintelligent.Analyzers;

namespace Lintelligent.Analyzers.Tests.Integration;

/// <summary>
/// Integration tests verifying all 8 Lintelligent rules execute correctly via Roslyn analyzer.
/// </summary>
public class AllRulesIntegrationTests
{
    [Fact]
    public async Task Analyze_MethodWith26Statements_ProducesLNT001Diagnostic()
    {
        var testCode = @"
class TestClass
{
    void LongMethod()
    {
        var a = string.Empty; var b = string.Empty; var c = string.Empty; var d = string.Empty;
        var e = string.Empty; var f = string.Empty; var g = string.Empty; var h = string.Empty;
        var i = string.Empty; var j = string.Empty; var k = string.Empty; var l = string.Empty;
        var m = string.Empty; var n = string.Empty; var o = string.Empty; var p = string.Empty;
        var q = string.Empty; var r = string.Empty; var s = string.Empty; var t = string.Empty;
        var u = string.Empty; var v = string.Empty; var w = string.Empty; var x = string.Empty;
        var y = string.Empty; var z = string.Empty;
    }
}";
        var diagnostics = await GetDiagnosticsAsync(testCode);
        AssertSingleDiagnostic(diagnostics, "LNT001", DiagnosticSeverity.Warning, "Method 'LongMethod' has 26 statements");
    }

    [Fact]
    public async Task Analyze_MethodWith6Parameters_ProducesLNT002Diagnostic()
    {
        var testCode = @"
class TestClass
{
    void MethodWithManyParams(int a, int b, int c, int d, int e, int f)
    {
    }
}";
        var diagnostics = await GetDiagnosticsAsync(testCode);
        AssertSingleDiagnostic(diagnostics, "LNT002", DiagnosticSeverity.Warning, "Method 'MethodWithManyParams' has 6 parameters");
    }

    [Fact]
    public async Task Analyze_NestedConditionalDepth4_ProducesLNT003Diagnostic()
    {
        var testCode = @"
class TestClass
{
    void DeeplyNestedMethod(bool a, bool b, bool c, bool d)
    {
        if (a)
        {
            if (b)
            {
                if (c)
                {
                    if (d)
                    {
                        System.Console.WriteLine(string.Empty);
                    }
                }
            }
        }
    }
}";
        var diagnostics = await GetDiagnosticsAsync(testCode);
        AssertSingleDiagnostic(diagnostics, "LNT003", DiagnosticSeverity.Warning, "Conditional nesting depth is 4");
    }

    [Fact]
    public async Task Analyze_MagicNumber_ProducesLNT004Diagnostic()
    {
        var testCode = @"
class TestClass
{
    void MethodWithMagicNumber()
    {
        var x = 42;
    }
}";
        var diagnostics = await GetDiagnosticsAsync(testCode);
        AssertSingleDiagnostic(diagnostics, "LNT004", DiagnosticSeverity.Info, "Magic number '42'");
    }

    [Fact]
    public async Task Analyze_GodClass_ProducesLNT005Diagnostic()
    {
        var testCode = @"
class GodClass
{
    void M1() {} void M2() {} void M3() {} void M4() {} void M5() {}
    void M6() {} void M7() {} void M8() {} void M9() {} void M10() {}
    void M11() {} void M12() {} void M13() {} void M14() {} void M15() {}
    void M16() {} void M17() {} void M18() {} void M19() {} void M20() {}
    void M21() {}
}";
        var diagnostics = await GetDiagnosticsAsync(testCode);
        AssertSingleDiagnostic(diagnostics, "LNT005", DiagnosticSeverity.Warning, "Class 'GodClass' has 21 methods");
    }

    [Fact]
    public async Task Analyze_UnusedPrivateMethod_ProducesLNT006Diagnostic()
    {
        var testCode = @"
class TestClass
{
    private void UnusedMethod()
    {
    }
}";
        var diagnostics = await GetDiagnosticsAsync(testCode);
        AssertSingleDiagnostic(diagnostics, "LNT006", DiagnosticSeverity.Info, "Private method 'UnusedMethod' is never used");
    }

    [Fact]
    public async Task Analyze_EmptyCatchBlock_ProducesLNT007Diagnostic()
    {
        var testCode = @"
class TestClass
{
    void MethodWithEmptyCatch()
    {
        try
        {
            System.Console.WriteLine(string.Empty);
        }
        catch
        {
        }
    }
}";
        var diagnostics = await GetDiagnosticsAsync(testCode);
        AssertSingleDiagnostic(diagnostics, "LNT007", DiagnosticSeverity.Warning, "Empty catch block");
    }

    [Fact]
    public async Task Analyze_MissingXmlDoc_ProducesLNT008Diagnostic()
    {
        var testCode = @"
/// <summary>Documented class</summary>
public class TestClass
{
    public void PublicMethodWithoutDocs()
    {
    }
}";
        var diagnostics = await GetDiagnosticsAsync(testCode);
        AssertSingleDiagnostic(diagnostics, "LNT008", DiagnosticSeverity.Info, "Public method 'PublicMethodWithoutDocs' is missing XML documentation");
    }

    private static void AssertSingleDiagnostic(ImmutableArray<Diagnostic> diagnostics, string expectedId, DiagnosticSeverity expectedSeverity, string expectedMessageFragment)
    {
        var exceptions = diagnostics.Where(d => d.Id == "LNT999").ToList();
        if (exceptions.Any())
        {
            var exceptionMessages = string.Join(Environment.NewLine, exceptions.Select(e => $"  - {e.GetMessage()}"));
            throw new Exception($"Analyzer threw {exceptions.Count} error(s):{Environment.NewLine}{exceptionMessages}");
        }

        Assert.Single(diagnostics);
        var diagnostic = diagnostics[0];
        Assert.Equal(expectedId, diagnostic.Id);
        Assert.Equal(expectedSeverity, diagnostic.Severity);
        Assert.Contains(expectedMessageFragment, diagnostic.GetMessage());
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Test.cs");  // Provide file path for testing
        
        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            });

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
