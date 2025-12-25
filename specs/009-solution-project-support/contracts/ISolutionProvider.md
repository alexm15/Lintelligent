# Interface: ISolutionProvider

**Namespace**: `Lintelligent.AnalyzerEngine.Abstractions`  
**Assembly**: `Lintelligent.AnalyzerEngine`  
**Purpose**: Framework-agnostic abstraction for parsing Visual Studio solution files

## Interface Definition

```csharp
namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
/// Provides solution file parsing capabilities.
/// </summary>
public interface ISolutionProvider
{
    /// <summary>
    /// Parses a Visual Studio solution file to discover projects and configurations.
    /// </summary>
    /// <param name="solutionPath">Absolute path to the .sln file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// Parsed solution with project paths and configurations.
    /// Projects are not yet evaluated (no MSBuild analysis performed).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="solutionPath"/> is null, empty, or not an absolute path.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when solution file does not exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when solution file is malformed or cannot be parsed.
    /// </exception>
    Task<Solution> ParseSolutionAsync(string solutionPath, CancellationToken cancellationToken = default);
}
```

## Contract Guarantees

**Preconditions**:
- `solutionPath` MUST be absolute path to existing .sln file
- Caller MUST have read permissions on solution file

**Postconditions**:
- Returns `Solution` entity with:
  - Valid `FilePath` (absolute, normalized)
  - At least one configuration (even if solution is empty)
  - Project paths (absolute, may not exist—validation deferred to IProjectProvider)
- Solution entity is immutable
- Same input produces same output (deterministic)

**Error Handling**:
- Invalid path: `ArgumentException`
- File not found: `FileNotFoundException`
- Malformed solution: `InvalidOperationException` with descriptive message
- Exceptions include solution path in message for diagnostics

**Thread Safety**:
- Implementation MUST be thread-safe (multiple solutions can be parsed concurrently)
- Returned `Solution` entity is immutable (safe for concurrent reads)

## Usage Example

```csharp
ISolutionProvider solutionProvider = GetSolutionProvider();

// Parse solution
Solution solution = await solutionProvider.ParseSolutionAsync(
    @"C:\Projects\MySolution.sln",
    cancellationToken
);

Console.WriteLine($"Solution: {solution.Name}");
Console.WriteLine($"Projects: {solution.Projects.Count}");
Console.WriteLine($"Configurations: {string.Join(", ", solution.Configurations)}");

// Projects at this point only have paths—not yet evaluated
foreach (var project in solution.Projects)
{
    Console.WriteLine($"  - {project.Name} ({project.FilePath})");
    // project.TargetFramework, ConditionalSymbols, etc. are not yet available
}
```

## Implementation Notes

**Default Implementation**: `BuildalyzerSolutionProvider` in `Lintelligent.Cli` project

**Dependencies**:
- Microsoft.Build.Construction (for SolutionFile.Parse)
- System.IO (for path validation)

**Performance**:
- Solution parsing is fast (typically <100ms for 50-project solution)
- No MSBuild evaluation performed—only text parsing
- Can be cached if same solution parsed multiple times
