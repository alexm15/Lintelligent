using Lintelligent.AnalyzerEngine.ProjectModel;

namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
/// Workspace metadata provided to workspace analyzers.
/// Syntax trees are passed separately to the Analyze method.
/// </summary>
/// <remarks>
/// Design Rationale:
/// - Trees passed as separate parameter for clarity and memory efficiency
/// - Context provides only metadata (solution structure, project relationships)
/// - Immutable after construction to ensure thread-safety and determinism
/// </remarks>
public class WorkspaceContext
{
    /// <summary>
    /// Parsed solution with all projects and configuration.
    /// Provides access to solution-level metadata, project references, and configurations.
    /// </summary>
    /// <remarks>
    /// From Feature 009: Solution provides GetDependencyGraph() for project relationships
    /// </remarks>
    public Solution Solution { get; }
    
    /// <summary>
    /// Dictionary mapping absolute file paths to their containing projects.
    /// Enables analyzers to determine which project a file belongs to.
    /// </summary>
    /// <remarks>
    /// Key: Absolute file path (normalized)
    /// Value: Project containing that file
    /// Usage: Identify cross-project duplications, respect project boundaries
    /// </remarks>
    public IReadOnlyDictionary<string, Project> ProjectsByPath { get; }

    /// <summary>
    /// Constructs a workspace context with solution and project mapping.
    /// </summary>
    public WorkspaceContext(Solution solution, IReadOnlyDictionary<string, Project> projectsByPath)
    {
        Solution = solution ?? throw new ArgumentNullException(nameof(solution));
        ProjectsByPath = projectsByPath ?? throw new ArgumentNullException(nameof(projectsByPath));
    }
}
