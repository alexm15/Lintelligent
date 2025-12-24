# Implementation Plan: Test Coverage & CI Setup

**Branch**: `004-test-coverage-ci` | **Date**: 2025-12-24 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-test-coverage-ci/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Establish comprehensive test coverage and automated CI validation to support Constitutional Principle V (Testing Discipline). Deliver 100% unit test coverage for all analyzer rules, integration tests for multi-rule orchestration, CLI tests for command-line interface boundaries, and GitHub Actions workflow with 90% coverage enforcement using Coverlet.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: xUnit 2.9.3, FluentAssertions 6.8.0, Microsoft.CodeAnalysis.CSharp.Workspaces 4.12.0, Coverlet 6.0.4  
**Storage**: N/A (static analysis tool, no persistent storage)  
**Testing**: xUnit (unit + integration), in-memory test execution, Coverlet for coverage  
**Target Platform**: GitHub Actions runners (ubuntu-latest), local developer workstations  
**Project Type**: Multi-project .NET solution (3 src projects, 2 test projects)  
**Performance Goals**: <5s local test execution, <5min CI pipeline, <10min test timeout  
**Constraints**: 90% overall coverage threshold (enforced), no test retries, immediate failure on flaky tests  
**Scale/Scope**: Currently 1 analyzer rule (LongMethodRule), aiming for 100% rule coverage + AnalyzerEngine integration tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Layered Architecture**: Does this feature respect layer boundaries? (CLI → AnalyzerEngine/Reporting → Rules)
  - ✅ Tests mirror source structure; test projects reference corresponding src projects only
- [x] **DI Boundaries**: Is dependency injection confined to CLI layer only?
  - ✅ Rule unit tests have zero DI; AnalyzerEngine tests use in-memory implementations; CLI tests may use DI for orchestration testing only
- [x] **Rule Contracts**: If adding rules, are they stateless, deterministic, and implement `IAnalyzerRule`?
  - ✅ N/A - no new rules added, testing existing rules which already implement IAnalyzerRule
- [x] **Explicit Execution**: Does the CLI follow build → execute → exit model (no implicit background services)?
  - ✅ CLI tests verify explicit execution via CliApplication.Execute(), measuring exit codes
- [x] **Testing Discipline**: Can core logic be tested without DI or full application spin-up?
  - ✅ This feature IS the testing discipline implementation - rules tested in isolation, AnalyzerEngine with in-memory syntax trees, CLI with in-memory execution
- [x] **Determinism**: Will this feature produce consistent, reproducible results?
  - ✅ Test execution is deterministic; coverage calculation is deterministic; no flaky test retries
- [x] **Extensibility**: Does this maintain stable public APIs and avoid breaking changes?
  - ✅ No public API changes; adding tests and CI infrastructure only

*No violations - this feature directly implements Constitutional Principle V.*

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
│   ├── Abstrations/
│   │   └── IAnalyzerRule.cs
│   ├── Analysis/
│   │   ├── AnalyzerEngine.cs
│   │   └── AnalyzerManager.cs
│   ├── Results/
│   │   └── DiagnosticResult.cs
│   ├── Rules/
│   │   └── LongMethodRule.cs          # Existing rule - needs tests
│   └── Utilities/
├── Lintelligent.Cli/
│   ├── Commands/
│   │   └── ScanCommand.cs
│   ├── Bootstrapper.cs
│   └── Program.cs
└── Lintelligent.Reporting/
    └── ReportGenerator.cs

tests/
├── Lintelligent.AnalyzerEngine.Tests/
│   ├── Rules/
│   │   └── LongMethodRuleTests.cs     # NEW: 100% coverage for LongMethodRule
│   ├── Analysis/
│   │   ├── AnalyzerEngineTests.cs     # NEW: Integration tests (multi-rule, multi-file)
│   │   └── AnalyzerManagerTests.cs    # NEW: Registration and orchestration tests
│   └── UnitTest1.cs                   # REMOVE: Placeholder
└── Lintelligent.Cli.Tests/
    ├── Commands/
    │   └── ScanCommandTests.cs        # ENHANCE: Add exit code and orchestration tests
    ├── TestBase.cs
    └── (existing CLI tests)

.github/
└── workflows/
    └── ci.yml                          # NEW: Build, test, coverage pipeline
```

**Structure Decision**: Standard .NET multi-project solution. Test projects mirror source structure with parallel directories. GitHub Actions workflow added at repository root under `.github/workflows/`. No additional projects needed - all testing fits within existing test projects.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*No violations identified. This feature aligns with all constitutional principles.*

---

## Phase Summary

### Phase 0: Research ✅ Complete

**Deliverables**:
- [research.md](research.md) - Comprehensive analysis of Coverlet integration, xUnit patterns, GitHub Actions CI, coverage threshold enforcement, and in-memory CLI testing

**Key Decisions**:
1. **Coverage Tool**: coverlet.collector (already installed, cross-platform)
2. **Test Patterns**: AAA pattern with FluentAssertions, mirrored directory structure
3. **CI Platform**: GitHub Actions multi-stage workflow (ubuntu-latest)
4. **Threshold Enforcement**: ReportGenerator CLI with `--failonminimumcoverage:90`
5. **CLI Testing**: In-memory execution with DI test doubles

**Research Summary**: All technical unknowns resolved. No additional dependencies required - all packages already present in test projects.

---

### Phase 1: Design ✅ Complete

**Deliverables**:
- [data-model.md](data-model.md) - Conceptual entities: TestResult, TestCoverageReport, FileCoverageReport, RuleTestSuite, IntegrationTestScenario, CIPipeline
- [contracts/ci-workflow.md](contracts/ci-workflow.md) - GitHub Actions workflow contract with 8 stages, failure propagation, logging specification
- [quickstart.md](quickstart.md) - Developer guide: quick commands, test templates, coverage reports, troubleshooting

**Architecture Validation**:
- No new runtime data models (no persistent storage)
- Tests mirror source structure (layered architecture preserved)
- CI enforces 90% coverage threshold (Constitutional Principle V)

**Agent Context Update**: ✅ Updated [.github/agents/copilot-instructions.md](.github/agents/copilot-instructions.md) with new technologies:
- Coverlet 6.0.4 (coverage tool)
- ReportGenerator (threshold enforcement)
- GitHub Actions (CI platform)

---

### Phase 2: Tasks Breakdown ⏭️ Next

**Command**: `/speckit.tasks` (not part of `/speckit.plan` workflow)

**Expected Output**: [tasks.md](tasks.md) with detailed implementation tasks:
- Task 1: Create LongMethodRuleTests.cs with 100% coverage
- Task 2: Create AnalyzerEngineTests.cs for integration scenarios
- Task 3: Create AnalyzerManagerTests.cs for rule registration
- Task 4: Enhance ScanCommandTests.cs with exit code verification
- Task 5: Create GitHub Actions workflow (.github/workflows/ci.yml)
- Task 6: Configure ReportGenerator threshold enforcement
- Task 7: Remove placeholder test files (UnitTest1.cs)
- Task 8: Validate local coverage meets 90% threshold
- Task 9: Validate CI pipeline execution

**Not Created by `/speckit.plan`**: This command ends after Phase 1 design. Use `/speckit.tasks` to generate detailed implementation tasks.

---

## Final Constitution Re-Check ✅

*Re-evaluated after Phase 1 design completion:*

- [x] **Layered Architecture**: Tests preserve layer boundaries ✅
- [x] **DI Boundaries**: Only CLI tests use DI for orchestration ✅
- [x] **Rule Contracts**: No new rules, testing existing IAnalyzerRule implementations ✅
- [x] **Explicit Execution**: CLI tests verify build → execute → exit model ✅
- [x] **Testing Discipline**: Core feature - implements Constitutional Principle V ✅
- [x] **Determinism**: No flaky test retries, deterministic coverage calculation ✅
- [x] **Extensibility**: No API changes, infrastructure only ✅

**Verdict**: All constitutional principles satisfied. No violations. Ready for implementation.

---

## Next Steps for Implementation

1. **Generate tasks**: Run `/speckit.tasks` to create detailed implementation breakdown
2. **Review artifacts**: Read [quickstart.md](quickstart.md) for developer workflow
3. **Validate plan**: Review [contracts/ci-workflow.md](contracts/ci-workflow.md) for CI expectations
4. **Begin implementation**: Follow tasks.md when created

**Branch**: `004-test-coverage-ci`  
**Spec**: [spec.md](spec.md)  
**Plan**: This file  
**Status**: Planning complete ✅ - Ready for task generation
