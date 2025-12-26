using Lintelligent.AnalyzerEngine.Results;

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
/// </remarks>
public interface IWorkspaceAnalyzer
{
    /// <summary>
    /// Unique identifier for the workspace analyzer.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Human-readable description of what the analyzer checks.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Severity level of findings produced by this analyzer.
    /// </summary>
    Severity Severity { get; }
    
    /// <summary>
    /// Category for grouping related analyzers.
    /// </summary>
    string Category { get; }
    
    /// <summary>
    /// Analyzes syntax trees and returns zero or more diagnostic findings.
    /// Must be deterministic and not throw exceptions under normal operation.
    /// </summary>
    /// <param name="trees">All syntax trees in the workspace to analyze.</param>
    /// <param name="context">Workspace metadata (solution, projects) for contextual analysis.</param>
    /// <returns>Enumerable of diagnostic results. Never null.</returns>
    /// <remarks>
    /// The context parameter provides access to solution structure and project metadata
    /// from Feature 009 (Solution/Project parsing). This enables analyzers to:
    /// - Identify cross-project duplications
    /// - Respect project boundaries and dependencies
    /// - Access compilation context (target frameworks, conditional symbols)
    /// </remarks>
    IEnumerable<DiagnosticResult> Analyze(
        IReadOnlyList<SyntaxTree> trees,
        WorkspaceContext context);
}
