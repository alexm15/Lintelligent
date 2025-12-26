using Lintelligent.AnalyzerEngine.ProjectModel;

namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
///     Provides project evaluation capabilities using MSBuild.
/// </summary>
public interface IProjectProvider
{
    /// <summary>
    ///     Evaluates a single .NET project file to extract compilation settings.
    /// </summary>
    /// <param name="projectPath">Absolute path to the project file (.csproj, .vbproj, .fsproj).</param>
    /// <param name="configuration">Build configuration (e.g., Debug, Release). Defaults to "Debug".</param>
    /// <param name="targetFramework">Target framework moniker to evaluate. If null, uses first available target.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Evaluated project with compilation settings.</returns>
    /// <exception cref="FileNotFoundException">Project file not found.</exception>
    /// <exception cref="InvalidOperationException">Project evaluation failed or target framework not found.</exception>
    public Task<Project> EvaluateProjectAsync(
        string projectPath,
        string configuration = "Debug",
        string? targetFramework = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Evaluates all projects in a solution in parallel.
    /// </summary>
    /// <param name="solution">Solution with project paths to evaluate.</param>
    /// <param name="configuration">Build configuration to use for all projects.</param>
    /// <param name="targetFramework">
    ///     Target framework moniker to use for multi-targeted projects. If null, uses first
    ///     available target.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Solution with all projects evaluated (failed projects logged but not included).</returns>
    public Task<Solution> EvaluateAllProjectsAsync(
        Solution solution,
        string configuration = "Debug",
        string? targetFramework = null,
        CancellationToken cancellationToken = default);
}
