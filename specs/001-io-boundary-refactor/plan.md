# Implementation Plan: Refactor AnalyzerEngine IO Boundary

**Branch**: `001-io-boundary-refactor` | **Date**: 2025-12-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-io-boundary-refactor/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Refactor AnalyzerEngine to eliminate direct file system IO operations, introducing an ICodeProvider abstraction that enables in-memory testing, IDE integration, and alternative frontends. This resolves Constitution Principle I violation where the AnalyzerEngine core was coupled to file system operations, preventing proper unit testing and limiting extensibility. The refactor maintains 100% backward compatibility with existing CLI functionality while enabling the engine to accept syntax trees from any source (files, memory, IDE buffers, network).

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: Microsoft.CodeAnalysis.CSharp 4.12.0 (Roslyn APIs), Microsoft.Extensions.DependencyInjection 10.0.1 (CLI layer only)  
**Storage**: N/A (stateless analysis, results output to stdout/files)  
**Testing**: xUnit (existing test framework in Lintelligent.AnalyzerEngine.Tests)  
**Target Platform**: Cross-platform CLI (.NET 10.0 runtime - Windows, Linux, macOS)
**Project Type**: Single project structure (src/, tests/ at repository root)  
**Performance Goals**: Process 10,000+ C# files without memory exhaustion, maintain current analysis speed ±5%  
**Constraints**: Zero file IO operations in AnalyzerEngine project, DI usage restricted to CLI layer only, streaming processing (yield pattern) required for large codebases  
**Scale/Scope**: Refactor affects AnalyzerEngine (core) and CLI (composition root), no changes to Rules or Reporting layers; supports codebases up to 100k+ files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Layered Architecture**: Yes - Refactor ENFORCES layer boundaries by removing IO from AnalyzerEngine (core) and moving it to FileSystemCodeProvider in CLI layer
- [x] **DI Boundaries**: Yes - ICodeProvider abstraction lives in AnalyzerEngine but implementation (FileSystemCodeProvider) resides in CLI where DI is permitted
- [x] **Rule Contracts**: N/A - This refactor doesn't add or modify rules, only changes how AnalyzerEngine receives code
- [x] **Explicit Execution**: Yes - No changes to CLI execution model; refactor maintains build → execute → exit flow
- [x] **Testing Discipline**: Yes - PRIMARY GOAL of refactor is to enable AnalyzerEngine testing without DI or file system dependencies
- [x] **Determinism**: Yes - Same syntax trees yield identical results regardless of ICodeProvider implementation (validated in SC-007)
- [x] **Extensibility**: Yes - ICodeProvider abstraction enables future IDE plugins, alternative frontends without breaking changes to AnalyzerEngine

*Violations MUST be documented in Complexity Tracking section with justification.*

**Gate Status**: ✅ **PASSED** - This refactor actively resolves existing constitutional violation (Principle I) and strengthens compliance across all principles.

## Project Structure

### Documentation (this feature)

```text
specs/001-io-boundary-refactor/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── ICodeProvider.cs # Interface contract for code discovery abstraction
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Lintelligent.AnalyzerEngine/     # Core analysis engine (NO IO after refactor)
│   ├── Analysis/
│   │   ├── AnalyzerEngine.cs        # REFACTORED: Accepts IEnumerable<SyntaxTree> instead of path
│   │   └── AnalyzerManager.cs       # Unchanged
│   ├── Abstrations/                 # NEW: ICodeProvider interface added here
│   │   └── ICodeProvider.cs         # NEW: Abstraction for code discovery
│   ├── Results/
│   │   └── DiagnosticResult.cs      # Unchanged
│   └── Rules/
│       ├── IAnalyzerRule.cs         # Unchanged
│       └── LongMethodRule.cs        # Unchanged
│
├── Lintelligent.Cli/                # Composition root (DI allowed here)
│   ├── Commands/
│   │   └── ScanCommand.cs           # REFACTORED: Uses FileSystemCodeProvider
│   ├── Providers/                   # NEW: Code provider implementations
│   │   └── FileSystemCodeProvider.cs # NEW: IO operations moved here from AnalyzerEngine
│   ├── Bootstrapper.cs              # Updated to register ICodeProvider
│   └── Program.cs                   # Unchanged
│
└── Lintelligent.Reporting/          # Unchanged by this refactor
    └── ReportGenerator.cs

tests/
├── Lintelligent.AnalyzerEngine.Tests/
│   ├── Analysis/
│   │   └── AnalyzerEngineTests.cs   # NEW: In-memory tests without file system
│   └── UnitTest1.cs                 # Existing (may be refactored/removed)
│
└── Lintelligent.Cli.Tests/
    ├── Providers/
    │   └── FileSystemCodeProviderTests.cs  # NEW: Tests for file discovery
    └── ScanCommandTests.cs          # UPDATED: Integration tests for CLI

```

**Structure Decision**: Single project structure (Option 1) is used as Lintelligent is a CLI tool with library components. The refactor adds the `Abstrations/` folder to AnalyzerEngine for the ICodeProvider interface and `Providers/` folder to CLI for FileSystemCodeProvider implementation. This maintains the existing layered architecture while enforcing the IO boundary separation required by Constitution Principle I.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations** - This refactor resolves an existing constitutional violation rather than introducing new ones. All checks passed.

---

## Phase 1 Design Review: Constitution Re-Check

*Re-evaluating constitutional compliance after design decisions*

- [x] **Layered Architecture**: ✅ STRENGTHENED - ICodeProvider abstraction lives in AnalyzerEngine, implementations in CLI. Zero IO in core.
- [x] **DI Boundaries**: ✅ MAINTAINED - FileSystemCodeProvider instantiated in CLI, not injected into AnalyzerEngine.
- [x] **Rule Contracts**: ✅ N/A - No rule changes.
- [x] **Explicit Execution**: ✅ MAINTAINED - No changes to CLI execution model.
- [x] **Testing Discipline**: ✅ ENHANCED - In-memory tests demonstrated in quickstart.md, no DI required.
- [x] **Determinism**: ✅ VALIDATED - Research.md confirms same trees → same results, streaming preserves determinism.
- [x] **Extensibility**: ✅ PROVEN - Data-model.md documents 4 future ICodeProvider implementations without breaking changes.

**Final Gate Status**: ✅ **PASSED** - Design maintains and strengthens constitutional compliance.

---

## Implementation Readiness

**Phase 0 Complete**: ✅ Research findings documented (8 decisions, 0 unknowns remaining)  
**Phase 1 Complete**: ✅ Design artifacts generated (data-model.md, contracts/, quickstart.md)  
**Agent Context**: ✅ Updated with C# .NET 10.0, Roslyn APIs  
**Constitutional Review**: ✅ All principles validated post-design

**Next Step**: Run `/speckit.tasks` to generate task breakdown for implementation.

---

## Artifacts Generated

- **[research.md](research.md)** - 8 research questions resolved, key decisions documented
- **[data-model.md](data-model.md)** - Entities, relationships, contracts, validation rules
- **[contracts/ICodeProvider.cs](contracts/ICodeProvider.cs)** - Interface contract with documentation
- **[quickstart.md](quickstart.md)** - Developer implementation guide with examples
- **[plan.md](plan.md)** - This file (implementation plan)

**Branch**: `001-io-boundary-refactor` (created)  
**Feature Number**: 001  
**Status**: Ready for task breakdown and implementation
