namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
///     Severity level of a diagnostic finding.
///     Defines the importance and impact of issues detected by analysis rules.
/// </summary>
public enum Severity : byte
{
    /// <summary>
    ///     Critical issues that block release.
    ///     Examples: null reference errors, security vulnerabilities, correctness bugs.
    /// </summary>
    Error = 0,

    /// <summary>
    ///     Issues that should be fixed but are non-blocking.
    ///     Examples: code smells, maintainability issues, minor performance concerns.
    /// </summary>
    Warning = 1,

    /// <summary>
    ///     Informational suggestions for improvement.
    ///     Examples: style recommendations, best practice hints, optional optimizations.
    /// </summary>
    Info = 2
}
