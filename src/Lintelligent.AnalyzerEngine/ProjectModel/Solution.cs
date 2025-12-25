namespace Lintelligent.AnalyzerEngine.ProjectModel;

/// <summary>
/// Represents a Visual Studio solution with projects and configurations.
/// </summary>
public sealed class Solution
{
    /// <summary>
    /// Absolute path to the .sln file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Solution display name (file name without extension).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// All projects contained in the solution.
    /// </summary>
    public IReadOnlyList<Project> Projects { get; }

    /// <summary>
    /// Solution-level configurations (e.g., Debug, Release).
    /// </summary>
    public IReadOnlyList<string> Configurations { get; }

    public Solution(
        string filePath,
        string name,
        IReadOnlyList<Project> projects,
        IReadOnlyList<string> configurations)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!IsAbsolutePath(filePath))
            throw new ArgumentException("File path must be absolute.", nameof(filePath));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Solution name cannot be null or empty.", nameof(name));

        if (projects == null)
            throw new ArgumentNullException(nameof(projects));
        if (configurations == null)
            throw new ArgumentNullException(nameof(configurations));

        if (configurations.Count == 0)
            throw new ArgumentException("Solution must have at least one configuration.", nameof(configurations));

        FilePath = filePath;
        Name = name;
        Projects = projects;
        Configurations = configurations;
    }

    /// <summary>
    /// Get dependency graph for all projects.
    /// Returns dictionary mapping project path to list of referenced project paths.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetDependencyGraph()
    {
        var graph = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in Projects)
        {
            var dependencies = project.ProjectReferences
                .Select(r => r.ReferencedProjectPath)
                .ToList();

            graph[project.FilePath] = dependencies;
        }

        return graph;
    }

    private static bool IsAbsolutePath(string path) =>
        Path.IsPathRooted(path) && !Path.GetPathRoot(path)!.Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);

    public override string ToString() => $"{Name} ({Projects.Count} projects)";
}
