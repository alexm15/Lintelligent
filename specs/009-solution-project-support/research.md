# Research: Solution & Project File Support

**Feature**: 009-solution-project-support  
**Phase**: 0 (Outline & Research)  
**Date**: December 25, 2025

## Research Tasks

### 1. Buildalyzer vs Direct MSBuild APIs

**Question**: Should we use Buildalyzer or direct MSBuild APIs (Microsoft.Build, Microsoft.Build.Locator)?

**Research Findings**:

**Buildalyzer**:
- **Pros**:
  - Abstracts MSBuild Locator complexity (automatically finds and loads correct MSBuild instance)
  - Simpler API for design-time project evaluation
  - Handles multi-targeted projects elegantly
  - Active maintenance (last updated 2024)
  - Used by production tools (NDepend, Cake.Frosting)
- **Cons**:
  - Adds external dependency
  - Less control over MSBuild evaluation process
  - May not expose all MSBuild properties (though extensible)

**Direct MSBuild APIs**:
- **Pros**:
  - Official Microsoft APIs
  - Full control over evaluation
  - No external dependencies beyond Microsoft packages
- **Cons**:
  - MSBuild.Locator is complex (must find correct MSBuild version)
  - Requires manual handling of SDK resolution
  - Multi-targeting requires manual iteration
  - More code to maintain

**Decision**: **Use Buildalyzer**

**Rationale**: 
- Buildalyzer's abstraction layer handles the most complex parts (locator, multi-targeting) reliably
- Active community usage validates production readiness
- Simpler implementation aligns with "boring and stable" constitutional principle (VI)
- Can fall back to direct MSBuild APIs if Buildalyzer proves insufficient (interface abstraction allows swapping)

**Alternatives Considered**: Direct MSBuild APIs rejected because complexity-to-benefit ratio is poor for our use case (design-time evaluation only, not custom build orchestration)

---

### 2. Solution File Parsing Strategy

**Question**: How should we parse .sln files—use MSBuild's SolutionFile class or custom parser?

**Research Findings**:

**MSBuild SolutionFile** (Microsoft.Build.Construction.SolutionFile):
- **Pros**:
  - Official parser
  - Handles all .sln formats (VS2010+, .slnx in future)
  - Exposes projects, solution folders, configurations
- **Cons**:
  - Requires Microsoft.Build package (already needed for Buildalyzer)
  - API is somewhat low-level

**Custom Parser**:
- **Pros**:
  - Can tailor to exact needs
  - No MSBuild dependency for solution parsing
- **Cons**:
  - .sln format has edge cases (nested folders, solution items, etc.)
  - Maintenance burden
  - Risk of bugs with non-standard solution files

**Decision**: **Use Microsoft.Build.Construction.SolutionFile**

**Rationale**:
- Official parser handles edge cases we might miss
- Already pulling in Microsoft.Build via Buildalyzer
- No need to reinvent the wheel for well-defined format
- Extensibility principle (VI): rely on stable, official APIs

**Alternatives Considered**: Custom parser rejected due to maintenance burden and risk of incomplete implementation

---

### 3. Conditional Compilation Symbol Handling

**Question**: How should we extract and apply DefineConstants to analysis?

**Research Findings**:

Conditional symbols come from:
1. `<DefineConstants>` in .csproj (e.g., `DEBUG;TRACE;CUSTOM`)
2. Configuration-specific PropertyGroups (Debug vs Release)
3. Platform-specific conditions (AnyCPU, x64, ARM)

**Buildalyzer Approach**:
- Exposes `AnalyzerResult.PreprocessorSymbols` per configuration
- Requires specifying configuration when analyzing (defaults to first available)
- Multi-targeted projects return multiple AnalyzerResults

**Roslyn Approach**:
- CSharpParseOptions.PreprocessorSymbols accepts IEnumerable<string>
- Roslyn's SyntaxTree parsing respects symbols for `#if/#elif/#else` directives
- DirectiveTriviaSyntax nodes can be inspected for active/inactive code

**Decision**: **Extract symbols from Buildalyzer, pass to Roslyn ParseOptions**

**Rationale**:
- Buildalyzer correctly evaluates MSBuild conditions to determine active symbols for given configuration
- Roslyn's parse options are already used in existing analysis (from Feature 001 ICodeProvider)
- Symbols need to be passed when creating SyntaxTree, not when analyzing

**Implementation Strategy**:
1. Buildalyzer provides symbols per project/configuration
2. New `Project` domain model stores symbols
3. `ICodeProvider` implementation receives symbols and passes to CSharpParseOptions.WithPreprocessorSymbols()
4. Inactive code is automatically excluded from syntax tree analysis

**Alternatives Considered**: 
- Manual XML parsing of .csproj rejected—MSBuild evaluation is more accurate (handles conditions, imports, etc.)
- Post-parse filtering rejected—Roslyn already handles this during parse if symbols are provided

---

### 4. Target Framework Selection for Multi-Targeted Projects

**Question**: When a project targets multiple frameworks (e.g., `<TargetFrameworks>net472;net8.0</TargetFrameworks>`), which should we analyze?

**Research Findings**:

**Options**:
1. **Analyze first target only** (simplest, MVP approach)
2. **Analyze all targets** (most accurate, but slower and produces more results)
3. **Allow user to specify via CLI flag** (flexible but requires CLI changes)
4. **Analyze primary target** (heuristic: latest, or most common)

**Buildalyzer Behavior**:
- Returns one `AnalyzerResult` per target framework
- Each result has different symbols, references, and compile items

**Real-World Impact**:
- Code often uses `#if NET8_0_OR_GREATER` to conditionally include modern APIs
- Analyzing all targets could produce duplicate or conflicting diagnostics
- Most teams have a "primary" target (usually latest) they care most about

**Decision**: **Analyze first target framework by default, add `--target-framework` CLI flag for explicit selection**

**Rationale**:
- MVP simplicity: Single result set easier to understand and implement
- Extensibility: CLI flag allows power users to select specific framework
- Determinism: Always analyze first target (alphabetically) ensures consistent results across runs
- Future enhancement: Can add `--all-targets` flag to analyze all frameworks

**Implementation Strategy**:
1. Buildalyzer returns `AnalyzerResults` collection (one per target)
2. Default: Take first result (usually alphabetically first target, e.g., `net472` before `net8.0`)
3. CLI flag `--target-framework net8.0`: Filter results to specified framework
4. Error if specified framework doesn't exist in project

**Alternatives Considered**:
- Analyze all targets rejected for MVP (complexity, result aggregation unclear)
- Heuristic "latest" target rejected (requires TFM parsing and version comparison)

---

### 5. Compile Include/Exclude Glob Pattern Handling

**Question**: How should we resolve `<Compile Include="**/*.cs" />` and `<Compile Remove="Generated/**" />` patterns?

**Research Findings**:

**SDK-Style Projects**:
- Default includes: `**/*.cs` (all C# files under project directory)
- Default excludes: `**/obj/**`, `**/bin/**`, project file itself
- Explicit `<Compile Remove>` adds to excludes
- Explicit `<Compile Include>` can add files outside project directory (linked files)

**Buildalyzer Handling**:
- `AnalyzerResult.SourceFiles` returns fully-resolved list of files after glob evaluation
- Already applies Include/Remove/Update logic
- Returns absolute paths

**Decision**: **Use Buildalyzer's AnalyzerResult.SourceFiles directly**

**Rationale**:
- Buildalyzer has already evaluated all globs, conditions, and MSBuild logic
- No need to reimplement glob matching (error-prone)
- SourceFiles is deterministic (same project → same files)
- Absolute paths integrate easily with existing ICodeProvider

**Implementation Strategy**:
1. Extract `AnalyzerResult.SourceFiles` for each project
2. Create `CompileItem` domain models with metadata (file path, project)
3. Pass to existing analysis engine via enhanced ICodeProvider
4. ICodeProvider may filter by extension (.cs, .vb) if needed

**Alternatives Considered**:
- Manual glob matching rejected—MSBuild evaluation is canonical
- Parsing .csproj XML directly rejected—misses imported .props/.targets and conditions

---

### 6. Project Reference Dependency Graph

**Question**: How should we capture and expose project-to-project dependencies?

**Research Findings**:

**Buildalyzer Data**:
- `AnalyzerResult.ProjectReferences` returns list of referenced project paths
- Paths are absolute (resolved by MSBuild)
- No metadata about reference (e.g., CopyLocal, ReferenceOutputAssembly)

**Use Cases**:
- **Current**: Aggregating results by project (which diagnostics belong to which project)
- **Future**: Cross-project rules (e.g., detect unused project references, API boundary violations)

**Graph Representation Options**:
1. **Simple list** per project (ProjectA → [ProjectB, ProjectC])
2. **Graph data structure** (nodes=projects, edges=references)
3. **Dependency order** (topological sort for bottom-up analysis)

**Decision**: **Store simple list per project, expose via Solution.GetDependencyGraph() method for future use**

**Rationale**:
- MVP only needs per-project tracking (which project owns which diagnostics)
- Future cross-project features can build graph on-demand
- Avoid premature optimization (graph libraries, topological sort)
- Determinism: Store raw data, compute derived structures on-demand

**Implementation Strategy**:
1. `Project` domain model includes `ProjectReferences` property (List<string> of referenced project paths)
2. `Solution` domain model includes `GetDependencyGraph()` method (returns Dictionary<string, List<string>>)
3. Analysis results include `ProjectPath` metadata for filtering/grouping
4. Future: Implement `IDependencyAnalyzerRule` interface for cross-project rules

**Alternatives Considered**:
- Graph library (QuikGraph, etc.) rejected—overkill for MVP, can add later
- Topological sort rejected—not needed until we implement dependency-order analysis

---

### 7. Missing or Malformed Project Handling

**Question**: How should we handle solution files that reference non-existent or malformed projects?

**Research Findings**:

**Common Scenarios**:
- Project file deleted but .sln not updated
- Project file moved without updating .sln
- .csproj contains invalid XML
- .csproj uses unsupported SDK or features

**Buildalyzer Behavior**:
- Throws exception on missing project files
- Throws exception on malformed XML
- MSBuild errors surface as exceptions

**User Experience Expectations**:
- Graceful degradation (analyze available projects, skip broken ones)
- Clear error messages identifying which project failed and why
- Exit code indicates partial success vs complete failure

**Decision**: **Try-catch per project, log errors, continue analysis for remaining projects**

**Rationale**:
- Partial results are better than no results (especially in large solutions)
- Clear logging helps users fix issues
- Exit code indicates if any projects failed (non-zero)

**Implementation Strategy**:
1. Parse solution to get project list (may succeed even if projects missing)
2. For each project:
   - Try: Buildalyzer evaluation
   - Catch: Log error with project path and exception message
   - Continue: Process next project
3. Aggregate results from successful projects
4. Exit code: 0 if all projects succeeded, 1 if any failed (even if partial results)
5. Error log: "Failed to analyze {ProjectPath}: {ErrorMessage}"

**Alternatives Considered**:
- Fail-fast (stop on first error) rejected—prevents analyzing valid projects
- Silent skipping rejected—users need to know something failed
- Retry logic rejected—project evaluation errors are typically not transient

---

### 8. Configuration Selection (Debug/Release)

**Question**: How should we determine which configuration (Debug/Release) to use for analysis?

**Research Findings**:

**Configuration Impact**:
- Different DefineConstants (DEBUG vs RELEASE)
- Different preprocessor symbols
- Different output paths (not relevant for analysis)
- Different optimization settings (not relevant for static analysis)

**Buildalyzer API**:
- `AnalyzerManager.Projects[projectPath].Build(configuration)` accepts configuration
- Defaults to first configuration if not specified (usually "Debug")
- `ProjectAnalyzer.Build()` (no args) uses default configuration

**Common Patterns**:
- Most developers analyze in Debug mode (matches local development)
- CI often builds in Release mode
- Some projects have custom configurations (Staging, Production)

**Decision**: **Default to "Debug", add `--configuration` CLI flag for explicit selection**

**Rationale**:
- Debug is most common development configuration
- Determinism: Always analyze same configuration by default
- Flexibility: CLI flag allows matching CI build configuration
- Simplicity: No auto-detection or heuristics needed

**Implementation Strategy**:
1. CLI accepts `--configuration Debug|Release|Custom` flag (default: Debug)
2. Pass configuration to Buildalyzer's `Build(configuration)` method
3. Error if configuration doesn't exist in project
4. Log which configuration is being used

**Alternatives Considered**:
- Auto-detect from environment (CONFIGURATION env var) rejected—explicit over implicit
- Analyze all configurations rejected—produces duplicate/conflicting results
- Match active VS configuration rejected—not deterministic across environments

---

## Summary of Decisions

| Decision Area | Selected Approach | Key Rationale |
|---------------|-------------------|---------------|
| MSBuild Evaluation | Buildalyzer | Simpler API, handles complexity, production-proven |
| Solution Parsing | Microsoft.Build.Construction.SolutionFile | Official parser, handles edge cases |
| Conditional Symbols | Buildalyzer → Roslyn ParseOptions | Accurate MSBuild evaluation + Roslyn integration |
| Multi-Target | First target by default + CLI flag | MVP simplicity + future flexibility |
| Glob Patterns | Buildalyzer SourceFiles | Already evaluated, deterministic |
| Dependency Graph | Simple list per project | MVP storage, defer graph computation |
| Error Handling | Try-catch per project, continue | Partial results + clear errors |
| Configuration | Default "Debug" + CLI flag | Common case + explicit control |

**No NEEDS CLARIFICATION items remain**. All technical unknowns resolved with concrete decisions and rationale.
