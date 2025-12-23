using Lintelligent.AnalyzerEngine.Abstractions;

namespace Lintelligent.AnalyzerEngine.Results;

/// <summary>
/// Represents a single diagnostic finding from an analyzer rule.
/// Immutable value object containing location and metadata.
/// </summary>
/// <param name="FilePath">Absolute or relative path to the analyzed file.</param>
/// <param name="RuleId">Identifier of the rule that produced this finding (matches IAnalyzerRule.Id).</param>
/// <param name="Message">Human-readable description of the finding.</param>
/// <param name="LineNumber">Line number where the issue was found (1-based).</param>
/// <param name="Severity">Severity level of the finding (inherited from the rule).</param>
/// <param name="Category">Category of the finding (inherited from the rule).</param>
/// <remarks>
/// Breaking Change from v1.x:
/// - Old constructor: DiagnosticResult(string FilePath, string RuleId, string Message, int LineNumber)
/// - New constructor: Adds Severity and Category parameters (required)
/// - Migration: Pass rule.Severity and rule.Category to constructor
/// 
/// Immutability:
/// - Implemented as C# record (structural equality, immutable after construction)
/// - No setters, all properties initialized via constructor
/// - Thread-safe by design (no mutable state)
/// 
/// Constitutional Compliance:
/// - Principle VII (Determinism): Immutable structure ensures consistent comparison
/// - Principle III (Rule Contract): Enforces metadata flow via required constructor params
/// </remarks>
public record DiagnosticResult(
    string FilePath,
    string RuleId,
    string Message,
    int LineNumber,
    Severity Severity,
    string Category
)
{
    /// <summary>
    /// Validates constructor parameters.
    /// Called automatically by record initialization.
    /// </summary>
    public DiagnosticResult
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FilePath, nameof(FilePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(RuleId, nameof(RuleId));
        ArgumentException.ThrowIfNullOrWhiteSpace(Message, nameof(Message));
        ArgumentException.ThrowIfNullOrWhiteSpace(Category, nameof(Category));

        if (LineNumber < 1)
            throw new ArgumentOutOfRangeException(
                nameof(LineNumber),
                LineNumber,
                "Line number must be >= 1 (1-based indexing)");

        if (!Enum.IsDefined(typeof(Severity), Severity))
            throw new ArgumentException(
                $"Undefined severity value: {Severity}",
                nameof(Severity));
    }
}
