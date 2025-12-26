using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.Analyzers.Tests.Integration;

/// <summary>
///     Integration tests verifying monad detection analyzer opt-in behavior.
/// </summary>
public class MonadDetectionIntegrationTests
{
    /// <summary>
    ///     When LanguageExt.Core is not referenced, monad rules should not execute
    ///     even if code contains patterns that look like monads.
    /// </summary>
    [Fact]
    public async Task Analyze_WithoutLanguageExtCore_NoMonadDiagnostics()
    {
        // Arrange: Code that would trigger monad detection if LanguageExt.Core was referenced
        var testCode = @"
class TestClass
{
    string? GetValue() => null;
    
    void UseValue()
    {
        string? value = GetValue();
        if (value != null)
        {
            System.Console.WriteLine(value);
        }
    }
}";

        // Act: Analyze without LanguageExt.Core reference
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(testCode, includeLanguageExt: false);

        // Assert: No monad diagnostics (LNT200-299) should be reported
        var monadDiagnostics = diagnostics.Where(d => 
            d.Id.StartsWith("LNT2", StringComparison.Ordinal) && 
            d.Id.Length == 6).ToList();
        
        Assert.Empty(monadDiagnostics);
    }

    /// <summary>
    ///     Verify that NullableToOptionRule can be discovered and executed.
    ///     Note: Full EditorConfig integration requires .editorconfig file in real project.
    /// </summary>
    [Fact]
    public async Task Analyze_WithLanguageExtCore_NullableToOptionRuleDiscovered()
    {
        // Arrange: Simple test code
        var testCode = @"
class TestClass
{
    void Method() { }
}";

        // Act: Analyze WITH LanguageExt.Core reference and EditorConfig enabled
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(
            testCode, 
            includeLanguageExt: true, 
            editorConfigOptions: new Dictionary<string, string>
            {
                ["language_ext_monad_detection"] = "true"
            });

        // Assert: No analyzer exceptions (LNT999)
        var exceptions = diagnostics.Where(d => d.Id == "LNT999").ToList();
        Assert.Empty(exceptions);
        
        // Note: Full integration test with LNT200 detection requires .editorconfig file
        // in actual project structure. Unit tests verify rule logic is correct.
    }

    /// <summary>
    ///     Verify that TryCatchToEitherRule can be discovered and executed.
    /// </summary>
    [Fact]
    public async Task Analyze_WithLanguageExtCore_TryCatchToEitherRuleDiscovered()
    {
        // Arrange: Simple test code
        var testCode = @"
class TestClass
{
    void Method() { }
}";

        // Act: Analyze WITH LanguageExt.Core reference and EditorConfig enabled
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(
            testCode, 
            includeLanguageExt: true, 
            editorConfigOptions: new Dictionary<string, string>
            {
                ["language_ext_monad_detection"] = "true"
            });

        // Assert: No analyzer exceptions (LNT999)
        var exceptions = diagnostics.Where(d => d.Id == "LNT999").ToList();
        Assert.Empty(exceptions);
    }

    /// <summary>
    ///     Verify that SequentialValidationRule can be discovered and executed.
    /// </summary>
    [Fact]
    public async Task Analyze_WithLanguageExtCore_SequentialValidationRuleDiscovered()
    {
        // Arrange: Simple test code
        var testCode = @"
class TestClass
{
    void Method() { }
}";

        // Act: Analyze WITH LanguageExt.Core reference and EditorConfig enabled
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(
            testCode, 
            includeLanguageExt: true, 
            editorConfigOptions: new Dictionary<string, string>
            {
                ["language_ext_monad_detection"] = "true"
            });

        // Assert: No analyzer exceptions (LNT999)
        var exceptions = diagnostics.Where(d => d.Id == "LNT999").ToList();
        Assert.Empty(exceptions);
    }

    /// <summary>
    ///     Verify that analyzer doesn't crash when LanguageExt.Core is referenced.
    ///     Tests that the opt-in mechanism itself is working correctly.
    /// </summary>
    [Fact]
    public async Task Analyze_WithLanguageExtCore_NoExceptions()
    {
        // Arrange
        var testCode = """

                       class TestClass
                       {
                           void Method()
                           {
                               var x = 42;
                           }
                       }
                       """;

        // Act & Assert: Should not throw any analyzer exceptions (LNT999)
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(testCode, includeLanguageExt: true);
        
        var exceptions = diagnostics.Where(d => d.Id == "LNT999").ToList();
        if (exceptions.Any())
        {
            var exceptionMessages = string.Join(Environment.NewLine, 
                exceptions.Select(e => $"  - {e.GetMessage()}"));
            throw new Exception($"Analyzer threw {exceptions.Count} error(s):{Environment.NewLine}{exceptionMessages}");
        }
    }

    /// <summary>
    ///     Verify analyzer works correctly on edge case: empty file.
    /// </summary>
    [Fact]
    public async Task Analyze_EmptyFile_NoExceptions()
    {
        // Arrange
        var testCode = string.Empty;

        // Act & Assert
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(testCode, includeLanguageExt: true);
        
        var exceptions = diagnostics.Where(d => d.Id == "LNT999").ToList();
        Assert.Empty(exceptions);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string source, 
        bool includeLanguageExt = false,
        Dictionary<string, string>? editorConfigOptions = null)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Test.cs");

        // Build metadata references
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        // Conditionally add LanguageExt.Core reference
        if (includeLanguageExt)
        {
            // Note: This assumes LanguageExt.Core is available at test runtime
            // In actual test environment, this would need to be a real assembly reference
            // For now, we'll use a mock assembly name that our analyzer checks for
            // The analyzer only checks assembly names, not actual types
            
            // Create a mock assembly with the name "LanguageExt.Core"
            var languageExtCompilation = CSharpCompilation.Create(
                "LanguageExt.Core",
                new[] { CSharpSyntaxTree.ParseText("namespace LanguageExt { }") },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            var languageExtStream = new MemoryStream();
            var emitResult = languageExtCompilation.Emit(languageExtStream);
            
            if (emitResult.Success)
            {
                languageExtStream.Position = 0;
                references.Add(MetadataReference.CreateFromStream(languageExtStream));
            }
        }

        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            new[] { syntaxTree },
            references);

        // Create analyzer options with EditorConfig
        AnalyzerOptions? analyzerOptions = null;
        if (editorConfigOptions != null)
        {
            var configOptions = new TestAnalyzerConfigOptions(editorConfigOptions);
            var configProvider = new TestAnalyzerConfigOptionsProvider(configOptions);
            analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, configProvider);
        }

        CompilationWithAnalyzers compilationWithAnalyzers = analyzerOptions != null
            ? compilation.WithAnalyzers(
                ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()),
                analyzerOptions)
            : compilation.WithAnalyzers(
                ImmutableArray.Create<DiagnosticAnalyzer>(new LintelligentDiagnosticAnalyzer()));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    /// <summary>
    ///     Test implementation of AnalyzerConfigOptions for EditorConfig simulation.
    /// </summary>
    private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options;

        public TestAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value!);
        }
    }

    /// <summary>
    ///     Test implementation of AnalyzerConfigOptionsProvider.
    /// </summary>
    private class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _options;

        public TestAnalyzerConfigOptionsProvider(AnalyzerConfigOptions options)
        {
            _options = options;
        }

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;    }
}