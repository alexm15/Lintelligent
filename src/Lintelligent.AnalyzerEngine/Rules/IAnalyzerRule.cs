using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.AnalyzerEngine.Rules;

/// <summary>
///     Contract for code analysis rules.
///     Rules must be stateless, deterministic, and free of external dependencies.
/// </summary>
/// <remarks>
///     Constitutional Requirements (Principle III):
///     - Stateless: No mutable state, same instance can analyze multiple files
///     - Deterministic: Same input produces same output, always
///     - No I/O: No file system, network, or database access
///     - No Logging: No dependency on logging abstractions
///     - No Rule Dependencies: Cannot depend on other rules
///     Performance Expectations:
///     - Analysis should complete in O(n) time relative to syntax tree size
///     - Memory usage should be bounded (no unbounded caching)
///     - Lazy evaluation preferred (use yield return for multiple findings)
/// </remarks>
public interface IAnalyzerRule
{
    /// <summary>
    ///     Unique identifier for the rule.
    ///     Must be non-null, non-empty, and unique within the analyzer.
    /// </summary>
    /// <example>"LongMethod", "CA1001", "SEC001"</example>
    public string Id { get; }

    /// <summary>
    ///     Human-readable description of what the rule checks.
    ///     Should clearly explain the issue detected and why it matters.
    /// </summary>
    /// <example>"Methods should not exceed 50 lines to maintain readability"</example>
    public string Description { get; }

    /// <summary>
    ///     Severity level of findings produced by this rule.
    ///     Must be a defined value from the Severity enum.
    /// </summary>
    /// <remarks>
    ///     Severity Guide:
    ///     - Error: Critical issues (bugs, security, correctness)
    ///     - Warning: Should fix (maintainability, performance)
    ///     - Info: Nice to have (style, suggestions)
    /// </remarks>
    public Severity Severity { get; }

    /// <summary>
    ///     Category for grouping related rules.
    ///     Must be non-null and non-empty.
    ///     Use constants from DiagnosticCategories when applicable.
    /// </summary>
    /// <example>"Maintainability", "Performance", "Security"</example>
    public string Category { get; }

    /// <summary>
    ///     Analyzes a syntax tree and returns zero or more diagnostic findings.
    ///     Must be deterministic and not throw exceptions under normal operation.
    /// </summary>
    /// <param name="tree">The syntax tree to analyze. Never null.</param>
    /// <returns>
    ///     Enumerable of diagnostic results. Return Enumerable.Empty&lt;DiagnosticResult&gt;()
    ///     if no findings. Never return null.
    /// </returns>
    /// <remarks>
    ///     Implementation Guidelines:
    ///     - Use yield return for multiple findings (enables lazy evaluation)
    ///     - Return Enumerable.Empty() for no findings (not null)
    ///     - Catch expected exceptions (e.g., malformed syntax), return empty enumerable
    ///     - Let unexpected exceptions propagate (AnalyzerEngine will handle)
    ///     - Ensure metadata (Severity, Category) is passed to DiagnosticResult constructor
    ///     Breaking Change from v1.x:
    ///     - Old signature: DiagnosticResult? Analyze(SyntaxTree tree)
    ///     - New signature: IEnumerable&lt;DiagnosticResult&gt; Analyze(SyntaxTree tree)
    ///     - Migration: Change return type, use yield return or Enumerable.Empty()
    /// </remarks>
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
