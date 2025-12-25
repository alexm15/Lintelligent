namespace Lintelligent.Reporting.Formatters.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Root JSON output model conforming to Lintelligent JSON schema.
/// Constitutional Compliance: Principle VII - Testability via pure data model.
/// </summary>
public record JsonOutputModel
{
    /// <summary>
    /// Overall status of the analysis run ("success" or "error").
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }
    
    /// <summary>
    /// Summary statistics of all violations.
    /// </summary>
    [JsonPropertyName("summary")]
    public required SummaryModel Summary { get; init; }
    
    /// <summary>
    /// Array of all violations found during analysis.
    /// </summary>
    [JsonPropertyName("violations")]
    public required List<ViolationModel> Violations { get; init; }
}

/// <summary>
/// Summary statistics for the analysis run.
/// </summary>
public record SummaryModel
{
    /// <summary>
    /// Total number of violations across all severities.
    /// </summary>
    [JsonPropertyName("total")]
    public required int Total { get; init; }
    
    /// <summary>
    /// Violation count grouped by severity level (error/warning/info).
    /// </summary>
    [JsonPropertyName("bySeverity")]
    public required Dictionary<string, int> BySeverity { get; init; }
}

/// <summary>
/// Individual code violation details.
/// </summary>
public record ViolationModel
{
    /// <summary>
    /// File path where the violation occurred.
    /// </summary>
    [JsonPropertyName("filePath")]
    public required string FilePath { get; init; }
    
    /// <summary>
    /// Line number where the violation was found.
    /// </summary>
    [JsonPropertyName("lineNumber")]
    public required int LineNumber { get; init; }
    
    /// <summary>
    /// Rule identifier (e.g., LINT001).
    /// </summary>
    [JsonPropertyName("ruleId")]
    public required string RuleId { get; init; }
    
    /// <summary>
    /// Severity level (error/warning/info).
    /// </summary>
    [JsonPropertyName("severity")]
    public required string Severity { get; init; }
    
    /// <summary>
    /// Violation category (e.g., Complexity, Naming).
    /// </summary>
    [JsonPropertyName("category")]
    public required string Category { get; init; }
    
    /// <summary>
    /// Human-readable description of the violation.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
