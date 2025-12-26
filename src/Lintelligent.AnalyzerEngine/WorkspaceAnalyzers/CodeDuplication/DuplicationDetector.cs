using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Configuration;
using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;

namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
/// Detects code duplication across an entire workspace using token-based analysis.
/// Implements <see cref="IWorkspaceAnalyzer"/> for integration with the analyzer engine.
/// </summary>
/// <remarks>
/// Constitutional Compliance:
/// - Principle I (Stateless): No instance fields beyond configuration
/// - Principle III (Deterministic): Same input → same output, ordered results
/// - Principle VII (Composable): Integrates via IWorkspaceAnalyzer without side effects
/// </remarks>
public sealed class DuplicationDetector : IWorkspaceAnalyzer
{
    private readonly DuplicationOptions _options;

    /// <summary>
    /// Initializes a new instance with default options.
    /// </summary>
    public DuplicationDetector() : this(new DuplicationOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance with specified options.
    /// </summary>
    public DuplicationDetector(DuplicationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Id => "DUP001";

    /// <inheritdoc />
    public string Description => "Detects exact code duplications across files and projects";

    /// <inheritdoc />
    public Severity Severity => Severity.Warning;

    /// <inheritdoc />
    public string Category => "Code Quality";

    /// <inheritdoc />
    public IEnumerable<DiagnosticResult> Analyze(
        IReadOnlyList<SyntaxTree> trees,
        WorkspaceContext context)
    {
        ArgumentNullException.ThrowIfNull(trees);
        ArgumentNullException.ThrowIfNull(context);

        return AnalyzeCore(trees, context);
    }

    private IEnumerable<DiagnosticResult> AnalyzeCore(
        IReadOnlyList<SyntaxTree> trees,
        WorkspaceContext context)
    {
        // Find all duplication groups using token-based exact matching
        var groups = ExactDuplicationFinder.FindDuplicates(trees, _options);

        // Convert each group to a diagnostic result
        foreach (var group in groups)
        {
            yield return ConvertToDiagnostic(group, context);
        }
    }

    /// <summary>
    /// Converts a duplication group to a diagnostic result.
    /// </summary>
    /// <remarks>
    /// Primary location: First instance (alphabetically by file path)
    /// Message: "Code duplicated in {count} files: {project1}/{file1}, {project2}/{file2}, ..."
    /// </remarks>
    private static DiagnosticResult ConvertToDiagnostic(DuplicationGroup group, WorkspaceContext context)
    {
        // Enhance instances with project names from workspace context
        var enhancedInstances = group.Instances
            .Select(instance => EnhanceWithProjectName(instance, context))
            .OrderBy(i => i.ProjectName, StringComparer.Ordinal)
            .ThenBy(i => i.FilePath, StringComparer.Ordinal)
            .ToList();

        // First instance becomes the primary location
        var primaryInstance = enhancedInstances[0];

        // Build message with affected files
        var affectedFiles = string.Join(", ", enhancedInstances.Select(i =>
        {
            var fileName = Path.GetFileName(i.FilePath);
            return string.IsNullOrEmpty(i.ProjectName) ? fileName : $"{i.ProjectName}/{fileName}";
        }));

        var message = $"Code duplicated in {group.Instances.Count} files ({group.LineCount} lines, {group.TokenCount} tokens): {affectedFiles}";

        return new DiagnosticResult(
            filePath: primaryInstance.FilePath,
            ruleId: "DUP001",
            message: message,
            lineNumber: primaryInstance.Location.Start.Line + 1, // Convert to 1-based
            severity: Severity.Warning,
            category: "Code Quality");
    }

    /// <summary>
    /// Enhances a duplication instance with project name from workspace context.
    /// </summary>
    private static DuplicationInstance EnhanceWithProjectName(
        DuplicationInstance instance,
        WorkspaceContext context)
    {
        // Find the project containing this file
        var projectName = string.Empty;

        foreach (var project in context.Solution.Projects)
        {
            // Check if file path is under project directory
            var projectDir = Path.GetDirectoryName(project.FilePath);
            if (projectDir != null && instance.FilePath.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
            {
                projectName = project.Name;
                break;
            }
        }

        return new DuplicationInstance(
            instance.FilePath,
            projectName,
            instance.Location,
            instance.TokenCount,
            instance.Hash,
            instance.SourceText);
    }
}
