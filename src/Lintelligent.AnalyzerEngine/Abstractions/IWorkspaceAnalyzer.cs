using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;

namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
/// Contract for workspace-level code analysis.
/// Analyzers must be stateless, deterministic, and free of external dependencies.
/// </summary>
/// <remarks>
/// Constitutional Requirements (Principles I, III, VII):
/// - Stateless: No mutable state, same instance can analyze multiple workspaces
/// - Deterministic: Same workspace produces same output, always
/// - No I/O: No file system, network, or database access
/// - No Logging: No dependency on logging abstractions
/// Performance Expectations:
/// - Analysis should scale linearly with workspace size
/// - Memory usage should be bounded (consider streaming for large workspaces)
/// - Lazy evaluation preferred (use yield return for multiple findings)
/// </remarks>
public interface IWorkspaceAnalyzer
{
    /// <summary>
    /// Unique identifier for the workspace analyzer.
    /// Must be non-null, non-empty, and unique within the analyzer.
    /// </summary>
    /// <example>"LNT-DUP", "LNT-ARCH", "LNT-DEP"</example>
    public string Id { get; }
    
    /// <summary>
    /// Human-readable description of what the analyzer checks.
    /// Should clearly explain the issue detected and why it matters.
    /// </summary>
    /// <example>"Detects duplicate code blocks across multiple files"</example>
    public string Description { get; }
    
    /// <summary>
    /// Severity level of findings produced by this analyzer.
    /// Must be a defined value from the Severity enum.
    /// </summary>
    /// <remarks>
    /// Severity Guide:
    /// - Error: Critical issues (architecture violations, cross-cutting bugs)
    /// - Warning: Should fix (duplication, dependency issues)
    /// - Info: Nice to have (architectural suggestions)
    /// </remarks>
    public Severity Severity { get; }
    
    /// <summary>
    /// Category for grouping related analyzers.
    /// Must be non-null and non-empty.
    /// </summary>
    /// <example>"Maintainability", "Architecture", "Dependencies"</example>
    public string Category { get; }
    
    /// <summary>
    /// Analyzes syntax trees and returns zero or more diagnostic findings.
    /// Must be deterministic and not throw exceptions under normal operation.
    /// </summary>
    /// <param name="trees">All syntax trees in the workspace to analyze. Never null.</param>
    /// <param name="context">Workspace metadata (solution, projects) for contextual analysis. Never null.</param>
    /// <returns>
    /// Enumerable of diagnostic results. Return Enumerable.Empty&lt;DiagnosticResult&gt;()
    /// if no findings. Never return null.
    /// </returns>
    /// <remarks>
    /// The context parameter provides access to solution structure and project metadata
    /// from Feature 009 (Solution/Project parsing). This enables analyzers to:
    /// - Identify cross-project duplications
    /// - Respect project boundaries and dependencies
    /// - Access compilation context (target frameworks, conditional symbols)
    /// 
    /// Implementation Guidelines:
    /// - Use yield return for multiple findings (enables lazy evaluation)
    /// - Return Enumerable.Empty() for no findings (not null)
    /// - Catch expected exceptions (e.g., malformed syntax), return empty enumerable
    /// - Let unexpected exceptions propagate (WorkspaceAnalyzerEngine will handle)
    /// - Ensure metadata (Severity, Category) is passed to DiagnosticResult constructor
    /// </remarks>
    public IEnumerable<DiagnosticResult> Analyze(
        IReadOnlyList<SyntaxTree> trees,
        WorkspaceContext context);
}
