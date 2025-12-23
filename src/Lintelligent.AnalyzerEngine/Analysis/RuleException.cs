namespace Lintelligent.AnalyzerEngine.Analysis;

/// <summary>
/// Represents an exception that occurred while executing an analyzer rule.
/// </summary>
/// <param name="RuleId">The ID of the rule that threw the exception.</param>
/// <param name="FilePath">The file path being analyzed when the exception occurred.</param>
/// <param name="Exception">The exception that was thrown.</param>
public sealed record RuleException(string RuleId, string FilePath, Exception Exception);
