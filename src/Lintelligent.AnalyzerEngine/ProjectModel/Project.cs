namespace Lintelligent.AnalyzerEngine.ProjectModel;

/// <summary>
/// Represents an evaluated .NET project with compilation settings.
/// </summary>
public sealed class Project
{
    /// <summary>
    /// Absolute path to the project file (.csproj, .vbproj, .fsproj).
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Project name (file name without extension).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Target framework for this evaluation (e.g., net8.0, net472).
    /// For multi-targeted projects, this is the selected framework.
    /// </summary>
    public TargetFramework TargetFramework { get; }

    /// <summary>
    /// All target frameworks this project can build for.
    /// For single-targeted projects, contains one element.
    /// </summary>
    public IReadOnlyList<TargetFramework> AllTargetFrameworks { get; }

    /// <summary>
    /// Conditional compilation symbols (e.g., DEBUG, TRACE, CUSTOM_FEATURE).
    /// Extracted from DefineConstants for the current configuration.
    /// </summary>
    public IReadOnlyList<string> ConditionalSymbols { get; }

    /// <summary>
    /// Build configuration (e.g., Debug, Release, Custom).
    /// </summary>
    public string Configuration { get; }

    /// <summary>
    /// Platform (e.g., AnyCPU, x64, ARM).
    /// </summary>
    public string Platform { get; }

    /// <summary>
    /// Project output type (Exe, Library, WinExe).
    /// </summary>
    public string OutputType { get; }

    /// <summary>
    /// Source files to be analyzed (after Include/Remove evaluation).
    /// </summary>
    public IReadOnlyList<CompileItem> CompileItems { get; }

    /// <summary>
    /// Other projects referenced by this project.
    /// </summary>
    public IReadOnlyList<ProjectReference> ProjectReferences { get; }

    /// <summary>
    /// Indicates if this project is multi-targeted.
    /// </summary>
    public bool IsMultiTargeted => AllTargetFrameworks.Count > 1;

    public Project(
        string filePath,
        string name,
        TargetFramework targetFramework,
        IReadOnlyList<TargetFramework> allTargetFrameworks,
        IReadOnlyList<string> conditionalSymbols,
        string configuration,
        string platform,
        string outputType,
        IReadOnlyList<CompileItem> compileItems,
        IReadOnlyList<ProjectReference> projectReferences)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!IsAbsolutePath(filePath))
            throw new ArgumentException("File path must be absolute.", nameof(filePath));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be null or empty.", nameof(name));

        if (targetFramework == null)
            throw new ArgumentNullException(nameof(targetFramework));
        if (allTargetFrameworks == null)
            throw new ArgumentNullException(nameof(allTargetFrameworks));
        if (conditionalSymbols == null)
            throw new ArgumentNullException(nameof(conditionalSymbols));
        if (compileItems == null)
            throw new ArgumentNullException(nameof(compileItems));
        if (projectReferences == null)
            throw new ArgumentNullException(nameof(projectReferences));

        if (allTargetFrameworks.Count == 0)
            throw new ArgumentException("Project must have at least one target framework.", nameof(allTargetFrameworks));

        if (!allTargetFrameworks.Contains(targetFramework))
            throw new ArgumentException("Selected target framework must be in AllTargetFrameworks list.", nameof(targetFramework));

        if (string.IsNullOrWhiteSpace(configuration))
            throw new ArgumentException("Configuration cannot be null or empty.", nameof(configuration));

        if (string.IsNullOrWhiteSpace(platform))
            throw new ArgumentException("Platform cannot be null or empty.", nameof(platform));

        if (string.IsNullOrWhiteSpace(outputType))
            throw new ArgumentException("Output type cannot be null or empty.", nameof(outputType));

        FilePath = filePath;
        Name = name;
        TargetFramework = targetFramework;
        AllTargetFrameworks = allTargetFrameworks;
        ConditionalSymbols = conditionalSymbols;
        Configuration = configuration;
        Platform = platform;
        OutputType = outputType;
        CompileItems = compileItems;
        ProjectReferences = projectReferences;
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

    public override string ToString() => $"{Name} ({TargetFramework})";
}
