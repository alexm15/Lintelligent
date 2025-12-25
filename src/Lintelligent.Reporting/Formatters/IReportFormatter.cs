using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.Reporting.Formatters;

/// <summary>
/// Defines the contract for report formatters that transform diagnostic results into output formats.
/// </summary>
/// <remarks>
/// Constitutional Compliance:
/// - Principle VI (Extensibility): Allows third-party formatters without modifying existing code
/// - Principle VII (Testability): Pure transformation, testable with mock DiagnosticResult data
/// - Principle III (Determinism): Same input → same output (no side effects)
/// </remarks>
public interface IReportFormatter
{
    /// <summary>
    /// Formats a collection of diagnostic results into a string representation.
    /// </summary>
    /// <param name="results">Diagnostic results to format (may be empty collection).</param>
    /// <returns>Formatted output string (JSON, SARIF, Markdown, etc.).</returns>
    /// <remarks>
    /// Implementation Requirements:
    /// - MUST be stateless and thread-safe
    /// - MUST handle empty collections gracefully (FR-014)
    /// - MUST NOT perform I/O operations (pure transformation)
    /// - SHOULD complete in &lt;10 seconds for 10,000 results (SC-008)
    /// </remarks>
    string Format(IEnumerable<DiagnosticResult> results);
    
    /// <summary>
    /// Gets the format name (e.g., "json", "sarif", "markdown") for CLI selection.
    /// </summary>
    string FormatName { get; }
}
