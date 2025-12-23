using Lintelligent.AnalyzerEngine.Abstractions;

namespace Lintelligent.AnalyzerEngine.Results;

/// <summary>
/// Represents a single diagnostic finding from an analyzer rule.
/// Immutable value object containing location and metadata.
/// </summary>
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
public record DiagnosticResult
{
    public string FilePath { get; init; }
    public string RuleId { get; init; }
    public string Message { get; init; }
    public int LineNumber { get; init; }
    public Severity Severity { get; init; }
    public string Category { get; init; }

    public DiagnosticResult(
        string filePath,
        string ruleId,
        string message,
        int lineNumber,
        Severity severity,
        string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId, nameof(ruleId));
        ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));
        ArgumentException.ThrowIfNullOrWhiteSpace(category, nameof(category));

        if (lineNumber < 1)
            throw new ArgumentOutOfRangeException(
                nameof(lineNumber),
                lineNumber,
                "Line number must be >= 1 (1-based indexing)");

        if (!Enum.IsDefined(typeof(Severity), severity))
            throw new ArgumentException(
                $"Undefined severity value: {severity}",
                nameof(severity));

        FilePath = filePath;
        RuleId = ruleId;
        Message = message;
        LineNumber = lineNumber;
        Severity = severity;
        Category = category;
    }
}

