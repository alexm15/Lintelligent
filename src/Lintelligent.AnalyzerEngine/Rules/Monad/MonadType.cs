namespace Lintelligent.AnalyzerEngine.Rules.Monad;

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
