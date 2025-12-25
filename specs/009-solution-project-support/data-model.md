# Data Model: Solution & Project File Support

**Feature**: 009-solution-project-support  
**Phase**: 1 (Design & Contracts)  
**Date**: December 25, 2025

## Domain Models

### Solution

Represents a parsed Visual Studio solution file (.sln) with its contained projects and configuration mappings.

```csharp
/// <summary>
/// Represents a Visual Studio solution with projects and configurations.
/// </summary>
public sealed class Solution
{
    /// <summary>
    /// Absolute path to the .sln file.
    /// </summary>
    public string FilePath { get; }
    
    /// <summary>
    /// Solution display name (file name without extension).
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// All projects contained in the solution.
    /// </summary>
    public IReadOnlyList<Project> Projects { get; }
    
    /// <summary>
    /// Solution-level configurations (e.g., Debug, Release).
    /// </summary>
    public IReadOnlyList<string> Configurations { get; }
    
    /// <summary>
    /// Get dependency graph for all projects.
    /// Returns dictionary mapping project path to list of referenced project paths.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetDependencyGraph();
}
```

**Validation Rules**:
- `FilePath` must be absolute path to existing .sln file
- `Name` must be non-empty
- `Projects` may be empty (valid but unusual solution)
- `Configurations` must contain at least one configuration

**Relationships**:
- Contains 0..N `Project` entities
- Projects may reference each other (captured in dependency graph)

---

### Project

Represents a .NET project file (.csproj, .vbproj, .fsproj) with evaluated build settings from MSBuild.

```csharp
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
}
```

**Validation Rules**:
- `FilePath` must be absolute path to existing project file
- `Name` must be non-empty
- `TargetFramework` must be non-null and valid TFM
- `AllTargetFrameworks` must contain at least one framework
- `TargetFramework` must be in `AllTargetFrameworks`
- `ConditionalSymbols` may be empty (no symbols defined)
- `Configuration` and `Platform` must be non-empty
- `OutputType` must be valid MSBuild value (Exe, Library, WinExe)
- `CompileItems` may be empty (valid but unusual)
- `ProjectReferences` may be empty (no dependencies)

**Relationships**:
- Contains 0..N `CompileItem` entities
- Contains 0..N `ProjectReference` entities
- Belongs to 0..1 `Solution` (orphan projects allowed)

---

### CompileItem

Represents a source file to be analyzed, with metadata about how it was included in the project.

```csharp
/// <summary>
/// Represents a source file included in project compilation.
/// </summary>
public sealed class CompileItem
{
    /// <summary>
    /// Absolute path to the source file.
    /// </summary>
    public string FilePath { get; }
    
    /// <summary>
    /// Indicates how this file was included in the project.
    /// </summary>
    public CompileItemInclusionType InclusionType { get; }
    
    /// <summary>
    /// Original path as specified in project file (may be relative or outside project).
    /// Null for files included via default glob patterns.
    /// </summary>
    public string? OriginalIncludePath { get; }
}

/// <summary>
/// How a compile item was included in the project.
/// </summary>
public enum CompileItemInclusionType
{
    /// <summary>
    /// Included via default SDK glob pattern (**/*.cs).
    /// </summary>
    DefaultGlob,
    
    /// <summary>
    /// Explicitly included via &lt;Compile Include="..." /&gt;.
    /// </summary>
    ExplicitInclude,
    
    /// <summary>
    /// Linked file from outside project directory.
    /// </summary>
    LinkedFile
}
```

**Validation Rules**:
- `FilePath` must be absolute path to existing file
- `InclusionType` must be valid enum value
- `OriginalIncludePath` is null for DefaultGlob, non-null for ExplicitInclude/LinkedFile

**Relationships**:
- Belongs to exactly one `Project`

---

### ProjectReference

Represents a reference from one project to another project (ProjectReference in MSBuild).

```csharp
/// <summary>
/// Represents a project-to-project reference.
/// </summary>
public sealed class ProjectReference
{
    /// <summary>
    /// Absolute path to the referenced project file.
    /// </summary>
    public string ReferencedProjectPath { get; }
    
    /// <summary>
    /// Name of the referenced project (file name without extension).
    /// </summary>
    public string ReferencedProjectName { get; }
}
```

**Validation Rules**:
- `ReferencedProjectPath` must be absolute path (may not exist—handled gracefully)
- `ReferencedProjectName` must be non-empty

**Relationships**:
- Belongs to exactly one `Project` (the referencing project)
- References exactly one project (by path, not object reference to avoid circular dependencies)

---

### TargetFramework

Represents a .NET target framework moniker (TFM).

```csharp
/// <summary>
/// Represents a .NET target framework moniker.
/// </summary>
public sealed class TargetFramework : IEquatable<TargetFramework>
{
    /// <summary>
    /// Short-form TFM (e.g., net8.0, net472, netstandard2.0).
    /// </summary>
    public string Moniker { get; }
    
    /// <summary>
    /// Framework family (e.g., .NETCoreApp, .NETFramework, .NETStandard).
    /// Derived from moniker.
    /// </summary>
    public string FrameworkFamily { get; }
    
    /// <summary>
    /// Framework version (e.g., 8.0, 4.7.2, 2.0).
    /// Derived from moniker.
    /// </summary>
    public string Version { get; }
    
    /// <summary>
    /// Indicates if this is a .NET Core/.NET 5+ framework.
    /// </summary>
    public bool IsModernDotNet => FrameworkFamily == ".NETCoreApp" || Moniker.StartsWith("net") && !Moniker.StartsWith("net4");
    
    /// <summary>
    /// Indicates if this is .NET Framework (net45, net472, etc.).
    /// </summary>
    public bool IsNetFramework => FrameworkFamily == ".NETFramework" || Moniker.StartsWith("net4");
    
    /// <summary>
    /// Indicates if this is .NET Standard.
    /// </summary>
    public bool IsNetStandard => FrameworkFamily == ".NETStandard";
}
```

**Validation Rules**:
- `Moniker` must be non-empty and valid TFM format
- `FrameworkFamily` must be recognized (.NETCoreApp, .NETFramework, .NETStandard)
- `Version` must be valid version string

**Relationships**:
- Associated with one or more `Project` entities

---

## Entity Relationships Diagram

```text
Solution (1)
  └── contains ──> (0..N) Project
                      ├── contains ──> (0..N) CompileItem
                      ├── contains ──> (0..N) ProjectReference
                      │                         └── references ──> Project (by path)
                      └── has ──> (1..N) TargetFramework
```

---

## Data Flow

### 1. Solution Discovery Flow

```text
.sln file path
    ↓
ISolutionProvider.ParseSolution()
    ↓
Microsoft.Build.Construction.SolutionFile.Parse()
    ↓
Extract project paths
    ↓
Solution entity (with project paths)
```

### 2. Project Evaluation Flow

```text
.csproj file path + configuration + target framework
    ↓
IProjectProvider.EvaluateProject()
    ↓
Buildalyzer.ProjectAnalyzer.Build(configuration)
    ↓
AnalyzerResult (MSBuild evaluation)
    ↓
Extract:
  - TargetFramework(s)
  - ConditionalSymbols (from DefineConstants)
  - SourceFiles (from Compile items)
  - ProjectReferences
    ↓
Project entity + CompileItem entities + ProjectReference entities
```

### 3. Analysis Execution Flow

```text
Solution entity
    ↓
For each Project:
    ↓
  For each CompileItem:
      ↓
    ICodeProvider.GetCode(filePath, conditionalSymbols)
        ↓
      Parse to SyntaxTree (with preprocessor symbols)
          ↓
        Existing IAnalyzerRule.Analyze()
            ↓
          DiagnosticResult (with project metadata)
              ↓
            Aggregate by project
                ↓
              IReportGenerator.GenerateReport()
```

---

## Implementation Notes

**Immutability**:
- All entities are immutable (readonly properties, no setters)
- Constructed once during parsing/evaluation phase
- Safe for concurrent access during analysis

**Validation**:
- Constructor validation ensures entities are always in valid state
- Invalid data throws ArgumentException with clear message

**Equality**:
- `TargetFramework` implements IEquatable for comparison
- Other entities use reference equality (instances represent unique parsed data)

**Performance**:
- CompileItem and ProjectReference use lightweight value types where possible
- IReadOnlyList/IReadOnlyDictionary prevent accidental mutation
- Lazy evaluation of dependency graph (computed on-demand)
