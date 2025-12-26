namespace Lintelligent.AnalyzerEngine.Results;

/// <summary>
///     Common diagnostic category constants for consistency across rules.
///     Rules may use these constants or define custom categories as needed.
/// </summary>
/// <remarks>
///     Usage Guidelines:
///     - Use these constants for built-in rules to maintain consistency
///     - Third-party rules can define custom categories (e.g., "DomainLogic", "DataAccess")
///     - Category is a string property (not enum) to allow extensibility
///     Examples:
///     - public string Category => DiagnosticCategories.Maintainability;
///     - public string Category => "CustomDomain"; // Custom category OK
/// </remarks>
public static class DiagnosticCategories
{
    /// <summary>
    ///     Code maintainability issues.
    ///     Examples: long methods, high cyclomatic complexity, deep nesting.
    /// </summary>
    public const string Maintainability = "Maintainability";

    /// <summary>
    ///     Performance concerns.
    ///     Examples: inefficient loops, excessive allocations, boxing/unboxing.
    /// </summary>
    public const string Performance = "Performance";

    /// <summary>
    ///     Security vulnerabilities.
    ///     Examples: SQL injection, hardcoded secrets, insecure crypto.
    /// </summary>
    public const string Security = "Security";

    /// <summary>
    ///     Code style and formatting issues.
    ///     Examples: naming conventions, whitespace, file organization.
    /// </summary>
    public const string Style = "Style";

    /// <summary>
    ///     Design pattern violations or architectural concerns.
    ///     Examples: SOLID violations, inappropriate abstractions, circular dependencies.
    /// </summary>
    public const string Design = "Design";

    /// <summary>
    ///     Code smells that indicate potential maintainability issues.
    ///     Examples: long parameter lists, deep nesting, exception swallowing.
    /// </summary>
    public const string CodeSmell = "Code Smell";

    /// <summary>
    ///     Documentation quality issues.
    ///     Examples: missing XML docs, incomplete summaries.
    /// </summary>
    public const string Documentation = "Documentation";

    /// <summary>
    ///     Functional programming opportunities.
    ///     Examples: nullable to Option&lt;T&gt;, try/catch to Either&lt;L, R&gt;, monad patterns.
    /// </summary>
    public const string Functional = "Functional";

    /// <summary>
    ///     General issues not fitting other categories.
    ///     Use sparingly - prefer specific categories when applicable.
    /// </summary>
    public const string General = "General";
}
