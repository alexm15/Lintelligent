# Implementation Plan: Explicit CLI Execution Model

**Branch**: `003-explicit-cli-execution` | **Date**: 2025-12-23 | **Spec**: [spec.md](spec.md)  
**Status**: Phase 1 Complete (Design) | **Next**: Phase 2 (Task Breakdown via `/speckit.tasks`)

## Summary

Implement explicit CLI execution model (CliApplicationBuilder → Build → Execute → Exit) to replace implicit async hosting pattern currently used in Program.cs. This establishes predictable, synchronous command execution enabling in-memory testing without process spawning, directly implementing Constitutional Principle IV (Explicit Execution Model).

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: Microsoft.CodeAnalysis.CSharp 4.12.0 (AnalyzerEngine), Microsoft.Extensions.Hosting 10.0.1 (CLI - to be REMOVED)  
**Storage**: File system only (read .cs files, output reports to stdout)  
**Testing**: xUnit (existing test projects: Lintelligent.AnalyzerEngine.Tests, Lintelligent.Cli.Tests)  
**Target Platform**: Cross-platform CLI (Windows, Linux, macOS - .NET 10 runtime)
**Project Type**: Single-repo multi-project solution (3 src projects: AnalyzerEngine, Reporting, Cli)  
**Performance Goals**: <50ms in-memory command execution for testing, <5s for real scan of small codebase  
**Constraints**: Zero process spawning in tests, synchronous Main() entry point, no hosting framework dependency  
**Scale/Scope**: Single CLI command (ScanCommand) to refactor, ~100 LOC in Program.cs/Bootstrapper.cs to replace

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Check (Pre-Research)
- [x] **Layered Architecture**: Does this feature respect layer boundaries? (CLI → AnalyzerEngine/Reporting → Rules)
  - ✅ Changes confined to CLI layer only; AnalyzerEngine and Reporting unchanged
- [x] **DI Boundaries**: Is dependency injection confined to CLI layer only?
  - ✅ New CliApplicationBuilder will register dependencies explicitly in CLI; no DI leakage to core layers
- [x] **Rule Contracts**: If adding rules, are they stateless, deterministic, and implement `IAnalyzerRule`?
  - ✅ N/A - no rule changes in this feature
- [x] **Explicit Execution**: Does the CLI follow build → execute → exit model (no implicit background services)?
  - ✅ PRIMARY GOAL: Replaces Host.RunAsync() with explicit Build() → Execute() → Exit pattern
- [x] **Testing Discipline**: Can core logic be tested without DI or full application spin-up?
  - ✅ New CommandResult enables in-memory testing of ScanCommand without process spawning
- [x] **Determinism**: Will this feature produce consistent, reproducible results?
  - ✅ Synchronous execution eliminates async timing issues; same inputs → same CommandResult
- [x] **Extensibility**: Does this maintain stable public APIs and avoid breaking changes?
  - ⚠️ BREAKING CHANGE: Program.Main() signature changes from implicit async to explicit `int Main(string[] args)`. No external consumers (internal CLI only).

### Post-Design Check (After Phase 1)
- [x] **Layered Architecture**: Verified - All new types (CliApplicationBuilder, CliApplication, CommandResult) are in Lintelligent.Cli/Infrastructure/. No changes to AnalyzerEngine or Reporting layers.
- [x] **DI Boundaries**: Verified - ServiceCollection usage confined to CliApplicationBuilder.ConfigureServices(). No DI usage in commands beyond constructor injection.
- [x] **Rule Contracts**: Verified - N/A for this feature
- [x] **Explicit Execution**: Verified - data-model.md and quickstart.md demonstrate explicit Build() → Execute(args) → return ExitCode flow. No background services.
- [x] **Testing Discipline**: Verified - quickstart.md shows in-memory testing pattern: build app, execute, assert on CommandResult. No process spawning required.
- [x] **Determinism**: Verified - CommandResult is immutable value object. Same args → same result. Exception-to-exit-code mapping is deterministic (ArgumentException → 2, others → 1).
- [x] **Extensibility**: Verified - Contracts define stable interfaces (ICommand, IAsyncCommand). Builder pattern allows future extension via new methods (e.g., ConfigureLogging).

**GATE STATUS**: ✅ PASS - All constitutional principles satisfied. Feature ready for task breakdown (Phase 2).

*Violations MUST be documented in Complexity Tracking section with justification.*

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Lintelligent.AnalyzerEngine/    # No changes (framework-agnostic core)
│   ├── Analysis/
│   ├── Rules/
│   └── Results/
├── Lintelligent.Reporting/         # No changes (pure transformation)
│   └── ReportGenerator.cs
└── Lintelligent.Cli/               # PRIMARY CHANGE AREA
    ├── Program.cs                  # Replace Host pattern with CliApplication.Execute()
    ├── Bootstrapper.cs             # Convert to CliApplicationBuilder
    ├── Commands/
    │   └── ScanCommand.cs          # Adapt to return CommandResult instead of Task
    └── Infrastructure/             # NEW: Core execution types
        ├── CliApplicationBuilder.cs
        ├── CliApplication.cs
        └── CommandResult.cs

tests/
├── Lintelligent.AnalyzerEngine.Tests/  # No changes
└── Lintelligent.Cli.Tests/
    ├── ScanCommandTests.cs         # UPDATE: In-memory testing without process spawning
    └── CliApplicationTests.cs      # NEW: Test build → execute → exit flow
```

**Structure Decision**: Single-repo, multi-project solution (existing structure). All changes confined to `Lintelligent.Cli` project to maintain layer boundaries. New `Infrastructure/` folder added to CLI for execution model types (CliApplicationBuilder, CliApplication, CommandResult).

## Complexity Tracking

> **No constitutional violations requiring justification.** 
>
> The breaking change to Program.Main() signature is acceptable because:
> - There are no external consumers (Lintelligent.Cli is an executable, not a library)
> - This change directly implements Constitutional Principle IV (required compliance)
> - Simpler synchronous model reduces complexity rather than adding it

---

## Phase Execution Summary

### Phase 0: Outline & Research ✅ COMPLETE

**Output**: [research.md](research.md)

**Completed Tasks**:
- Researched builder pattern for CLI applications (.NET best practices)
- Designed CommandResult structure (ExitCode, Output, Error properties)
- Analyzed impact of removing Microsoft.Extensions.Hosting
- Determined async-to-sync execution strategy (GetAwaiter().GetResult())
- Defined exit code conventions (0=success, 1=error, 2=invalid args, 3+=custom)

**All NEEDS CLARIFICATION items resolved** - no blockers for Phase 1.

### Phase 1: Design & Contracts ✅ COMPLETE

**Outputs**:
- [data-model.md](data-model.md) - Entity relationships and data flow
- [contracts/CliApplicationBuilder.cs](contracts/CliApplicationBuilder.cs) - Builder API contract
- [contracts/CliApplication.cs](contracts/CliApplication.cs) - Application execution API contract
- [contracts/CommandResult.cs](contracts/CommandResult.cs) - Result value object contract
- [contracts/ICommand.cs](contracts/ICommand.cs) - Command interface contracts (ICommand, IAsyncCommand)
- [quickstart.md](quickstart.md) - Developer implementation guide

**Completed Tasks**:
- Extracted entities from spec (CliApplicationBuilder, CliApplication, CommandResult, ICommand)
- Generated API contracts with XML documentation
- Created quickstart guide with code examples and test patterns
- Updated GitHub Copilot agent context with new technologies

**Constitution Re-Check**: ✅ PASS - All principles verified post-design.

### Phase 2: Task Breakdown ⏸️ PENDING

**Next Command**: `/speckit.tasks` or `.specify/scripts/powershell/create-tasks.ps1`

**Expected Output**: [tasks.md](tasks.md) with implementation tasks

---

## Next Steps

1. **Run `/speckit.tasks`** to generate detailed task breakdown
2. **Begin implementation** following tasks.md checklist
3. **Verify tests pass** after each task group
4. **Update documentation** as implementation progresses

**Estimated Effort**: 8-12 tasks, 4-6 hours implementation + testing
