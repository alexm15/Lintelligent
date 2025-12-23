# Implementation Plan: Enhanced Rule Contract

**Branch**: `002-rule-contract-enhancement` | **Date**: 2025-12-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-rule-contract-enhancement/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Enhance the `IAnalyzerRule` interface to expose severity and category metadata and return multiple findings per file. This breaking change aligns the rule contract with constitutional requirements (Principle III), enabling users to filter analysis results by severity level and receive comprehensive reports when multiple violations exist in a single file.

**Technical Approach**: Modify interface signature, add Severity enum, update DiagnosticResult to accept metadata via constructor, implement validation at rule registration, and migrate LongMethodRule to demonstrate multi-finding capability.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: Microsoft.CodeAnalysis.CSharp 4.12.0 (Roslyn), xUnit 2.9.3  
**Storage**: N/A (in-memory analysis)  
**Testing**: xUnit 2.9.3 with FluentAssertions, manual performance benchmarks  
**Target Platform**: Cross-platform CLI (.NET 10.0 runtime)  
**Project Type**: Single project (multi-library solution)  
**Performance Goals**: Maintain ≥10,000 files/sec throughput, ±10% performance variance for multi-finding rules  
**Constraints**: <50MB memory growth for 10K files, deterministic results (same input → same output)  
**Scale/Scope**: 3 projects (AnalyzerEngine, Cli, Reporting), ~1-2K LOC addition, 1 existing rule migration

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Layered Architecture**: ✅ Feature modifies IAnalyzerRule interface (AnalyzerEngine layer) and LongMethodRule (Rules). No CLI/Reporting coupling introduced. Maintains layer boundaries.
- [x] **DI Boundaries**: ✅ No DI usage introduced. Rule registration remains explicit constructor-based in CLI layer only.
- [x] **Rule Contracts**: ✅ Enhancement strengthens rule contract: adds Severity/Category metadata (FR-001, FR-002), enables multiple findings (FR-003), enforces immutability (FR-009), validates metadata at registration (FR-013).
- [x] **Explicit Execution**: ✅ No changes to CLI execution model. Analysis remains synchronous build → execute → exit.
- [x] **Testing Discipline**: ✅ Rules remain unit-testable without DI. AnalyzerEngine testable with in-memory rule implementations. Enhanced contract improves testability (explicit metadata validation).
- [x] **Determinism**: ✅ Multiple findings use lazy IEnumerable (deterministic iteration order). No random/time-based logic. Metadata is immutable (FR-009).
- [x] **Extensibility**: ✅ Breaking change to IAnalyzerRule, but maintains backward compatibility for consumers of AnalyzerEngine public API (SC-006). Category uses string (not enum) for third-party extensibility.

*Violations*: None. All constitutional principles satisfied.

*Risk Assessment*: **Medium** - Breaking change to IAnalyzerRule requires migration of custom rules, but change aligns with Principle III (Rule Implementation Contract) and enables constitutional compliance (severity metadata, comprehensive findings).

---

## Post-Design Constitutional Re-Check

*Phase 1 design artifacts complete. Re-evaluating constitutional compliance.*

- [x] **Layered Architecture**: ✅ Data model and contracts maintain strict boundaries. Severity enum in Abstractions layer, DiagnosticResult in Results layer, no cross-layer violations.
- [x] **DI Boundaries**: ✅ No DI introduced. All entities instantiable via simple constructors. AnalyzerManager validation uses explicit constructor injection.
- [x] **Rule Contracts**: ✅ Enhanced contract strengthens constitutional requirements. Metadata validation (FR-013), immutability (FR-009), deterministic multi-findings (IEnumerable).
- [x] **Explicit Execution**: ✅ Validation occurs during build phase (AnalyzerManager.RegisterRule), analysis during execute phase. No background processes.
- [x] **Testing Discipline**: ✅ All entities testable without DI. Severity enum testable via unit tests, DiagnosticResult via constructor tests, rules via in-memory SyntaxTree.
- [x] **Determinism**: ✅ IEnumerable maintains iteration order (per LINQ specification). Immutable record structures prevent state mutation. Lazy evaluation preserves determinism.
- [x] **Extensibility**: ✅ Category string (not enum) allows third-party extension. Severity enum fixed for stability but versionable. Breaking change documented with migration guide.

*Post-Design Violations*: None. All design artifacts constitutional.

*Changes from Initial Check*: None - design validated initial assessment. Contracts in `/contracts/` folder demonstrate constitutional compliance.

## Project Structure

### Documentation (this feature)

```text
specs/002-rule-contract-enhancement/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── IAnalyzerRule.cs # Updated interface signature
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Lintelligent.AnalyzerEngine/
│   ├── Abstractions/
│   │   ├── IAnalyzerRule.cs           # MODIFIED: Add Severity, Category, change return type
│   │   └── Severity.cs                # NEW: Enum with Error/Warning/Info
│   ├── Analysis/
│   │   ├── AnalyzerEngine.cs          # MODIFIED: Handle IEnumerable return, exception collection
│   │   └── AnalyzerManager.cs         # MODIFIED: Validate rule metadata at registration
│   └── Results/
│       └── DiagnosticResult.cs        # MODIFIED: Constructor requires severity/category
├── Lintelligent.Cli/
│   └── Commands/
│       └── ScanCommand.cs             # MODIFIED: Display severity/category in output
└── Lintelligent.Reporting/
    └── ReportGenerator.cs             # MODIFIED: Group by severity/category

tests/
├── Lintelligent.AnalyzerEngine.Tests/
│   ├── RuleContractTests.cs           # NEW: Test metadata validation, multiple findings
│   ├── SeverityFilteringTests.cs      # NEW: Test severity-based filtering
│   └── LongMethodRuleTests.cs         # MODIFIED: Test multiple findings scenario
└── Lintelligent.Cli.Tests/
    └── ScanCommandTests.cs            # MODIFIED: Verify severity filtering in CLI
```

**Structure Decision**: Single project structure (existing). Feature modifies core interfaces in AnalyzerEngine, updates CLI command output, and adds test coverage for new contract requirements.

## Complexity Tracking

*No constitutional violations. This section is empty.*
