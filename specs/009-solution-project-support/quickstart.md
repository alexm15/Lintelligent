# Quickstart: Solution & Project File Support

**Feature**: 009-solution-project-support  
**Audience**: Developers implementing or using this feature  
**Estimated Reading Time**: 10 minutes

## Overview

This feature enables Lintelligent to analyze entire Visual Studio solutions by parsing .sln files and evaluating .csproj files using MSBuild. The analyzer now understands compilation context (conditional symbols, file includes/excludes, target frameworks) and aggregates results across multiple projects.

## Prerequisites

- .NET 10.0 SDK installed (required for MSBuild evaluation)
- Solution or project files on local disk
- `dotnet restore` run on solution (recommended but not strictly required)

## Quick Start Guide

### Analyze a Solution

```bash
# Analyze all projects in a solution (Debug configuration, default)
lintelligent scan MySolution.sln

# Analyze with Release configuration
lintelligent scan MySolution.sln --configuration Release

# Analyze specific target framework for multi-targeted projects
lintelligent scan MySolution.sln --target-framework net8.0

# Analyze with JSON output showing per-project breakdown
lintelligent scan MySolution.sln --format json --output results.json
```

### Analyze a Single Project

```bash
# Still works as before (backward compatible)
lintelligent scan MyProject.csproj

# With specific configuration and target framework
lintelligent scan MyProject.csproj --configuration Release --target-framework net472
```

### Analyze a Directory (Existing Behavior)

```bash
# Existing directory scanning still works
lintelligent scan ./src/
```

## How It Works

### 1. Solution Discovery

When you provide a .sln file:

1. **Parse Solution**: ISolutionProvider extracts project paths and configurations
2. **Validate Projects**: Check that referenced project files exist
3. **Log Discovery**: Output which projects were found

```text
Analyzing solution: MySolution.sln
Discovered 5 projects:
  - ProjectA (src/ProjectA/ProjectA.csproj)
  - ProjectB (src/ProjectB/ProjectB.csproj)
  - ProjectC (tests/ProjectC.Tests/ProjectC.Tests.csproj)
  - ProjectD (src/ProjectD/ProjectD.csproj)
  - ProjectE (src/ProjectE/ProjectE.csproj)

Configuration: Debug
```

### 2. Project Evaluation

For each project:

1. **MSBuild Evaluation**: IProjectProvider uses Buildalyzer to evaluate project
2. **Extract Settings**:
   - Target framework(s)
   - Conditional symbols (DEBUG, TRACE, etc.)
   - Source files (after glob evaluation)
   - Project references
3. **Handle Errors**: Log and skip projects that fail evaluation

```text
Evaluating ProjectA (net8.0)...
  - Conditional symbols: DEBUG, TRACE
  - Source files: 42
  - Project references: ProjectB, ProjectC

Evaluating ProjectB (net472;net8.0)...
  - Selected framework: net472 (first target)
  - Conditional symbols: DEBUG, TRACE, NET472
  - Source files: 28
  - Project references: (none)

ERROR: ProjectC evaluation failed: Missing SDK
```

### 3. Analysis Execution

1. **Analyze Each Project**: Run existing rules on each project's source files
2. **Respect Conditional Compilation**: Only analyze code active for selected configuration
3. **Aggregate Results**: Combine diagnostics from all projects

```text
Analyzing ProjectA...
  - LNT001: 3 violations
  - LNT005: 1 violation

Analyzing ProjectB...
  - LNT001: 2 violations
  - LNT008: 5 violations

Total: 11 violations across 2 projects
```

### 4. Report Generation

Results include project metadata:

```json
{
  "summary": {
    "totalViolations": 11,
    "projectsAnalyzed": 2,
    "projectsFailed": 1
  },
  "projects": [
    {
      "name": "ProjectA",
      "path": "C:\\Projects\\MySolution\\src\\ProjectA\\ProjectA.csproj",
      "targetFramework": "net8.0",
      "violations": [
        {
          "ruleId": "LNT001",
          "filePath": "C:\\Projects\\MySolution\\src\\ProjectA\\LongMethod.cs",
          "line": 10,
          "message": "Method 'ProcessData' exceeds maximum line count (82 > 60)"
        }
      ]
    }
  ]
}
```

## Key Concepts

### Conditional Compilation

The analyzer respects `#if/#elif/#else` directives:

```csharp
// In Debug configuration, this code IS analyzed
#if DEBUG
public void DebugOnlyMethod() 
{
    // LNT001 violation detected here
}
#endif

// In Release configuration, this code IS NOT analyzed (skipped)
#if DEBUG
public void DebugOnlyMethod() 
{
    // No violation reported in Release mode
}
#endif
```

### Multi-Targeted Projects

For projects with multiple targets:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net472;net8.0</TargetFrameworks>
  </PropertyGroup>
</Project>
```

**Default Behavior**: Analyze first target (net472)

```bash
# Analyzes net472 target
lintelligent scan MyProject.csproj
```

**Explicit Selection**: Specify target framework

```bash
# Analyzes net8.0 target
lintelligent scan MyProject.csproj --target-framework net8.0
```

### Compile Includes/Excludes

The analyzer respects MSBuild compile directives:

```xml
<!-- Exclude generated files -->
<Compile Remove="Generated/**/*.cs" />

<!-- Include shared files from another directory -->
<Compile Include="..\SharedCode\*.cs" Link="Shared\%(FileName)%(Extension)" />
```

**Result**: Generated files are NOT analyzed, shared files ARE analyzed.

### Project Dependencies

The analyzer captures dependency graph for future use:

```text
Solution Dependency Graph:
  ProjectA → ProjectB, ProjectC
  ProjectB → (none)
  ProjectC → ProjectD
  ProjectD → (none)
```

**Current Use**: Per-project result aggregation  
**Future Use**: Cross-project rules (unused references, API boundary violations)

## Error Handling

### Missing Projects

If a solution references a project that doesn't exist:

```text
WARNING: Project not found: C:\Projects\MySolution\src\ProjectX\ProjectX.csproj
Skipping ProjectX...

Continuing with remaining 4 projects...
```

### Malformed Projects

If a project file is malformed:

```text
ERROR: Failed to evaluate ProjectY: Invalid XML at line 15
Skipping ProjectY...

Continuing with remaining 4 projects...
```

### No Projects Succeed

If all projects fail:

```text
ERROR: No projects could be evaluated successfully
0 violations found (no source code analyzed)
Exit code: 1
```

## Integration with Existing Features

### Feature 006: Structured Output Formats

Solution analysis supports all output formats:

```bash
# JSON with project metadata
lintelligent scan MySolution.sln --format json

# Markdown report with per-project sections
lintelligent scan MySolution.sln --format markdown

# SARIF with project-level location metadata
lintelligent scan MySolution.sln --format sarif
```

### Feature 007: Rule Filtering (Future)

Once implemented, filtering applies to all projects:

```bash
# Only LNT001 rule across all projects
lintelligent scan MySolution.sln --rules LNT001

# Exclude specific rules
lintelligent scan MySolution.sln --exclude-rules LNT008
```

### Feature 008: Exit Codes (Future)

Exit code indicates success/failure across all projects:

```bash
lintelligent scan MySolution.sln
# Exit 0: No violations in any project
# Exit 1: Violations found in one or more projects
# Exit 2: One or more projects failed evaluation
```

## Performance Tips

### Large Solutions

For solutions with many projects (50+):

```bash
# Parallel evaluation is automatic, but ensure sufficient resources
lintelligent scan LargeSolution.sln

# Consider analyzing subset of projects
lintelligent scan ProjectA.csproj ProjectB.csproj

# Or use directory scanning for specific folder
lintelligent scan ./src/CriticalProjects/
```

### Caching

Evaluation results are not cached between runs. For repeated analysis:

```bash
# Run once, save results
lintelligent scan MySolution.sln --format json --output baseline.json

# Compare future runs against baseline
lintelligent scan MySolution.sln --format json --output current.json
diff baseline.json current.json
```

## Troubleshooting

### "Missing SDK" Error

**Problem**: Project requires SDK not installed

**Solution**: Install required .NET SDK version

```bash
dotnet --list-sdks
# Install missing SDK from https://dotnet.microsoft.com/download
```

### "Package restore required" Warning

**Problem**: NuGet packages not restored

**Solution**: Run restore before analysis

```bash
dotnet restore MySolution.sln
lintelligent scan MySolution.sln
```

### Incorrect Conditional Symbols

**Problem**: Wrong code is being analyzed (DEBUG vs RELEASE)

**Solution**: Specify configuration explicitly

```bash
# Ensure Release configuration
lintelligent scan MySolution.sln --configuration Release
```

### Performance Issues

**Problem**: Analysis is slow

**Diagnosis**:
1. Check project count: `dotnet sln MySolution.sln list`
2. Check project complexity: Large .csproj files with many conditions

**Solutions**:
- Analyze smaller subsets (specific projects or directories)
- Ensure .NET SDK is up-to-date (newer MSBuild is faster)
- Consider excluding test projects if not needed

## Next Steps

- **API Documentation**: See [ISolutionProvider.md](contracts/ISolutionProvider.md) and [IProjectProvider.md](contracts/IProjectProvider.md)
- **Data Model**: See [data-model.md](data-model.md) for entity definitions
- **Implementation Plan**: See [plan.md](plan.md) for architecture details
