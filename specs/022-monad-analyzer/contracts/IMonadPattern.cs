namespace Lintelligent.AnalyzerEngine.Rules.Monad;

/// <summary>
/// Represents a detected monad usage pattern with educational information.
/// Used by monad detection rules to provide structured diagnostic data.
/// </summary>
public interface IMonadPattern
{
    /// <summary>
    /// The type of monad suggested for this pattern.
    /// </summary>
    MonadType Type { get; }

    /// <summary>
    /// Human-readable name of the detected pattern (e.g., "nullable return type", "try/catch control flow").
    /// </summary>
    string PatternName { get; }

    /// <summary>
    /// Code snippet showing current implementation (before refactoring).
    /// Should be valid C# code formatted for educational display.
    /// </summary>
    string CurrentCode { get; }

    /// <summary>
    /// Code snippet showing suggested monad-based implementation (after refactoring).
    /// Should be valid C# code formatted for educational display.
    /// </summary>
    string SuggestedCode { get; }

    /// <summary>
    /// Educational explanation of why this monad is beneficial for this pattern.
    /// Plain text, 2-4 sentences.
    /// </summary>
    string Explanation { get; }

    /// <summary>
    /// Complexity score for this pattern (e.g., number of null checks, validation steps).
    /// Used to filter low-complexity cases below threshold.
    /// </summary>
    int ComplexityScore { get; }

    /// <summary>
    /// URL to documentation or additional resources for this monad type.
    /// </summary>
    string DocumentationUrl { get; }
}

/// <summary>
/// Types of monads detected by the analyzer.
/// </summary>
public enum MonadType
{
    /// <summary>
    /// Option&lt;T&gt; - Represents optional values (alternative to null).
    /// </summary>
    Option,

    /// <summary>
    /// Either&lt;L, R&gt; - Represents success (Right) or failure (Left).
    /// </summary>
    Either,

    /// <summary>
    /// Validation&lt;T&gt; - Accumulates multiple validation errors.
    /// </summary>
    Validation,

    /// <summary>
    /// Try&lt;T&gt; - Represents computations that may throw exceptions.
    /// </summary>
    Try
}
