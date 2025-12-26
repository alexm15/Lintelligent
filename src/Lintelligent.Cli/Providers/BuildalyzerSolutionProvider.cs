using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.ProjectModel;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

namespace Lintelligent.Cli.Providers;

/// <summary>
///     Parses Visual Studio solution files using Microsoft.Build.Construction.
/// </summary>
public sealed class BuildalyzerSolutionProvider : ISolutionProvider
{
    private readonly ILogger<BuildalyzerSolutionProvider> _logger;

    public BuildalyzerSolutionProvider(ILogger<BuildalyzerSolutionProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Solution> ParseSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            throw new ArgumentException("Solution path cannot be null or empty.", nameof(solutionPath));

        if (!File.Exists(solutionPath))
            throw new FileNotFoundException($"Solution file not found: {solutionPath}", solutionPath);

        try
        {
            _logger.LogInformation("Parsing solution: {SolutionPath}", solutionPath);

            var solutionFile = SolutionFile.Parse(solutionPath);
            List<Project> projects = ExtractProjects(solutionFile, solutionPath);
            List<string> configurations = ExtractConfigurations(solutionFile);

            var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
            _logger.LogInformation(
                "Solution parsed successfully: {SolutionName} - {ProjectCount} projects, {ConfigurationCount} configurations",
                solutionName,
                projects.Count,
                configurations.Count);

            // T117: Handle empty solution
            if (projects.Count == 0) _logger.LogWarning("Solution {SolutionName} contains no projects", solutionName);

            var solution = new Solution(
                Path.GetFullPath(solutionPath),
                solutionName,
                projects,
                configurations
            );

            return Task.FromResult(solution);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogError(ex, "Failed to parse solution file: {SolutionPath}", solutionPath);
            throw new InvalidOperationException($"Solution file is malformed: {solutionPath}", ex);
        }
    }

    private static List<Project> ExtractProjects(SolutionFile solutionFile, string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;

        return solutionFile.ProjectsInOrder
            .Where(p => p.ProjectType != SolutionProjectType.SolutionFolder)
            .Select(p => CreatePlaceholderProject(p, solutionDirectory))
            .ToList();
    }

    private static Project CreatePlaceholderProject(ProjectInSolution projectInSolution, string solutionDirectory)
    {
        var absoluteProjectPath = Path.IsPathRooted(projectInSolution.AbsolutePath)
            ? projectInSolution.AbsolutePath
            : Path.GetFullPath(Path.Combine(solutionDirectory, projectInSolution.RelativePath));

        var targetFramework = new TargetFramework("net8.0");

        return new Project(
            absoluteProjectPath,
            projectInSolution.ProjectName,
            targetFramework,
            new List<TargetFramework> {targetFramework}, // Include at least one
            new List<string>(),
            "Debug",
            "AnyCPU",
            "Library",
            new List<CompileItem>(),
            new List<ProjectReference>()
        );
    }

    private static List<string> ExtractConfigurations(SolutionFile solutionFile)
    {
        return solutionFile.SolutionConfigurations
            .Select(c => c.FullName)
            .ToList();
    }
}
