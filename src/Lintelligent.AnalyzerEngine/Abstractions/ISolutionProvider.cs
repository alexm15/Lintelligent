using Lintelligent.AnalyzerEngine.ProjectModel;

namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
///     Provides solution file parsing capabilities.
/// </summary>
public interface ISolutionProvider
{
    /// <summary>
    ///     Parses a Visual Studio solution file to extract project paths and configurations.
    /// </summary>
    /// <param name="solutionPath">Absolute path to the .sln file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Solution entity with discovered projects (not yet evaluated).</returns>
    /// <exception cref="FileNotFoundException">Solution file not found.</exception>
    /// <exception cref="InvalidOperationException">Solution file is malformed.</exception>
    public Task<Solution> ParseSolutionAsync(string solutionPath, CancellationToken cancellationToken = default);
}
