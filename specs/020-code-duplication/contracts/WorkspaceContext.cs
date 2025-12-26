using Lintelligent.AnalyzerEngine.ProjectModel;

namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
/// Workspace metadata provided to workspace analyzers.
/// Syntax trees are passed separately to the Analyze method.
/// </summary>
public class WorkspaceContext
{
    /// <summary>
    /// Parsed solution with all projects and configuration.
    /// </summary>
    public required Solution Solution { get; init; }
    
    /// <summary>
    /// Dictionary mapping absolute file paths to their containing projects.
    /// Enables analyzers to determine which project a file belongs to.
    /// </summary>
    public required IReadOnlyDictionary<string, Project> ProjectsByPath { get; init; }
}
