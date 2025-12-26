using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Configuration;
using Lintelligent.AnalyzerEngine.ProjectModel;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.Cli.Infrastructure;
using Lintelligent.Cli.Providers;
using Lintelligent.Reporting;
using Microsoft.Extensions.Logging;

#pragma warning disable MA0006 // Use string.Equals instead of == operator
#pragma warning disable MA0026 // TODO comment detected
#pragma warning disable S1135 // TODO comment detected
#pragma warning disable MA0015 // ArgumentException parameter name
#pragma warning disable MA0004 // ConfigureAwait(false) not needed in CLI

namespace Lintelligent.Cli.Commands;

/// <summary>
///     CLI command that scans a project directory and analyzes C# source files.
/// </summary>
/// <remarks>
///     This command orchestrates the analysis workflow:
///     1. Create FileSystemCodeProvider to discover .cs files
///     2. Get syntax trees from provider
///     3. Pass trees to AnalyzerEngine for analysis
///     4. Filter results by severity (if --severity specified)
///     5. Generate and display report
///     The command is responsible for file system access (via FileSystemCodeProvider).
///     The AnalyzerEngine core performs no IO operations, maintaining constitutional compliance.
/// </remarks>
public sealed class ScanCommand(
    AnalyzerEngine.Analysis.AnalyzerEngine engine,
    AnalyzerEngine.Analysis.WorkspaceAnalyzerEngine workspaceEngine,
    DuplicationOptions duplicationOptions,
    ISolutionProvider? solutionProvider = null,
    IProjectProvider? projectProvider = null,
    ILogger<ScanCommand>? logger = null) : IAsyncCommand
{
    /// <inheritdoc/>
#pragma warning disable MA0051 // Method is too long - CLI argument parsing requires multiple options
    public async Task<CommandResult> ExecuteAsync(string[] args)
#pragma warning restore MA0051
    {
        try
        {
            var path = args.Length > 1 ? args[1] : ".";
            var severityFilter = ParseSeverityFilter(args);
            var groupBy = ParseGroupByOption(args);
            var configuration = ParseConfigurationOption(args);
            var targetFramework = ParseTargetFrameworkOption(args);
            var minDuplicationLines = ParseMinDuplicationLinesOption(args);
            var minDuplicationTokens = ParseMinDuplicationTokensOption(args);
            var enableStructuralSimilarity = ParseEnableStructuralSimilarityOption(args);
            var minSimilarity = ParseMinSimilarityOption(args);
            _ = ParseFormatOption(args); // TODO: Use format option when implementing output formatting
            _ = ParseOutputOption(args); // TODO: Use output path when implementing file output

            // Apply CLI flag overrides to DuplicationOptions (CLI takes precedence)
            if (minDuplicationLines.HasValue)
                duplicationOptions.MinLines = minDuplicationLines.Value;
            if (minDuplicationTokens.HasValue)
                duplicationOptions.MinTokens = minDuplicationTokens.Value;
            if (enableStructuralSimilarity)
                duplicationOptions.EnableStructuralSimilarity = true;
            if (minSimilarity.HasValue)
                duplicationOptions.MinSimilarityPercent = minSimilarity.Value;

            // Check if path is a solution file
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                if (solutionProvider == null)
                    throw new InvalidOperationException("Solution support requires ISolutionProvider to be registered.");

                return await AnalyzeSolutionAsync(path, severityFilter, groupBy, configuration, targetFramework);
            }

            // Original directory-based analysis
            var codeProvider = new FileSystemCodeProvider(path);
            var syntaxTrees = codeProvider.GetSyntaxTrees();

            // Analyze syntax trees (no IO in engine)
            var results = engine.Analyze(syntaxTrees);

            // Filter by severity if specified
            if (severityFilter.HasValue) results = results.Where(r => r.Severity == severityFilter.Value);

            // Materialize results before passing to report generator
            var materializedResults = results.ToList();

            // Generate report with optional grouping
            var report = groupBy switch
            {
                "category" => ReportGenerator.GenerateMarkdownGroupedByCategory(materializedResults),
                _ => ReportGenerator.GenerateMarkdown(materializedResults)
            };

            // Return success with report in Output
            return await Task.FromResult(CommandResult.Success(report));
        }
        catch (ArgumentException ex)
        {
            // ArgumentException and derived types → exit code 2
            return CommandResult.Failure(2, ex.Message);
        }
        catch (Exception ex)
        {
            // All other exceptions → exit code 1
            return CommandResult.Failure(1, ex.Message);
        }
    }

    private async Task<CommandResult> AnalyzeSolutionAsync(
        string solutionPath,
        Severity? severityFilter,
        string? groupBy,
        string configuration,
        string? targetFramework)
    {
        logger?.LogInformation("Analyzing solution: {SolutionPath} with configuration: {Configuration}", solutionPath, configuration);

        // Parse solution to get project list
        var solution = await solutionProvider!.ParseSolutionAsync(solutionPath);
        logger?.LogInformation("Found {ProjectCount} projects in solution", solution.Projects.Count);

        // Check if we have project provider for metadata extraction
        if (projectProvider != null)
        {
            return await AnalyzeSolutionWithProviderAsync(solution, configuration, targetFramework, severityFilter, groupBy);
        }

        // Fallback to directory-based analysis if IProjectProvider not available
        return AnalyzeSolutionDirectories(solution, severityFilter, groupBy);
    }

    private async Task<CommandResult> AnalyzeSolutionWithProviderAsync(
        Solution solution,
        string configuration,
        string? targetFramework,
        Severity? severityFilter,
        string? groupBy)
    {
        var analysisStart = DateTime.UtcNow;
        
        // Evaluate all projects to get metadata (ConditionalSymbols, TargetFrameworks, etc.)
        var evaluatedSolution = await projectProvider!.EvaluateAllProjectsAsync(
            solution,
            configuration,
            targetFramework
        );
        
        logger?.LogInformation("Successfully evaluated {Count} projects", evaluatedSolution.Projects.Count);
        
        // T116: Log dependency graph information
        var depGraph = evaluatedSolution.GetDependencyGraph();
        var totalReferences = depGraph.Values.Sum(refs => refs.Count);
        logger?.LogInformation("Dependency graph: {ProjectCount} projects, {ReferenceCount} project references",
            depGraph.Count, totalReferences);

        // Analyze each evaluated project with its conditional symbols
        var allResults = AnalyzeEvaluatedProjects(evaluatedSolution);

        // Filter by severity if specified
        var filteredResults = severityFilter.HasValue
            ? allResults.Where(r => r.Severity == severityFilter.Value).ToList()
            : allResults;

        // Generate report
        var report = groupBy switch
        {
            "category" => ReportGenerator.GenerateMarkdownGroupedByCategory(filteredResults),
            _ => ReportGenerator.GenerateMarkdown(filteredResults)
        };

        var totalDuration = DateTime.UtcNow - analysisStart;
        logger?.LogInformation("Total analysis completed in {Duration}ms: {DiagnosticCount} diagnostics",
            totalDuration.TotalMilliseconds, filteredResults.Count);

        return CommandResult.Success(report);
    }

    private CommandResult AnalyzeSolutionDirectories(Solution solution, Severity? severityFilter, string? groupBy)
    {
        var allResultsFallback = AnalyzeProjectDirectories(solution);

        // Filter by severity if specified
        var filteredResultsFallback = severityFilter.HasValue
            ? allResultsFallback.Where(r => r.Severity == severityFilter.Value).ToList()
            : allResultsFallback;

        // Generate report
        var reportFallback = groupBy switch
        {
            "category" => ReportGenerator.GenerateMarkdownGroupedByCategory(filteredResultsFallback),
            _ => ReportGenerator.GenerateMarkdown(filteredResultsFallback)
        };

        return CommandResult.Success(reportFallback);
    }

    private List<DiagnosticResult> AnalyzeEvaluatedProjects(Solution solution)
    {
        var allResults = new List<DiagnosticResult>();
        var allTrees = new List<Microsoft.CodeAnalysis.SyntaxTree>();
        
        // Pass 1: Single-file analysis (existing rules)
        AnalyzeSingleFileRules(solution, allResults, allTrees);

        logger?.LogInformation("Single-file analysis complete: {Count} diagnostics found", allResults.Count);

        // Pass 2: Workspace-level analysis (duplication detection, etc.)
        AnalyzeWorkspaceRules(solution, allTrees, allResults);

        return allResults;
    }

    private void AnalyzeSingleFileRules(
        Solution solution,
        List<DiagnosticResult> allResults,
        List<Microsoft.CodeAnalysis.SyntaxTree> allTrees)
    {
        foreach (var project in solution.Projects)
        {
            try
            {
                // Use CompileItems from the project if available, otherwise fallback to directory scan
                if (project.CompileItems.Count > 0)
                {
                    logger?.LogInformation(
                        "Analyzing project: {ProjectName} with {SymbolCount} conditional symbols",
                        project.Name,
                        project.ConditionalSymbols.Count);

                    // Get syntax trees for the project's source files with conditional symbols
                    var projectDir = Path.GetDirectoryName(project.FilePath);
                    if (projectDir != null)
                    {
                        var codeProvider = new FileSystemCodeProvider(projectDir);
                        var syntaxTrees = codeProvider.GetSyntaxTrees(project.ConditionalSymbols).ToList();
                        
                        // Single-file analysis
                        var results = engine.Analyze(syntaxTrees);
                        allResults.AddRange(results);
                        
                        // Collect trees for workspace analysis
                        allTrees.AddRange(syntaxTrees);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to analyze project {ProjectName}", project.Name);
                // Continue with other projects
            }
        }
    }

    private void AnalyzeWorkspaceRules(
        Solution solution,
        List<Microsoft.CodeAnalysis.SyntaxTree> allTrees,
        List<DiagnosticResult> allResults)
    {
        if (allTrees.Count > 0 && workspaceEngine.Analyzers.Count > 0)
        {
            logger?.LogInformation("Running workspace analyzers across {TreeCount} syntax trees", allTrees.Count);
            
            var context = new WorkspaceContext(
                solution,
                solution.Projects.ToDictionary(
                    p => p.FilePath,
                    p => p,
                    StringComparer.OrdinalIgnoreCase));
            
            var workspaceResults = workspaceEngine.Analyze(allTrees, context);
            allResults.AddRange(workspaceResults);
            
            logger?.LogInformation("Workspace analysis complete: {TotalCount} total diagnostics", allResults.Count);
        }
    }

    private List<DiagnosticResult> AnalyzeProjectDirectories(Solution solution)
    {
        var allResults = new List<DiagnosticResult>();
        
        foreach (var project in solution.Projects)
        {
            try
            {
                var projectDir = Path.GetDirectoryName(project.FilePath);
                if (projectDir == null || !Directory.Exists(projectDir))
                {
                    logger?.LogWarning("Skipping project {ProjectName}: directory not found", project.Name);
                    continue;
                }

                logger?.LogInformation("Analyzing project: {ProjectName}", project.Name);
                var codeProvider = new FileSystemCodeProvider(projectDir);
                var syntaxTrees = codeProvider.GetSyntaxTrees();
                var results = engine.Analyze(syntaxTrees);
                allResults.AddRange(results);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to analyze project {ProjectName}", project.Name);
                // Continue with other projects
            }
        }

        logger?.LogInformation("Total diagnostics found: {Count}", allResults.Count);
        return allResults;
    }

    private static Severity? ParseSeverityFilter(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--severity")
                return args[i + 1].ToLowerInvariant() switch
                {
                    "error" => Severity.Error,
                    "warning" => Severity.Warning,
                    "info" => Severity.Info,
                    _ => null
                };

        return null;
    }

    private static string? ParseGroupByOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--group-by")
            {
                var value = args[i + 1].ToLowerInvariant();
                return value == "category" ? value : null;
            }

        return null;
    }
    
    private static string ParseFormatOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--format")
            {
                var value = args[i + 1].ToLowerInvariant();
                var validFormats = new[] { "json", "sarif", "markdown" };
                if (!validFormats.Contains(value))
                {
                    throw new ArgumentException(
                        $"Invalid format '{value}'. Valid formats: {string.Join(", ", validFormats)}");
                }
                return value;
            }

        return "markdown"; // Default format
    }
    
    private static string? ParseOutputOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--output")
                return args[i + 1];

        return null; // Default: stdout
    }
    
    /// <summary>
    ///     Parses the --configuration flag from command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>
    ///     Configuration name (e.g., Debug, Release, or custom configurations).
    ///     Defaults to "Debug" if not specified.
    /// </returns>
    /// <remarks>
    ///     The configuration value determines which preprocessor symbols are defined:
    ///     - Debug: typically defines DEBUG and TRACE symbols
    ///     - Release: typically defines RELEASE symbol
    ///     Custom configurations may define different symbols based on project settings.
    ///     
    ///     Example usage:
    ///     - lintelligent scan MySolution.sln --configuration Debug
    ///     - lintelligent scan MyProject.csproj --configuration Release
    ///     - lintelligent scan MySolution.sln --configuration Staging (custom config)
    /// </remarks>
    private static string ParseConfigurationOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--configuration" || args[i] == "-c")
                return args[i + 1];

        return "Debug"; // Default configuration
    }

    /// <summary>
    ///     Parses the --target-framework flag from command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Target framework moniker (e.g., net8.0) or null if not specified.</returns>
    /// <remarks>
    ///     The --target-framework flag specifies which target framework to analyze for multi-targeted projects.
    ///     If not specified, the first target framework in the project will be used.
    ///     Example usage:
    ///     - lintelligent scan MultiTargetProject.csproj --target-framework net8.0
    ///     - lintelligent scan MySolution.sln --target-framework net472
    ///     - lintelligent scan MyProject.csproj -f net8.0 (short form)
    /// </remarks>
    private static string? ParseTargetFrameworkOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--target-framework" || args[i] == "-f")
                return args[i + 1];

        return null; // Default: use first available target framework
    }

    /// <summary>
    ///     Parses the --min-duplication-lines flag from command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Minimum line count for duplication detection or null if not specified.</returns>
    /// <remarks>
    ///     The --min-duplication-lines flag specifies the minimum number of lines required
    ///     for code to be reported as a duplication. Duplications with fewer lines will be
    ///     filtered out unless they meet the token threshold.
    ///     Default: 10 lines (if not specified)
    ///     Example usage:
    ///     - lintelligent scan MySolution.sln --min-duplication-lines 15
    ///     - lintelligent scan MyProject --min-duplication-lines 5
    /// </remarks>
    private static int? ParseMinDuplicationLinesOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--min-duplication-lines")
                return int.TryParse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null;

        return null; // Default: use DuplicationOptions default (10)
    }

    /// <summary>
    ///     Parses the --min-duplication-tokens flag from command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Minimum token count for duplication detection or null if not specified.</returns>
    /// <remarks>
    ///     The --min-duplication-tokens flag specifies the minimum number of tokens required
    ///     for code to be reported as a duplication. This allows detection of token-dense
    ///     duplications even if they have few lines.
    ///     Default: 50 tokens (if not specified)
    ///     Example usage:
    ///     - lintelligent scan MySolution.sln --min-duplication-tokens 100
    ///     - lintelligent scan MyProject --min-duplication-tokens 30
    /// </remarks>
    private static int? ParseMinDuplicationTokensOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--min-duplication-tokens")
                return int.TryParse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null;

        return null; // Default: use DuplicationOptions default (50)
    }

    /// <summary>
    ///     Parses the --enable-structural-similarity flag from command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>True if structural similarity detection is enabled, false otherwise.</returns>
    /// <remarks>
    ///     The --enable-structural-similarity flag enables detection of structurally similar code
    ///     with different identifiers and literals (e.g., same logic with different variable names).
    ///     This is more computationally expensive than exact duplication detection.
    ///     Default: false (disabled)
    ///     Example usage:
    ///     - lintelligent scan MySolution.sln --enable-structural-similarity
    ///     - lintelligent scan MyProject --enable-structural-similarity --min-similarity 90
    /// </remarks>
    private static bool ParseEnableStructuralSimilarityOption(string[] args)
    {
        return args.Contains("--enable-structural-similarity", StringComparer.Ordinal);
    }

    /// <summary>
    ///     Parses the --min-similarity flag from command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Minimum similarity percentage (0-100) or null if not specified.</returns>
    /// <remarks>
    ///     The --min-similarity flag specifies the minimum similarity percentage (0-100)
    ///     for structural similarity matches. Only applies when --enable-structural-similarity is used.
    ///     Default: 85.0 (85%)
    ///     Example usage:
    ///     - lintelligent scan MySolution.sln --enable-structural-similarity --min-similarity 90
    ///     - lintelligent scan MyProject --enable-structural-similarity --min-similarity 80
    /// </remarks>
    private static double? ParseMinSimilarityOption(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--min-similarity")
                return double.TryParse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null;

        return null; // Default: use DuplicationOptions default (85.0)
    }
}

