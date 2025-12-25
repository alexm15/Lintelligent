# Feature Specification: Solution & Project File Support

**Feature Branch**: `009-solution-project-support`  
**Created**: December 25, 2025  
**Status**: Draft  
**Priority**: P2  
**Constitutional Principles**: I (Clear Boundaries), VII (Build Tool Integration)  
**Input**: User description: "Solution & Project File Support: Analyze .sln and .csproj files to understand project structure and dependencies"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Analyze Single Project from Solution (Priority: P1)

A developer runs Lintelligent on a Visual Studio solution file (.sln) and wants all projects within that solution to be analyzed automatically, respecting the compilation context defined in each .csproj file.

**Why this priority**: This is the fundamental workflow - most .NET developers work with solutions containing multiple projects, and manually specifying each project would be cumbersome and error-prone.

**Independent Test**: Can be fully tested by running `lintelligent scan MySolution.sln` and verifying that all projects listed in the solution are discovered and analyzed, with results aggregated into a single report.

**Acceptance Scenarios**:

1. **Given** a solution file with 3 projects (A, B, C), **When** user runs `lintelligent scan Solution.sln`, **Then** all three projects are analyzed and results include diagnostics from all projects
2. **Given** a solution with nested solution folders, **When** user analyzes the solution, **Then** projects in nested folders are discovered and analyzed
3. **Given** a solution with different project types (.csproj, .vbproj), **When** user analyzes the solution, **Then** both C# and VB.NET projects are analyzed appropriately

---

### User Story 2 - Respect Conditional Compilation (Priority: P1)

A developer has a project with conditional compilation symbols (e.g., DEBUG, RELEASE, CUSTOM_FEATURE) defined in the .csproj file. The analyzer should respect these symbols to accurately understand which code is actually compiled.

**Why this priority**: Conditional compilation is widely used in .NET projects. Analyzing code that won't be compiled in the current configuration produces misleading results and false positives.

**Independent Test**: Can be tested by creating a project with `#if DEBUG` blocks, running analysis with Debug configuration, and verifying that only Debug-active code is analyzed.

**Acceptance Scenarios**:

1. **Given** a .csproj with `<DefineConstants>DEBUG;TRACE</DefineConstants>`, **When** analyzer processes the project, **Then** code within `#if DEBUG` blocks is analyzed
2. **Given** a .csproj with `<DefineConstants>RELEASE</DefineConstants>`, **When** analyzer processes the project, **Then** code within `#if DEBUG` blocks is skipped
3. **Given** a .csproj with multiple configurations (Debug/Release), **When** user specifies configuration via CLI flag, **Then** appropriate conditional symbols are used

---

### User Story 3 - Handle Compile Includes/Excludes (Priority: P1)

A developer has a project with custom `<Compile Include="...">` and `<Compile Remove="...">` directives, linked files, or glob patterns. The analyzer must respect these directives to analyze only the files that are actually compiled.

**Why this priority**: Projects often exclude generated files, include shared files from other locations, or use glob patterns. Analyzing wrong files leads to inaccurate results.

**Independent Test**: Can be tested by creating a project with `<Compile Remove="Temp/**/*.cs" />`, placing files in Temp/, and verifying they are not analyzed.

**Acceptance Scenarios**:

1. **Given** a .csproj with `<Compile Remove="Generated/**/*.cs" />`, **When** analyzer processes the project, **Then** files in Generated/ folder are excluded from analysis
2. **Given** a .csproj with `<Compile Include="..\Shared\*.cs" />` (linked files), **When** analyzer processes the project, **Then** linked files from Shared folder are included
3. **Given** a .csproj using SDK-style glob patterns, **When** analyzer processes the project, **Then** all .cs files matching glob patterns are analyzed, respecting default includes/excludes

---

### User Story 4 - Multi-Project Analysis Aggregation (Priority: P2)

A developer analyzes a solution with multiple projects that reference each other. The analyzer should aggregate results across all projects and understand project-to-project dependencies for contextual analysis.

**Why this priority**: Understanding cross-project relationships enables better diagnostics (e.g., detecting unused project references, understanding API usage across boundaries). This is essential for solution-level analysis quality.

**Independent Test**: Can be tested by creating a solution where ProjectA references ProjectB, running analysis, and verifying that both projects' results are aggregated with dependency metadata preserved.

**Acceptance Scenarios**:

1. **Given** a solution where ProjectA references ProjectB, **When** analyzer processes the solution, **Then** results clearly indicate which diagnostics belong to which project
2. **Given** a solution with 5 projects, **When** analysis completes, **Then** output shows total diagnostic count across all projects with per-project breakdown
3. **Given** a solution with project dependencies, **When** analysis runs, **Then** dependency graph is available for future cross-project rules (metadata captured even if not yet used)

---

### User Story 5 - Target Framework Awareness (Priority: P2)

A developer has projects targeting different .NET versions (.NET Framework 4.7.2, .NET 6, .NET 8). The analyzer should be aware of target frameworks to avoid reporting diagnostics for APIs that don't exist in older frameworks.

**Why this priority**: Target framework determines available APIs and language features. Reporting errors for .NET 8 APIs in a .NET Framework 4.7.2 project creates confusion.

**Independent Test**: Can be tested by creating a multi-targeted project (`<TargetFrameworks>net472;net8.0</TargetFrameworks>`), using .NET 8-specific APIs, and verifying diagnostics are framework-aware.

**Acceptance Scenarios**:

1. **Given** a project targeting `net472`, **When** code uses .NET Framework 4.7.2 APIs, **Then** no false diagnostics about unavailable APIs
2. **Given** a project with `<TargetFrameworks>net6.0;net8.0</TargetFrameworks>`, **When** analyzer processes the project, **Then** analysis runs for both target frameworks (or a specified one)
3. **Given** a project targeting `netstandard2.0`, **When** analyzer processes the project, **Then** analysis respects .NET Standard API surface

---

### Edge Cases

- What happens when a solution file references a project file that doesn't exist on disk?
- How does the system handle circular project references (should be impossible in valid solutions, but malformed files exist)?
- What happens when a .csproj file is malformed or contains invalid XML?
- How does the system handle multi-targeted projects (`<TargetFrameworks>` with semicolon-separated list)?
- What happens when a project uses custom MSBuild targets that dynamically modify the file list?
- How does the system handle projects with platform-specific compilation (`Condition="'$(Platform)'=='x64'"`)?
- What happens when solution configurations (Debug/Release) map to different project configurations?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST parse Visual Studio solution files (.sln) to discover all projects within the solution
- **FR-002**: System MUST parse .NET project files (.csproj, .vbproj, .fsproj) to extract compilation settings
- **FR-003**: System MUST respect `<Compile Include="...">` and `<Compile Remove="...">` directives when determining which files to analyze
- **FR-004**: System MUST support SDK-style project files with implicit glob patterns (e.g., `**/*.cs` is default include)
- **FR-005**: System MUST extract conditional compilation symbols (`<DefineConstants>`) from project configuration
- **FR-006**: System MUST respect the current configuration (Debug/Release/Custom) when determining which files and symbols apply
- **FR-007**: System MUST handle multi-targeted projects by analyzing for the primary target framework (or allowing user to specify which target)
- **FR-008**: System MUST aggregate analysis results from all projects in a solution into a unified report
- **FR-009**: System MUST capture project-to-project reference metadata (ProjectReference) for future dependency-aware analysis
- **FR-010**: System MUST handle linked files (files included from outside the project directory)
- **FR-011**: System MUST gracefully handle missing project files referenced in solution with clear error messages
- **FR-012**: System MUST use MSBuild evaluation APIs or equivalent (e.g., Buildalyzer) to accurately process project files as MSBuild would
- **FR-013**: CLI MUST accept solution file paths as input in addition to individual project files or directories
- **FR-014**: System MUST log which projects are discovered and analyzed for transparency

### Key Entities

- **Solution**: Represents a .sln file containing zero or more projects, solution folders, and configuration mappings
  - Attributes: File path, list of projects, solution configurations (Debug/Release)
  - Relationships: Contains multiple Projects
  
- **Project**: Represents a .csproj/.vbproj/.fsproj file with compilation settings
  - Attributes: File path, target framework(s), conditional symbols, output type (exe/library), platform, configuration
  - Relationships: References other Projects, contains CompileItems, belongs to Solution
  
- **CompileItem**: Represents a source file to be analyzed
  - Attributes: File path (absolute), inclusion reason (explicit include, glob pattern, linked file)
  - Relationships: Belongs to Project
  
- **ProjectReference**: Represents a reference from one project to another
  - Attributes: Source project, target project, reference metadata
  - Relationships: Links two Project entities

- **TargetFramework**: Represents the .NET platform version a project targets
  - Attributes: Target framework moniker (e.g., net8.0, net472, netstandard2.0)
  - Relationships: Associated with Project

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When analyzing a solution with 10 projects, all 10 projects are discovered and analyzed without manual intervention
- **SC-002**: Analysis results correctly exclude files marked with `<Compile Remove="...">` directives
- **SC-003**: Analysis results correctly include files specified with `<Compile Include="...">` directives, including linked files
- **SC-004**: When a project has `<DefineConstants>DEBUG;CUSTOM</DefineConstants>`, analysis processes code within `#if DEBUG` and `#if CUSTOM` blocks
- **SC-005**: When analyzing a multi-targeted project, analysis targets at least one framework without errors (with option to specify which)
- **SC-006**: Analysis aggregation report clearly shows per-project diagnostic counts and totals
- **SC-007**: System produces clear error messages for missing or malformed project files rather than failing silently
- **SC-008**: Analysis of a 20-project solution completes in reasonable time (performance baseline to be established)

## Assumptions

- Users are analyzing valid Visual Studio solutions and project files (not hand-crafted malformed XML)
- MSBuild or compatible evaluation library (Buildalyzer) is available in the runtime environment
- Projects use either SDK-style (.NET Core/5+/6+) or legacy .csproj format (both are supported by MSBuild APIs)
- For multi-targeted projects, analyzing a single target framework is acceptable for MVP (full multi-target support can be added later)
- Solution configurations (Debug/Release) are standard; custom configuration names are lower priority
- Projects are on local disk (remote projects or NuGet-based projects are out of scope)

## Out of Scope

The following are explicitly NOT part of this feature:

- Analyzing projects from .NET Framework web.config or app.config files
- Analyzing non-.NET projects (C++, Python, etc.) that might be in the same solution
- Executing custom MSBuild targets or build scripts as part of analysis
- Resolving NuGet packages or analyzing dependencies from packages
- Cross-project code flow analysis (dependency graph is captured for future use, but not analyzed)
- Modifying or writing to solution/project files
- Supporting project.json (legacy .NET Core format, deprecated)
- Analyzing projects that require restoration before they can be evaluated (user must ensure `dotnet restore` has run)

## Dependencies

### Technical Dependencies

- **MSBuild APIs** or **Buildalyzer library**: Required for accurate project file evaluation
  - MSBuild APIs: Microsoft.Build, Microsoft.Build.Locator (official but complex)
  - Buildalyzer: NuGet package that simplifies MSBuild project analysis (recommended)
  - Decision: Use Buildalyzer as it provides simpler API for design-time project evaluation

- **.NET SDK**: Must be installed on the machine for MSBuild evaluation to work (already required for development)

### Feature Dependencies

- **Feature 001 (IO Boundary Refactor)**: ICodeProvider abstraction must be extended or a new ISolutionProvider/IProjectProvider abstraction created
- **Feature 006 (Structured Output Formats)**: Aggregated multi-project results should be represented in JSON/SARIF output with project metadata

### Risks

- **MSBuild complexity**: MSBuild evaluation is complex; edge cases (custom targets, dynamic file generation) may not be fully handled
  - Mitigation: Start with Buildalyzer which abstracts most complexity; document unsupported scenarios
  
- **Performance**: Evaluating large solutions with many projects could be slow
  - Mitigation: Implement parallel project analysis; measure and optimize as needed
  
- **Multi-targeting**: Handling projects with multiple target frameworks requires choosing one or analyzing all
  - Mitigation: MVP analyzes first target framework only; add `--target-framework` CLI flag for user choice

## Implementation Notes

These notes are for planning purposes only and do not dictate implementation details:

- Consider using **Buildalyzer** NuGet package (wraps MSBuild Locator and evaluation)
- May need to introduce new abstractions: `ISolutionProvider`, `IProjectProvider` 
- Existing `ICodeProvider` may become a lower-level primitive consumed by `IProjectProvider`
- Project evaluation should happen once per project, caching results
- Dependency graph (ProjectReference) can be stored as metadata even if not immediately used by rules
- Consider making `--configuration` and `--target-framework` CLI flags for explicit control
