# Interface: IProjectProvider

**Namespace**: `Lintelligent.AnalyzerEngine.Abstractions`  
**Assembly**: `Lintelligent.AnalyzerEngine`  
**Purpose**: Framework-agnostic abstraction for evaluating .NET project files with MSBuild

## Interface Definition

```csharp
namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
/// Provides project file evaluation capabilities using MSBuild evaluation APIs.
/// </summary>
public interface IProjectProvider
{
    /// <summary>
    /// Evaluates a .NET project file to extract compilation settings and source files.
    /// </summary>
    /// <param name="projectPath">Absolute path to the project file (.csproj, .vbproj, .fsproj).</param>
    /// <param name="configuration">Build configuration (e.g., Debug, Release). Defaults to "Debug".</param>
    /// <param name="targetFramework">
    /// Target framework to evaluate (e.g., net8.0, net472).
    /// If null, evaluates first target framework.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// Evaluated project with compilation settings, source files, and metadata.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="projectPath"/> is null, empty, or not an absolute path.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when project file does not exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when project evaluation fails (malformed XML, missing SDK, unsupported project type, etc.).
    /// </exception>
    Task<Project> EvaluateProjectAsync(
        string projectPath,
        string configuration = "Debug",
        string? targetFramework = null,
        CancellationToken cancellationToken = default
    );
    
    /// <summary>
    /// Evaluates all projects in a solution.
    /// </summary>
    /// <param name="solution">Parsed solution with project paths.</param>
    /// <param name="configuration">Build configuration (e.g., Debug, Release). Defaults to "Debug".</param>
    /// <param name="targetFramework">
    /// Target framework to evaluate for multi-targeted projects.
    /// If null, evaluates first target framework for each project.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// Solution with fully evaluated projects.
    /// Projects that fail evaluation are logged but not included in results (graceful degradation).
    /// </returns>
    /// <remarks>
    /// Projects are evaluated in parallel for performance.
    /// Failed projects are logged via ILogger and skipped.
    /// </remarks>
    Task<Solution> EvaluateAllProjectsAsync(
        Solution solution,
        string configuration = "Debug",
        string? targetFramework = null,
        CancellationToken cancellationToken = default
    );
}
```

## Contract Guarantees

**Preconditions (EvaluateProjectAsync)**:
- `projectPath` MUST be absolute path to existing project file
- `configuration` MUST be non-empty (defaults to "Debug")
- `targetFramework` MAY be null (uses first target)
- .NET SDK MUST be installed on machine (for MSBuild evaluation)
- Caller MUST have read permissions on project file and referenced files

**Postconditions (EvaluateProjectAsync)**:
- Returns `Project` entity with:
  - Valid `TargetFramework` (selected from AllTargetFrameworks)
  - `ConditionalSymbols` extracted from DefineConstants
  - `CompileItems` fully resolved (globs evaluated, absolute paths)
  - `ProjectReferences` with absolute paths
- Project entity is immutable
- Same input (path + config + TFM) produces same output (deterministic)

**Preconditions (EvaluateAllProjectsAsync)**:
- `solution` MUST be non-null with valid project paths
- `configuration` and `targetFramework` follow same rules as single-project evaluation

**Postconditions (EvaluateAllProjectsAsync)**:
- Returns `Solution` with `Projects` collection containing only successfully evaluated projects
- Failed projects are logged (not silently dropped)
- At least one project SHOULD succeed (empty solution is valid but unusual)
- Projects are evaluated in parallel (no guaranteed order)

**Error Handling (Single Project)**:
- Invalid path: `ArgumentException`
- File not found: `FileNotFoundException`
- Malformed project, missing SDK, evaluation failure: `InvalidOperationException` with descriptive message
- Configuration not found: Use default configuration, log warning
- Target framework not found: `InvalidOperationException` with available frameworks listed

**Error Handling (All Projects)**:
- Per-project errors: Logged, project skipped
- No projects succeed: Empty solution returned (not exception)
- Solution-level error (e.g., cancellation): Propagates exception

**Thread Safety**:
- Implementation MUST be thread-safe (multiple projects can be evaluated concurrently)
- Returned entities are immutable (safe for concurrent reads)

## Usage Examples

### Single Project Evaluation

```csharp
IProjectProvider projectProvider = GetProjectProvider();

// Evaluate project with defaults (Debug configuration, first target framework)
Project project = await projectProvider.EvaluateProjectAsync(
    @"C:\Projects\MyProject\MyProject.csproj",
    cancellationToken: cancellationToken
);

Console.WriteLine($"Project: {project.Name}");
Console.WriteLine($"Target Framework: {project.TargetFramework.Moniker}");
Console.WriteLine($"Symbols: {string.Join(", ", project.ConditionalSymbols)}");
Console.WriteLine($"Source Files: {project.CompileItems.Count}");

// Analyze source files
foreach (var item in project.CompileItems)
{
    Console.WriteLine($"  {item.FilePath} ({item.InclusionType})");
}
```

### Multi-Targeted Project (Explicit Framework Selection)

```csharp
// Project targets net472;net8.0
// Explicitly select net8.0 for analysis
Project project = await projectProvider.EvaluateProjectAsync(
    projectPath: @"C:\Projects\MultiTarget\MultiTarget.csproj",
    configuration: "Release",
    targetFramework: "net8.0",
    cancellationToken: cancellationToken
);

// project.TargetFramework.Moniker == "net8.0"
// project.AllTargetFrameworks == ["net472", "net8.0"]
```

### Solution Evaluation (All Projects)

```csharp
ISolutionProvider solutionProvider = GetSolutionProvider();
IProjectProvider projectProvider = GetProjectProvider();

// Parse solution
Solution solution = await solutionProvider.ParseSolutionAsync(
    @"C:\Projects\MySolution.sln",
    cancellationToken
);

// Evaluate all projects (Debug configuration)
Solution evaluatedSolution = await projectProvider.EvaluateAllProjectsAsync(
    solution,
    configuration: "Debug",
    cancellationToken: cancellationToken
);

Console.WriteLine($"Evaluated {evaluatedSolution.Projects.Count} projects:");
foreach (var project in evaluatedSolution.Projects)
{
    Console.WriteLine($"  - {project.Name}: {project.CompileItems.Count} files");
}

// Access dependency graph
var graph = evaluatedSolution.GetDependencyGraph();
foreach (var (projectPath, references) in graph)
{
    Console.WriteLine($"{Path.GetFileName(projectPath)} references:");
    foreach (var refPath in references)
    {
        Console.WriteLine($"  -> {Path.GetFileName(refPath)}");
    }
}
```

## Implementation Notes

**Default Implementation**: `BuildalyzerProjectProvider` in `Lintelligent.Cli` project

**Dependencies**:
- Buildalyzer NuGet package (wraps MSBuild evaluation)
- Microsoft.Build, Microsoft.Build.Locator (via Buildalyzer)
- .NET SDK installed on machine

**Performance**:
- Project evaluation is I/O and CPU intensive (MSBuild evaluation)
- Typical: 100-500ms per project (depends on project complexity)
- Parallel evaluation recommended for multi-project solutions
- Consider caching evaluation results if analyzing same project repeatedly

**Limitations**:
- Requires .NET SDK installation (MSBuild evaluation depends on SDK)
- Custom MSBuild targets that dynamically generate files at build time are not executed
- NuGet package restore SHOULD be completed before evaluation (design-time evaluation may fail otherwise)
