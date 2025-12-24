using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.Analyzers;

/// <summary>
///     Contract for Lintelligent Roslyn diagnostic analyzer.
///     Bridges IAnalyzerRule implementations to Roslyn analyzer infrastructure.
/// </summary>
/// <remarks>
///     Constitutional Compliance:
///     - Principle I (Layered Architecture): Sits at AnalyzerEngine layer, wraps IAnalyzerRule
///     - Principle II (DI Boundaries): No DI (instantiated by Roslyn host)
///     - Principle V (Testing Discipline): Testable via Microsoft.CodeAnalysis.Testing
///     - Principle VII (Determinism): Inherits IAnalyzerRule determinism
///     
///     Roslyn Integration:
///     - Implements DiagnosticAnalyzer (Roslyn analyzer base class)
///     - Decorated with [DiagnosticAnalyzer(LanguageNames.CSharp)]
///     - Discovered automatically by Roslyn when package installed
///     
///     Performance:
///     - Stateless execution (EnableConcurrentExecution)
///     - Static rule discovery (one-time reflection cost)
///     - No caching (rules are deterministic, fast)
/// </remarks>
public interface ILintelligentDiagnosticAnalyzer
{
    /// <summary>
    ///     Get all supported diagnostic descriptors (one per rule).
    ///     Called once by Roslyn during analyzer initialization.
    /// </summary>
    /// <returns>Immutable array of diagnostic descriptors (LNT001-LNT008)</returns>
    ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    /// <summary>
    ///     Initialize analyzer and register analysis callbacks.
    ///     Called once by Roslyn host when analyzer is loaded.
    /// </summary>
    /// <param name="context">Analysis context for registering callbacks</param>
    /// <remarks>
    ///     Implementation must:
    ///     - Call context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None) to skip generated code
    ///     - Call context.EnableConcurrentExecution() for parallel analysis
    ///     - Call context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree) to register analysis callback
    /// </remarks>
    void Initialize(AnalysisContext context);

    /// <summary>
    ///     Analyze a single syntax tree and report diagnostics.
    ///     Called by Roslyn for each file in compilation.
    /// </summary>
    /// <param name="context">Syntax tree analysis context (contains tree, options, diagnostic reporter)</param>
    /// <remarks>
    ///     Implementation must:
    ///     - Iterate all discovered IAnalyzerRule instances
    ///     - Check EditorConfig for severity overrides (dotnet_diagnostic.{RuleId}.severity)
    ///     - Skip rule if severity is "none"
    ///     - Call rule.Analyze(context.Tree)
    ///     - Convert DiagnosticResult to Roslyn Diagnostic
    ///     - Report diagnostic via context.ReportDiagnostic()
    ///     - Handle exceptions gracefully (log error, continue analysis)
    /// </remarks>
    void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context);

    /// <summary>
    ///     Discover all IAnalyzerRule implementations via reflection.
    ///     Called once during static initialization.
    /// </summary>
    /// <returns>Array of instantiated IAnalyzerRule objects</returns>
    /// <remarks>
    ///     Implementation must:
    ///     - Scan Lintelligent.AnalyzerEngine assembly for IAnalyzerRule types
    ///     - Filter to concrete classes (exclude interfaces, abstract classes)
    ///     - Instantiate each rule via parameterless constructor
    ///     - Handle instantiation failures gracefully (log warning, continue)
    ///     - Throw InvalidOperationException if no rules found
    /// </remarks>
    IAnalyzerRule[] DiscoverRules();

    /// <summary>
    ///     Create DiagnosticDescriptor for each discovered rule.
    ///     Called once during static initialization.
    /// </summary>
    /// <param name="rules">Array of discovered IAnalyzerRule instances</param>
    /// <returns>Array of DiagnosticDescriptor (one per rule)</returns>
    /// <remarks>
    ///     Uses RuleDescriptorFactory.Create() to map IAnalyzerRule → DiagnosticDescriptor
    /// </remarks>
    DiagnosticDescriptor[] CreateDescriptors(IAnalyzerRule[] rules);
}
