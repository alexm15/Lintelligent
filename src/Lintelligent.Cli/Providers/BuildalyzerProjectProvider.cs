using Buildalyzer;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.ProjectModel;
using Microsoft.Extensions.Logging;

namespace Lintelligent.Cli.Providers;

/// <summary>
/// Implements project evaluation using Buildalyzer to extract compilation settings from MSBuild projects.
/// </summary>
public sealed class BuildalyzerProjectProvider(ILogger<BuildalyzerProjectProvider> logger) : IProjectProvider
{
    /// <inheritdoc/>
    public async Task<Project> EvaluateProjectAsync(
        string projectPath,
        string configuration = "Debug",
        string? targetFramework = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evaluating project: {ProjectPath} (Configuration: {Configuration})", projectPath, configuration);

        if (!File.Exists(projectPath))
            throw new FileNotFoundException($"Project file not found: {projectPath}", projectPath);

        return await Task.Run(() =>
        {
            var manager = new AnalyzerManager();
            var analyzer = manager.GetProject(projectPath);
            
            var environmentOptions = new Buildalyzer.Environment.EnvironmentOptions
            {
                DesignTime = false,
                Restore = false
            };
            
            // Add Configuration to global properties
            environmentOptions.GlobalProperties.Add("Configuration", configuration);
            
            var results = analyzer.Build(environmentOptions);

            if (results.Count == 0)
                throw new InvalidOperationException($"Project evaluation failed for {projectPath}. No build results returned.");

            // Convert results to list (IAnalyzerResults is a collection of IAnalyzerResult)
            var resultsList = results.ToList();
            
            if (resultsList.Count == 0)
                throw new InvalidOperationException($"Project evaluation failed for {projectPath}. All build results were null.");

            // Select target framework: specified, or first available
            var selectedResult = SelectTargetFrameworkResult(resultsList, targetFramework, projectPath);

            logger.LogInformation("Selected target framework: {TargetFramework}", selectedResult.TargetFramework);

            return CreateProjectFromResult(selectedResult, resultsList, projectPath, configuration);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Solution> EvaluateAllProjectsAsync(
        Solution solution,
        string configuration = "Debug",
        string? targetFramework = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        logger.LogInformation("Evaluating {ProjectCount} projects from solution: {SolutionName}", 
            solution.Projects.Count, solution.Name);

        var evaluatedProjects = new List<Project>();

        // Evaluate projects in parallel with error handling
        var tasks = solution.Projects.Select(async project =>
        {
            try
            {
                var evaluated = await EvaluateProjectAsync(project.FilePath, configuration, targetFramework, cancellationToken).ConfigureAwait(false);
                return (Success: true, Project: evaluated);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to evaluate project: {ProjectPath}", project.FilePath);
                return (Success: false, Project: (Project?)null);
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (var (success, project) in results)
        {
            if (success && project != null)
                evaluatedProjects.Add(project);
        }

        var duration = DateTime.UtcNow - startTime;
        logger.LogInformation("Successfully evaluated {EvaluatedCount} of {TotalCount} projects in {Duration}ms",
            evaluatedProjects.Count, solution.Projects.Count, duration.TotalMilliseconds);

        // T118: Handle all projects failing evaluation
        if (evaluatedProjects.Count == 0 && solution.Projects.Count > 0)
        {
            logger.LogError("All {ProjectCount} projects failed evaluation", solution.Projects.Count);
            throw new InvalidOperationException(
                $"All {solution.Projects.Count} projects in solution '{solution.Name}' failed evaluation. " +
                "Check build logs for details. Ensure .NET SDK is installed and projects can be restored.");
        }

        // Return new solution with evaluated projects
        return new Solution(solution.FilePath, solution.Name, evaluatedProjects, solution.Configurations);
    }

    private static Buildalyzer.IAnalyzerResult SelectTargetFrameworkResult(
        IReadOnlyList<Buildalyzer.IAnalyzerResult> results,
        string? targetFramework,
        string projectPath)
    {
        if (targetFramework == null)
        {
            // Use first available target framework
            return results[0];
        }

        // Find matching target framework
        var match = results.FirstOrDefault(r =>
            r.TargetFramework?.Equals(targetFramework, StringComparison.OrdinalIgnoreCase) == true);

        if (match == null)
        {
            var availableFrameworks = string.Join(", ", results.Select(r => r.TargetFramework));
            throw new InvalidOperationException(
                $"Target framework '{targetFramework}' not found in project {projectPath}. " +
                $"Available frameworks: {availableFrameworks}");
        }

        return match;
    }

    private Project CreateProjectFromResult(
        Buildalyzer.IAnalyzerResult result,
        IReadOnlyList<Buildalyzer.IAnalyzerResult> allResults,
        string projectPath,
        string configuration)
    {
        var targetFramework = ExtractTargetFramework(result);
        var allTargetFrameworks = allResults.Select(r => ExtractTargetFramework(r)).Distinct().ToList();
        var conditionalSymbols = ExtractConditionalSymbols(result);
        var compileItems = ExtractSourceFiles(result, projectPath);
        var projectReferences = ExtractProjectReferences(result, projectPath);
        var platform = result.Properties.TryGetValue("Platform", out var platformValue) ? platformValue : "AnyCPU";
        var outputType = result.Properties.TryGetValue("OutputType", out var outputTypeValue) ? outputTypeValue : "Library";

        logger.LogInformation("Project {ProjectName}: {SourceFileCount} source files, {SymbolCount} symbols, {ReferenceCount} project references",
            Path.GetFileNameWithoutExtension(projectPath), compileItems.Count, conditionalSymbols.Count, projectReferences.Count);

        return new Project(
            filePath: projectPath,
            name: Path.GetFileNameWithoutExtension(projectPath),
            targetFramework: targetFramework,
            allTargetFrameworks: allTargetFrameworks,
            conditionalSymbols: conditionalSymbols,
            configuration: configuration,
            platform: platform,
            outputType: outputType,
            compileItems: compileItems,
            projectReferences: projectReferences
        );
    }

    private static TargetFramework ExtractTargetFramework(Buildalyzer.IAnalyzerResult result)
    {
        // Extract target framework from Properties dictionary (TargetFramework or TargetFrameworkMoniker)
        string targetFrameworkMoniker;
        if (result.Properties.TryGetValue("TargetFramework", out var tfm) && !string.IsNullOrWhiteSpace(tfm))
        {
            targetFrameworkMoniker = tfm;
        }
        else if (result.Properties.TryGetValue("TargetFrameworkMoniker", out var tfMoniker) && !string.IsNullOrWhiteSpace(tfMoniker))
        {
            targetFrameworkMoniker = tfMoniker;
        }
        else
        {
            targetFrameworkMoniker = "net8.0"; // Fallback
        }

        return new TargetFramework(targetFrameworkMoniker);
    }

    private static List<string> ExtractConditionalSymbols(Buildalyzer.IAnalyzerResult result)
    {
        // Try PreprocessorSymbols first, fall back to parsing DefineConstants
        if (result.PreprocessorSymbols is { Length: > 0 })
        {
            return result.PreprocessorSymbols.ToList();
        }

        if (result.Properties.TryGetValue("DefineConstants", out var defineConstants) && !string.IsNullOrWhiteSpace(defineConstants))
        {
            // Parse DefineConstants: symbols are semicolon-separated
            return defineConstants
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        return new List<string>();
    }

    private static List<CompileItem> ExtractSourceFiles(Buildalyzer.IAnalyzerResult result, string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath) ?? string.Empty;
        
        // Try SourceFiles first (full build), then fall back to Compile items (design-time)
        if (result.SourceFiles is { Length: > 0 })
        {
            return result.SourceFiles
                .Select(filePath => CreateCompileItem(filePath, projectPath))
                .ToList();
        }

        // Fall back to Items collection
        if (result.Items.TryGetValue("Compile", out var compileItems) && compileItems.Length > 0)
        {
            return compileItems
                .Select(item => item.ItemSpec)
                .Where(spec => !string.IsNullOrWhiteSpace(spec))
                .Select(spec =>
                {
                    // Convert relative paths to absolute
                    var absolutePath = Path.IsPathRooted(spec)
                        ? spec
                        : Path.GetFullPath(Path.Combine(projectDir, spec));
                    return CreateCompileItem(absolutePath, projectPath);
                })
                .ToList();
        }

        return new List<CompileItem>();
    }

    private static CompileItem CreateCompileItem(string filePath, string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath) ?? string.Empty;
        var isInProjectDirectory = filePath.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase);

        // Determine inclusion type
        if (isInProjectDirectory)
        {
            // DefaultGlob: file in project directory, no originalIncludePath
            return new CompileItem(filePath, CompileItemInclusionType.DefaultGlob, null);
        }
        else
        {
            // LinkedFile: file outside project directory, originalIncludePath is the file path
            return new CompileItem(filePath, CompileItemInclusionType.LinkedFile, filePath);
        }
    }

    private static List<ProjectReference> ExtractProjectReferences(Buildalyzer.IAnalyzerResult result, string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath) ?? string.Empty;
        var references = new List<ProjectReference>();

        // Extract from ProjectReferences collection in the result
        if (result.ProjectReferences != null && result.ProjectReferences.Any())
        {
            foreach (var refPath in result.ProjectReferences)
            {
                if (string.IsNullOrWhiteSpace(refPath))
                    continue;

                // Convert relative paths to absolute
                var absolutePath = Path.IsPathRooted(refPath)
                    ? refPath
                    : Path.GetFullPath(Path.Combine(projectDir, refPath));

                var referenceName = Path.GetFileNameWithoutExtension(absolutePath);
                references.Add(new ProjectReference(absolutePath, referenceName));
            }
        }

        return references;
    }
}
