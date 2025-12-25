# Implementation Plan: Solution & Project File Support

**Branch**: `009-solution-project-support` | **Date**: December 25, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-solution-project-support/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Enable Lintelligent to analyze .NET solutions and projects by parsing .sln and .csproj files using MSBuild evaluation APIs (Buildalyzer). The feature will extract compilation context (conditional symbols, file includes/excludes, target frameworks) to ensure accurate analysis that respects the actual build configuration. Results from multiple projects will be aggregated with project metadata and dependency graph information for future cross-project analysis capabilities.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0  
**Primary Dependencies**: Buildalyzer (MSBuild wrapper), Microsoft.Build (if needed for advanced scenarios), Microsoft.Extensions.Logging.Abstractions (existing)  
**Storage**: N/A (file-based input only, no persistent storage)  
**Testing**: xUnit, FluentAssertions (existing test infrastructure)  
**Target Platform**: Cross-platform (.NET 10.0 runtime, requires .NET SDK installed for MSBuild evaluation)  
**Project Type**: Single project (extends existing AnalyzerEngine and CLI)  
**Performance Goals**: Parse and evaluate 20-project solution in <10 seconds (design-time evaluation only, not full build)  
**Constraints**: Must not require actual build/restore to run (design-time evaluation), must handle missing projects gracefully  
**Scale/Scope**: Support solutions with up to 100 projects (enterprise-scale codebases)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Layered Architecture**: YES - Solution/project parsing logic belongs in AnalyzerEngine layer. CLI orchestrates, AnalyzerEngine processes. No layer violations.
- [x] **DI Boundaries**: YES - New abstractions (ISolutionProvider, IProjectProvider) will be registered in CLI composition root only. AnalyzerEngine receives via constructor injection.
- [x] **Rule Contracts**: N/A - This feature does not add new analyzer rules. Existing rules consume parsed project context.
- [x] **Explicit Execution**: YES - Solution parsing is part of build phase (load context), followed by execution (analyze), then exit. No background services.
- [x] **Testing Discipline**: YES - Solution/project parsing can be unit tested with test .sln/.csproj files in test fixtures. No full application spin-up needed.
- [x] **Determinism**: YES - Given same solution/project files and configuration flags, parsing produces identical results. MSBuild evaluation is deterministic.
- [x] **Extensibility**: YES - New ISolutionProvider/IProjectProvider interfaces maintain stable contracts. Existing ICodeProvider may become implementation detail or be extended.

*Violations MUST be documented in Complexity Tracking section with justification.*

**Constitution Check Result**: ✅ PASS - No violations. Feature aligns with all constitutional principles.

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
├── Lintelligent.AnalyzerEngine/
│   ├── Abstractions/
│   │   ├── ISolutionProvider.cs          # NEW: Abstraction for solution parsing
│   │   ├── IProjectProvider.cs           # NEW: Abstraction for project evaluation
│   │   └── ICodeProvider.cs              # EXISTING: May be extended or become lower-level primitive
│   ├── ProjectModel/                     # NEW: Domain models for solutions/projects
│   │   ├── Solution.cs                   # Represents parsed .sln file
│   │   ├── Project.cs                    # Represents evaluated .csproj with metadata
│   │   ├── CompileItem.cs                # Source file with inclusion metadata
│   │   ├── ProjectReference.cs           # Project-to-project dependency
│   │   └── TargetFramework.cs            # Target framework moniker wrapper
│   └── Analysis/
│       └── AnalyzerEngine.cs             # MODIFIED: Accept ISolutionProvider/IProjectProvider
├── Lintelligent.Cli/
│   ├── Commands/
│   │   └── ScanCommand.cs                # MODIFIED: Accept .sln paths, handle solution input
│   ├── Providers/                        # NEW: Concrete provider implementations
│   │   ├── BuildalyzerSolutionProvider.cs    # Buildalyzer-based solution parser
│   │   └── BuildalyzerProjectProvider.cs     # Buildalyzer-based project evaluator
│   └── Bootstrapper.cs                   # MODIFIED: Register new providers in DI
└── Lintelligent.Reporting/
    └── ReportGenerator.cs                # MODIFIED: Include project metadata in reports

tests/
├── Lintelligent.AnalyzerEngine.Tests/
│   ├── ProjectModel/                     # NEW: Unit tests for domain models
│   │   ├── SolutionTests.cs
│   │   └── ProjectTests.cs
│   └── Fixtures/                         # NEW: Test .sln/.csproj files
│       ├── TestSolution.sln
│       ├── ProjectA/ProjectA.csproj
│       └── ProjectB/ProjectB.csproj
└── Lintelligent.Cli.Tests/
    ├── Commands/
    │   └── ScanCommandSolutionTests.cs   # NEW: Integration tests for .sln input
    └── Providers/                        # NEW: Provider implementation tests
        ├── BuildalyzerSolutionProviderTests.cs
        └── BuildalyzerProjectProviderTests.cs
```

**Structure Decision**: Single project structure (Option 1). This feature extends existing AnalyzerEngine and CLI projects rather than creating new projects. New abstractions and models are added to AnalyzerEngine (framework-agnostic), while Buildalyzer-specific implementations live in CLI (composition root).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No Complexity Violations**: This feature introduces no constitutional violations. The complexity tracking table is empty.

## Post-Design Constitution Re-Check

*Phase 1 design is complete. Re-evaluating constitutional compliance with concrete design decisions.*

### Layered Architecture ✅

**Pre-Design Assessment**: Solution/project parsing belongs in AnalyzerEngine layer

**Post-Design Validation**:
- ✅ Domain models (Solution, Project, CompileItem, ProjectReference, TargetFramework) defined in `Lintelligent.AnalyzerEngine/ProjectModel/`
- ✅ Abstractions (ISolutionProvider, IProjectProvider) defined in `Lintelligent.AnalyzerEngine/Abstractions/`
- ✅ Implementations (BuildalyzerSolutionProvider, BuildalyzerProjectProvider) in `Lintelligent.Cli/Providers/`
- ✅ CLI orchestrates via ScanCommand accepting .sln paths
- ✅ No layer boundary violations

**Conclusion**: PASS - Design respects layered architecture perfectly

### DI Boundaries ✅

**Pre-Design Assessment**: DI registration confined to CLI composition root

**Post-Design Validation**:
- ✅ ISolutionProvider and IProjectProvider registered in Bootstrapper.cs (CLI)
- ✅ AnalyzerEngine receives providers via constructor injection (explicit dependencies)
- ✅ Domain models have no DI dependencies (plain C# classes)
- ✅ No DI usage in AnalyzerEngine layer (only in CLI)

**Conclusion**: PASS - DI properly confined to composition root

### Rule Contracts ✅

**Pre-Design Assessment**: Feature does not add analyzer rules

**Post-Design Validation**:
- ✅ No new IAnalyzerRule implementations
- ✅ Existing rules consume Project.ConditionalSymbols via enhanced ICodeProvider
- ✅ Rules remain stateless and deterministic
- ✅ Project metadata available for future cross-project rules (but not required now)

**Conclusion**: PASS - No rule contract changes, existing rules unaffected

### Explicit Execution ✅

**Pre-Design Assessment**: Solution parsing is part of build phase

**Post-Design Validation**:
- ✅ Solution parsing happens during CLI build phase (before analysis execution)
- ✅ Project evaluation is synchronous design-time evaluation (no background tasks)
- ✅ Analysis execution follows existing build → execute → exit model
- ✅ No long-running services, no implicit async workflows

**Conclusion**: PASS - Explicit execution model maintained

### Testing Discipline ✅

**Pre-Design Assessment**: Core logic testable without DI

**Post-Design Validation**:
- ✅ Domain models are plain C# classes (testable with simple constructors)
- ✅ ISolutionProvider/IProjectProvider can be unit tested with test .sln/.csproj fixtures
- ✅ BuildalyzerSolutionProvider/BuildalyzerProjectProvider can be integration tested independently
- ✅ No dependency on full application spin-up for core logic tests
- ✅ CLI integration tests use DI, but only test orchestration (not business logic)

**Test Strategy**:
- Unit: Domain model validation, entity relationships
- Integration: Provider implementations with real .sln/.csproj files in fixtures
- End-to-end: ScanCommand with solution input, verify aggregated output

**Conclusion**: PASS - Core logic fully testable in isolation

### Determinism ✅

**Pre-Design Assessment**: Same input produces same output

**Post-Design Validation**:
- ✅ Solution parsing is deterministic (text-based .sln format)
- ✅ MSBuild evaluation is deterministic (given same SDK, configuration, project files)
- ✅ No random seeding, no time-based logic, no hardware dependencies
- ✅ Buildalyzer produces consistent AnalyzerResult for same inputs
- ✅ Domain models are immutable (no mutable state between runs)
- ✅ Default target framework selection is alphabetically first (consistent)

**Determinism Guarantees**:
- Same .sln + same configuration → same project list
- Same .csproj + same configuration + same TFM → same source files, same symbols
- Same source files + same symbols → same analysis results

**Conclusion**: PASS - Fully deterministic analysis

### Extensibility ✅

**Pre-Design Assessment**: Stable public APIs for future extensibility

**Post-Design Validation**:
- ✅ ISolutionProvider and IProjectProvider are stable interfaces
- ✅ Domain models expose IReadOnly* collections (immutable, safe to extend)
- ✅ TargetFramework implements IEquatable (proper value semantics)
- ✅ Solution.GetDependencyGraph() deferred computation (extend without breaking changes)
- ✅ CompileItem.InclusionType enum extensible (can add new types without breaking)
- ✅ Future cross-project rules can consume dependency graph without interface changes

**Backward Compatibility**:
- Adding new properties to domain models: Safe (default values, optional)
- Adding new methods to interfaces: Breaking (use extension methods or new interfaces)
- Adding new enum values: Safe (existing code handles unknown values gracefully)

**Future Extension Points**:
- IDependencyAnalyzerRule interface (cross-project rules)
- IProjectFilter interface (exclude projects from analysis)
- Custom MSBuild property extraction (beyond current metadata)

**Conclusion**: PASS - Design supports future extensibility without breaking changes

## Final Constitution Check Result

**Status**: ✅ **ALL PRINCIPLES SATISFIED**

**Summary**:
- 7 constitutional principles evaluated
- 7 principles passed (100% compliance)
- 0 violations requiring justification
- 0 complexity exceptions needed

**Readiness**: Feature design is constitutionally compliant and ready for implementation.

## Implementation Phases (Reference)

This plan document fulfills Phase 0 (Research) and Phase 1 (Design). The following phases are defined for reference but executed separately:

**Phase 0**: ✅ COMPLETE - Research decisions documented in [research.md](research.md)
**Phase 1**: ✅ COMPLETE - Data model in [data-model.md](data-model.md), contracts in [contracts/](contracts/), quickstart in [quickstart.md](quickstart.md)
**Phase 2**: ⏳ PENDING - Tasks generation via `/speckit.tasks` command (creates tasks.md with implementation checklist)
**Phase 3**: ⏳ PENDING - Implementation (execute tasks from tasks.md)
**Phase 4**: ⏳ PENDING - Validation (verify all acceptance criteria met)
