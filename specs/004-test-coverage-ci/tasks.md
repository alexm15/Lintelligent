# Tasks: Test Coverage & CI Setup

**Input**: Design documents from `/specs/004-test-coverage-ci/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify project infrastructure and prepare for test implementation

- [X] T001 Verify coverlet.collector v6.0.4 is installed in both test projects
- [X] T002 Verify xUnit 2.9.3 and FluentAssertions 6.8.0 are installed in test projects
- [X] T003 [P] Install ReportGenerator global tool: `dotnet tool install -g dotnet-reportgenerator-globaltool`

**Checkpoint**: Dependencies verified - test implementation can begin

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Remove placeholder tests and establish baseline

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Delete placeholder file tests/Lintelligent.AnalyzerEngine.Tests/UnitTest1.cs
- [X] T005 Verify solution builds successfully: `dotnet build --configuration Release`
- [X] T006 Verify existing CLI tests run successfully: `dotnet test tests/Lintelligent.Cli.Tests/`

**Checkpoint**: Foundation clean - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Comprehensive Rule Coverage (Priority: P1) 🎯 MVP

**Goal**: Achieve 100% test coverage for all analyzer rule classes

**Independent Test**: Each rule has dedicated test file with 100% code coverage, verified via coverage report showing 100% for all rule classes

### Implementation for User Story 1

- [X] T007 [P] [US1] Create test file tests/Lintelligent.AnalyzerEngine.Tests/Rules/LongMethodRuleTests.cs
- [X] T008 [US1] Write test: Analyze_MethodExceeds20Lines_ReturnsDiagnostic in LongMethodRuleTests.cs
- [X] T009 [US1] Write test: Analyze_MethodUnder20Lines_ReturnsNoDiagnostics in LongMethodRuleTests.cs
- [X] T010 [US1] Write test: Analyze_EmptyMethodBody_ReturnsNoDiagnostics in LongMethodRuleTests.cs
- [X] T011 [US1] Write test: Analyze_NullMethodBody_ReturnsNoDiagnostics in LongMethodRuleTests.cs
- [X] T012 [US1] Write test: Analyze_BoundaryCase_Exactly20Lines_ReturnsNoDiagnostics in LongMethodRuleTests.cs
- [X] T013 [US1] Write test: Analyze_BoundaryCase_Exactly21Lines_ReturnsDiagnostic in LongMethodRuleTests.cs
- [X] T014 [US1] Run coverage for rule tests: `dotnet test tests/Lintelligent.AnalyzerEngine.Tests/ --collect:"XPlat Code Coverage"`
- [X] T015 [US1] Verify LongMethodRule has 100% coverage in coverage report

**Checkpoint**: User Story 1 complete - All analyzer rules have 100% test coverage

---

## Phase 4: User Story 2 - AnalyzerEngine Integration Testing (Priority: P1)

**Goal**: Verify AnalyzerEngine orchestrates multiple rules correctly and handles errors gracefully

**Independent Test**: AnalyzerEngine tests run in isolation and verify multi-rule, multi-file scenarios without filesystem dependencies

### Implementation for User Story 2

- [X] T016 [P] [US2] Create test file tests/Lintelligent.AnalyzerEngine.Tests/Analysis/AnalyzerEngineTests.cs
- [X] T017 [P] [US2] Create test file tests/Lintelligent.AnalyzerEngine.Tests/Analysis/AnalyzerManagerTests.cs
- [X] T018 [US2] Write test: Analyze_MultipleRulesRegistered_ExecutesAllRules in AnalyzerEngineTests.cs
- [X] T019 [US2] Write test: Analyze_OneRuleThrowsException_OtherRulesContinue in AnalyzerEngineTests.cs
- [X] T020 [US2] Write test: Analyze_MultipleFiles_ResultsAttributedCorrectly in AnalyzerEngineTests.cs
- [X] T021 [US2] Write test: Analyze_NoRulesRegistered_ReturnsEmptyResults in AnalyzerEngineTests.cs
- [X] T022 [US2] Write test: RegisterRules_ValidRules_AddsToCollection in AnalyzerManagerTests.cs
- [X] T023 [US2] Write test: GetRegisteredRules_AfterRegistration_ReturnsAllRules in AnalyzerManagerTests.cs
- [X] T024 [US2] Run integration tests: `dotnet test tests/Lintelligent.AnalyzerEngine.Tests/Analysis/`
- [X] T025 [US2] Verify all integration tests pass

**Checkpoint**: User Story 2 complete - AnalyzerEngine orchestration verified with multi-rule scenarios

---

## Phase 5: User Story 4 - Automated CI Pipeline (Priority: P1)

**Goal**: Automated build and test validation on every commit with 90% coverage enforcement

**Independent Test**: GitHub Actions workflow can be triggered manually and validates build, test passage, and coverage thresholds

### Implementation for User Story 4

- [X] T026 [P] [US4] Create directory .github/workflows/
- [X] T027 [US4] Create GitHub Actions workflow file .github/workflows/ci.yml
- [X] T028 [US4] Add workflow trigger: on push to all branches and pull requests
- [X] T029 [US4] Add job step: Checkout code using actions/checkout@v4
- [X] T030 [US4] Add job step: Setup .NET 10.0.x using actions/setup-dotnet@v4
- [X] T031 [US4] Add job step: dotnet restore
- [X] T032 [US4] Add job step: dotnet build --no-restore --configuration Release
- [X] T033 [US4] Add job step: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage" --results-directory ./coverage
- [X] T034 [US4] Add job step: Install ReportGenerator global tool
- [X] T035 [US4] Add job step: reportgenerator with --failonminimumcoverage:90
- [X] T036 [US4] Add job step: Publish coverage report as artifact (retention-days: 90, if: always())
- [X] T037 [US4] Set job timeout-minutes: 10
- [X] T038 [US4] Commit and push .github/workflows/ci.yml to trigger first CI run
- [X] T039 [US4] Verify CI workflow executes successfully on GitHub Actions
- [X] T040 [US4] Verify build passes all stages (checkout, setup, restore, build, test, coverage, publish)

**Checkpoint**: User Story 4 complete - CI pipeline validates every commit with 90% coverage enforcement

---

## Phase 6: User Story 3 - CLI Orchestration Testing (Priority: P2)

**Goal**: Verify CLI correctly wires services, handles arguments, and returns appropriate exit codes

**Independent Test**: CLI tests execute in-memory using CliApplication and verify argument parsing, service wiring, and exit codes

### Implementation for User Story 3

- [X] T041 [US3] Enhance tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs with exit code verification
- [X] T042 [US3] Write test: Execute_ValidArguments_ReturnsExitCode0 in ScanCommandTests.cs
- [X] T043 [US3] Write test: Execute_InvalidArguments_ReturnsExitCode2 in ScanCommandTests.cs
- [X] T044 [US3] Write test: Execute_CommandThrowsException_ReturnsExitCode1 in ScanCommandTests.cs
- [X] T045 [US3] Write test: Execute_NoCommandSpecified_ReturnsExitCode2 in ScanCommandTests.cs
- [X] T046 [US3] Write test: ConfigureServices_RegistersAllRequiredServices in ScanCommandTests.cs
- [X] T047 [US3] Run CLI tests: `dotnet test tests/Lintelligent.Cli.Tests/`
- [X] T048 [US3] Verify all CLI orchestration tests pass

**Checkpoint**: User Story 3 complete - CLI orchestration and exit code handling verified

---

## Phase 7: User Story 5 - Coverage Reporting & Enforcement (Priority: P2)

**Goal**: Visibility into coverage metrics and automated threshold enforcement to prevent untested code

**Independent Test**: Coverage reports generated locally show per-file, per-class, and overall coverage; threshold enforcement validates by temporarily lowering coverage

### Implementation for User Story 5

- [X] T049 [US5] Run local coverage with HTML report: `dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage`
- [X] T050 [US5] Generate HTML coverage report locally using ReportGenerator with -reporttypes:"Html;Cobertura"
- [X] T051 [US5] Open coveragereport/index.html and verify line coverage, branch coverage, method coverage visible
- [X] T052 [US5] Verify per-file coverage breakdown shows uncovered line numbers
- [X] T053 [US5] Test threshold enforcement: Run reportgenerator with --failonminimumcoverage:90 and verify exit code 0 if coverage ≥90%
- [X] T054 [US5] Test threshold failure: Temporarily comment out a test, verify coverage drops and reportgenerator returns non-zero exit code
- [X] T055 [US5] Restore commented test and verify coverage returns to ≥90%
- [X] T056 [US5] Verify CI artifact published: Check GitHub Actions artifacts for coverage-report.zip

**Checkpoint**: User Story 5 complete - Coverage reporting and enforcement fully functional locally and in CI

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and final validation

- [X] T057 [P] Verify overall test coverage meets 90% threshold: Run full coverage report
- [X] T058 [P] Verify all tests pass: `dotnet test` (should show 0 Failed)
- [X] T059 [P] Verify local test execution time <5 seconds (NFR-001)
- [X] T060 [P] Verify CI pipeline execution time <5 minutes (NFR-002 target)
- [X] T061 [P] Validate quickstart.md commands work as documented
- [X] T062 Code cleanup: Remove any commented-out code or debug statements
- [X] T063 Verify no flaky tests: Run test suite 3 times consecutively, all should pass
- [X] T064 Final validation: Push commit and verify CI passes with green build
- [X] T065 Document coverage results in PR description: Include line/branch/method percentages

**Checkpoint**: Feature 004 complete - All success criteria met, ready for code review

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - US1 (Phase 3) - P1: Can start immediately after Phase 2
  - US2 (Phase 4) - P1: Can start after Phase 2 (parallel with US1 if capacity allows)
  - US4 (Phase 5) - P1: Can start after Phase 2 (parallel with US1/US2 if capacity allows)
  - US3 (Phase 6) - P2: Can start after Phase 2 (recommended after US1/US2/US4 complete)
  - US5 (Phase 7) - P2: Can start after Phase 2 (recommended after US4 complete for CI integration)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies on other stories - Tests LongMethodRule in isolation
- **User Story 2 (P1)**: No dependencies on other stories - Tests AnalyzerEngine orchestration (uses LongMethodRule but doesn't require US1 complete)
- **User Story 3 (P2)**: No dependencies on other stories - Tests CLI orchestration only
- **User Story 4 (P1)**: No dependencies on other stories - Sets up CI infrastructure
- **User Story 5 (P2)**: Soft dependency on US4 - Coverage reporting works locally without CI, but CI integration requires US4 complete

### Within Each User Story

**US1 - Comprehensive Rule Coverage**:
- T007 (create file) before T008-T013 (write tests)
- T008-T013 (write tests) before T014 (run coverage)
- T014 (run coverage) before T015 (verify coverage)

**US2 - AnalyzerEngine Integration Testing**:
- T016-T017 (create files) can run in parallel [P]
- T018-T023 (write tests) after file creation
- T024 (run tests) before T025 (verify)

**US4 - Automated CI Pipeline**:
- T026-T027 (create workflow file) before T028-T036 (add steps)
- T028-T037 (configure workflow) before T038 (commit and push)
- T038 (push) before T039-T040 (verify CI)

**US3 - CLI Orchestration Testing**:
- T041 (enhance file) before T042-T046 (write tests)
- T042-T046 (write tests) before T047 (run tests)
- T047 (run tests) before T048 (verify)

**US5 - Coverage Reporting & Enforcement**:
- T049-T050 (generate reports) before T051-T052 (verify reports)
- T053-T055 (test threshold) after T049
- T056 (verify CI artifact) requires US4 complete

### Parallel Opportunities

**Phase 1 - Setup**:
- T001, T002, T003 can all run in parallel [P]

**Phase 2 - Foundational**:
- T004, T005, T006 should run sequentially (T005 depends on T004 complete)

**Phase 3 - US1**:
- T008-T013 (all test methods) can be written in parallel once T007 creates the file

**Phase 4 - US2**:
- T016 and T017 (create test files) can run in parallel [P]
- T018-T021 (AnalyzerEngineTests) can be written in parallel
- T022-T023 (AnalyzerManagerTests) can be written in parallel

**Phase 5 - US4**:
- T026-T037 (workflow configuration) mostly sequential (defining workflow stages)

**Phase 6 - US3**:
- T042-T046 (test methods) can be written in parallel

**Phase 7 - US5**:
- T049-T052 (report generation and verification) sequential
- T053-T055 (threshold testing) sequential

**Phase 8 - Polish**:
- T057, T058, T059, T060, T061 can all run in parallel [P]

### Cross-Story Parallelization

If team has capacity, these user stories can be worked on in parallel after Phase 2:

**Parallel Track 1** (Developer A):
- Phase 3: US1 (T007-T015) - Rule tests
- Phase 6: US3 (T041-T048) - CLI tests

**Parallel Track 2** (Developer B):
- Phase 4: US2 (T016-T025) - Integration tests
- Phase 7: US5 (T049-T056) - Coverage reporting

**Parallel Track 3** (DevOps/Developer C):
- Phase 5: US4 (T026-T040) - CI pipeline

All tracks converge at Phase 8 (Polish) for final validation.

---

## Parallel Example: User Story 1

```bash
# After T007 creates LongMethodRuleTests.cs, these can all be written in parallel:

# Terminal 1: Write normal case tests
git checkout -b us1/normal-cases
# Write T008: Analyze_MethodExceeds20Lines_ReturnsDiagnostic
# Write T009: Analyze_MethodUnder20Lines_ReturnsNoDiagnostics

# Terminal 2: Write edge case tests
git checkout -b us1/edge-cases  
# Write T010: Analyze_EmptyMethodBody_ReturnsNoDiagnostics
# Write T011: Analyze_NullMethodBody_ReturnsNoDiagnostics

# Terminal 3: Write boundary tests
git checkout -b us1/boundary-cases
# Write T012: Analyze_BoundaryCase_Exactly20Lines_ReturnsNoDiagnostics
# Write T013: Analyze_BoundaryCase_Exactly21Lines_ReturnsDiagnostic

# Merge all branches, then run T014-T015 (coverage verification)
```

---

## Implementation Strategy

### MVP Scope (Minimum Viable Product)

**Deliver User Story 1 + User Story 4 first** for immediate value:
- US1: 100% rule coverage (T007-T015)
- US4: CI pipeline with coverage enforcement (T026-T040)

**Rationale**: This delivers the core constitutional requirement (testing discipline) with automated validation on every commit.

### Incremental Delivery

1. **Sprint 1** (Phases 1-3): Setup + Foundational + US1
   - Deliverable: All rules have 100% test coverage
   - Validation: Local coverage reports show 100% for rule classes

2. **Sprint 2** (Phases 4-5): US2 + US4
   - Deliverable: Integration tests + CI pipeline
   - Validation: Green build in GitHub Actions with 90% coverage

3. **Sprint 3** (Phases 6-7): US3 + US5
   - Deliverable: CLI tests + Coverage reporting
   - Validation: All 5 user stories complete, full coverage visibility

4. **Sprint 4** (Phase 8): Polish
   - Deliverable: Feature complete, all success criteria met
   - Validation: Ready for code review and merge

---

## Success Criteria Verification (from spec.md)

| Criteria | Verification Task(s) | Expected Outcome |
|----------|---------------------|------------------|
| SC-001: 100% rules have test files | T015 | LongMethodRuleTests.cs exists, 100% coverage |
| SC-002: ≥90% overall coverage | T057 | Coverage report shows ≥90% |
| SC-003: <5s local test execution | T059 | Test suite completes in <5s |
| SC-004: <5min CI feedback | T060 | CI pipeline completes in <5min |
| SC-005: No false positives | T053, T054 | Threshold enforcement accurate |
| SC-006: Clear coverage reports | T051, T052 | HTML report shows uncovered lines |
| SC-007: 100% commits validated | T039, T040 | CI triggers on all branches |
| SC-008: Single command coverage | T049, T061 | `dotnet test --collect:"XPlat Code Coverage"` works |

---

## Total Task Count

- **Phase 1 (Setup)**: 3 tasks
- **Phase 2 (Foundational)**: 3 tasks
- **Phase 3 (US1)**: 9 tasks
- **Phase 4 (US2)**: 10 tasks
- **Phase 5 (US4)**: 15 tasks
- **Phase 6 (US3)**: 8 tasks
- **Phase 7 (US5)**: 8 tasks
- **Phase 8 (Polish)**: 9 tasks

**Total**: 65 tasks

**Estimated Effort**: 8-12 hours (based on plan.md estimate)

**Parallelization Potential**: With 3 developers, Phases 3-7 can overlap significantly, reducing calendar time to ~4-6 hours.
