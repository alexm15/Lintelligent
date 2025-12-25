namespace Lintelligent.AnalyzerEngine.ProjectModel;

/// <summary>
/// Represents a project-to-project reference.
/// </summary>
public sealed class ProjectReference
{
    /// <summary>
    /// Absolute path to the referenced project file.
    /// </summary>
    public string ReferencedProjectPath { get; }

    /// <summary>
    /// Name of the referenced project (file name without extension).
    /// </summary>
    public string ReferencedProjectName { get; }

    public ProjectReference(string referencedProjectPath, string referencedProjectName)
    {
        if (string.IsNullOrWhiteSpace(referencedProjectPath))
            throw new ArgumentException("Referenced project path cannot be null or empty.", nameof(referencedProjectPath));

        if (!IsAbsolutePath(referencedProjectPath))
            throw new ArgumentException("Referenced project path must be absolute.", nameof(referencedProjectPath));

        if (string.IsNullOrWhiteSpace(referencedProjectName))
            throw new ArgumentException("Referenced project name cannot be null or empty.", nameof(referencedProjectName));

        ReferencedProjectPath = referencedProjectPath;
        ReferencedProjectName = referencedProjectName;
    }

    private static bool IsAbsolutePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (!Path.IsPathRooted(path))
            return false;

        var root = Path.GetPathRoot(path);
        if (root == null)
            return false;

        // Windows: root should be like "C:\\" or "\\\\network\\share", not just "\\"
        // Unix: root is "/", which is valid
        if (Path.DirectorySeparatorChar == '\\')
        {
            // Windows
            return root.Length > 1 || root.Equals("\\\\\\\\", StringComparison.Ordinal);
        }
        else
        {
            // Unix
            return root.Equals("/", StringComparison.Ordinal);
        }
    }

    public override string ToString() => ReferencedProjectName;
}
