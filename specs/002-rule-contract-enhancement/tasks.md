# Tasks: Enhanced Rule Contract

**Input**: Design documents from `/specs/002-rule-contract-enhancement/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Not explicitly requested in specification - tasks focus on implementation and migration validation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create new entities and update project structure

- [ ] T001 Create Severity enum in src/Lintelligent.AnalyzerEngine/Abstractions/Severity.cs with XML documentation for each value (Error, Warning, Info)
- [ ] T002 [P] Create DiagnosticCategories static class in src/Lintelligent.AnalyzerEngine/Results/DiagnosticCategories.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core interface and entity changes that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T003 Update IAnalyzerRule interface in src/Lintelligent.AnalyzerEngine/Rules/IAnalyzerRule.cs to add Severity and Category properties
- [ ] T004 Update IAnalyzerRule.Analyze() return type from DiagnosticResult? to IEnumerable<DiagnosticResult> in src/Lintelligent.AnalyzerEngine/Rules/IAnalyzerRule.cs
- [ ] T005 Update DiagnosticResult record constructor in src/Lintelligent.AnalyzerEngine/Results/DiagnosticResult.cs to add Severity and Category parameters
- [ ] T006 Add constructor validation to DiagnosticResult in src/Lintelligent.AnalyzerEngine/Results/DiagnosticResult.cs (validate LineNumber >= 1, Severity is defined)
- [ ] T007 Update AnalyzerManager.RegisterRule() in src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerManager.cs to validate rule metadata (Id, Severity, Category) at registration time
- [ ] T008 Update AnalyzerEngine.Analyze() in src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs to handle IEnumerable<DiagnosticResult> return type and exception collection

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Severity-Based Filtering (Priority: P1) 🎯 MVP

**Goal**: Enable users to filter analysis results by severity level (Error, Warning, Info) so they can focus on critical issues first.

**Independent Test**: Run analysis with rules of different severities, filter by severity level, verify only matching diagnostics are returned.

### Implementation for User Story 1

- [ ] T009 [US1] Migrate LongMethodRule to add Severity and Category properties in src/Lintelligent.AnalyzerEngine/Rules/LongMethodRule.cs
- [ ] T010 [US1] Update LongMethodRule.Analyze() to return IEnumerable<DiagnosticResult> with yield return in src/Lintelligent.AnalyzerEngine/Rules/LongMethodRule.cs
- [ ] T011 [US1] Update LongMethodRule to find all long methods (not just first) and pass Severity/Category to DiagnosticResult constructor in src/Lintelligent.AnalyzerEngine/Rules/LongMethodRule.cs
- [ ] T012 [US1] Add --severity command-line option to ScanCommand in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T013 [US1] Implement severity filtering logic in ScanCommand.Execute() in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T014 [US1] Update ScanCommand output formatting to display severity in src/Lintelligent.Cli/Commands/ScanCommand.cs

### Validation for User Story 1

- [ ] T015 [P] [US1] Update LongMethodRuleTests in tests/Lintelligent.AnalyzerEngine.Tests/LongMethodRuleTests.cs to test multiple findings scenario
- [ ] T016 [P] [US1] Add test for empty results (no violations) returns empty enumerable (not null) in tests/Lintelligent.AnalyzerEngine.Tests/LongMethodRuleTests.cs
- [ ] T017 [P] [US1] Add test for severity and category metadata propagation in tests/Lintelligent.AnalyzerEngine.Tests/LongMethodRuleTests.cs
- [ ] T018 [P] [US1] Update ScanCommandTests in tests/Lintelligent.Cli.Tests/ScanCommandTests.cs to verify severity filtering
- [ ] T019 [US1] Add integration test for filtering by Error severity in tests/Lintelligent.Cli.Tests/ScanCommandTests.cs
- [ ] T020 [US1] Add integration test for filtering by Warning severity in tests/Lintelligent.Cli.Tests/ScanCommandTests.cs

**Checkpoint**: At this point, User Story 1 should be fully functional - users can filter by severity and see metadata in output

---

## Phase 4: User Story 2 - Multiple Findings Per File (Priority: P1)

**Goal**: Enable rules to emit multiple findings when analyzing a single file, so all violations are reported instead of stopping at the first one.

**Independent Test**: Create a rule that detects multiple violations in a single file, verify all violations are reported in the results.

### Implementation for User Story 2

- [ ] T021 [P] [US2] Create RuleContractTests in tests/Lintelligent.AnalyzerEngine.Tests/RuleContractTests.cs to test IEnumerable return type contract
- [ ] T022 [P] [US2] Add test for rule emitting zero findings returns empty enumerable in tests/Lintelligent.AnalyzerEngine.Tests/RuleContractTests.cs
- [ ] T023 [P] [US2] Add test for rule emitting multiple findings (5+) in single file in tests/Lintelligent.AnalyzerEngine.Tests/RuleContractTests.cs
- [ ] T024 [P] [US2] Add test for lazy evaluation (yield return) doesn't eagerly materialize results in tests/Lintelligent.AnalyzerEngine.Tests/RuleContractTests.cs
- [ ] T025 [US2] Update AnalyzerEngine to enumerate all findings from all rules in src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs
- [ ] T026 [US2] Add test for AnalyzerEngine aggregating findings from multiple rules in tests/Lintelligent.AnalyzerEngine.Tests/AnalyzerEngineTests.cs

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - multiple findings per file with severity filtering

---

## Phase 5: User Story 3 - Categorization for Reporting (Priority: P2)

**Goal**: Enable categorization of rules by type (Maintainability, Performance, Security, etc.) so users can group issues by category in reports.

**Independent Test**: Create rules with different categories, verify results include category metadata and can be grouped by category.

### Implementation for User Story 3

- [ ] T027 [P] [US3] Add --group-by command-line option to ScanCommand in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T028 [US3] Implement category grouping logic in ScanCommand.Execute() in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T029 [US3] Update ReportGenerator to support grouping by category in src/Lintelligent.Reporting/ReportGenerator.cs
- [ ] T030 [US3] Update ReportGenerator output formatting to display category headers in src/Lintelligent.Reporting/ReportGenerator.cs
- [ ] T031 [P] [US3] Add test for grouping by category in tests/Lintelligent.Cli.Tests/ScanCommandTests.cs
- [ ] T032 [P] [US3] Add test for category metadata display in report output in tests/Lintelligent.Cli.Tests/ScanCommandTests.cs

**Checkpoint**: All user stories should now be independently functional - severity filtering, multiple findings, and category grouping

---

## Phase 6: Exception Handling & Validation (Cross-Cutting)

**Purpose**: Implement resilience features that span multiple user stories

- [ ] T033 [P] Update AnalyzerEngine to catch rule exceptions and continue analysis in src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs
- [ ] T034 [P] Add exception collection and reporting at end of analysis in src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs
- [ ] T035 [P] Add test for AnalyzerEngine continuing on rule exception in tests/Lintelligent.AnalyzerEngine.Tests/AnalyzerEngineTests.cs
- [ ] T036 [P] Add test for AnalyzerManager rejecting rule with empty Id in tests/Lintelligent.AnalyzerEngine.Tests/AnalyzerManagerTests.cs
- [ ] T037 [P] Add test for AnalyzerManager rejecting rule with undefined Severity in tests/Lintelligent.AnalyzerEngine.Tests/AnalyzerManagerTests.cs
- [ ] T038 [P] Add test for AnalyzerManager rejecting rule with null/empty Category in tests/Lintelligent.AnalyzerEngine.Tests/AnalyzerManagerTests.cs

---

## Phase 7: Performance & Compliance Validation

**Purpose**: Verify performance targets and constitutional compliance

- [ ] T039 [P] Add performance benchmark for multiple findings vs single finding in tests/Lintelligent.AnalyzerEngine.Tests/PerformanceAndComplianceTests.cs
- [ ] T040 [P] Verify memory growth <50MB for 10K files with multiple findings in tests/Lintelligent.AnalyzerEngine.Tests/PerformanceAndComplianceTests.cs
- [ ] T041 [P] Verify throughput ≥20K files/sec (within ±10% of Feature 001 baseline) in tests/Lintelligent.AnalyzerEngine.Tests/PerformanceAndComplianceTests.cs
- [ ] T042 Run all tests and verify ≥95% code coverage for rule contract
- [ ] T043 Verify determinism (3 runs with same input produce identical results) in tests/Lintelligent.AnalyzerEngine.Tests/PerformanceAndComplianceTests.cs

---

## Phase 8: Documentation & Migration

**Purpose**: Update documentation and provide migration support

- [ ] T044 [P] Create migration guide section in README.md with quickstart.md examples
- [ ] T045 [P] Update README.md with severity filtering examples and category usage
- [ ] T046 [P] Add CHANGELOG.md entry for v2.0.0 breaking changes
- [ ] T047 [P] Update API documentation comments in IAnalyzerRule.cs with migration notes (already in contracts/)
- [ ] T048 Validate quickstart.md migration examples by running them manually
- [ ] T049 Update constitutional compliance checklist to confirm Principle III alignment

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User Story 1 - Severity-Based Filtering (P1) - can start immediately after foundational
  - User Story 2 - Multiple Findings Per File (P1) - can start in parallel with US1 after foundational
  - User Story 3 - Categorization for Reporting (P2) - can start after foundational (or wait for US1/US2)
- **Exception Handling (Phase 6)**: Depends on Foundational, can run in parallel with User Stories
- **Performance Validation (Phase 7)**: Depends on all user stories being complete
- **Documentation (Phase 8)**: Depends on all implementation being complete

### User Story Dependencies

- **User Story 1 - Severity-Based Filtering (P1)**: Depends on Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 - Multiple Findings Per File (P1)**: Depends on Foundational (Phase 2) - Can run in parallel with US1
- **User Story 3 - Categorization for Reporting (P2)**: Depends on Foundational (Phase 2) - Can run in parallel with US1/US2, but builds on severity/category metadata

### Within Each User Story

**User Story 1 - Severity-Based Filtering**:
1. Migrate LongMethodRule (T009-T011) - sequential (same file)
2. Update ScanCommand (T012-T014) - sequential (same file)
3. Update tests (T015-T020) - all [P], can run in parallel

**User Story 2 - Multiple Findings Per File**:
1. Create RuleContractTests (T021-T024) - all [P], can run in parallel
2. Update AnalyzerEngine (T025) - depends on Foundational
3. Update AnalyzerEngine tests (T026) - depends on T025

**User Story 3 - Categorization for Reporting**:
1. Update ScanCommand (T027-T028) - sequential (same file)
2. Update ReportGenerator (T029-T030) - sequential (same file)
3. Add tests (T031-T032) - [P], can run in parallel

### Parallel Opportunities

**After Foundational Phase Completes**:
- T009-T011 (LongMethodRule migration) in parallel with T021-T024 (RuleContractTests)
- T012-T014 (ScanCommand severity) in parallel with T027-T028 (ScanCommand grouping) if different branches/PRs
- T015-T020 (US1 tests) can all run in parallel (different test methods)
- T021-T024 (US2 tests) can all run in parallel (different test methods)
- T033-T038 (Exception handling & validation) can run in parallel (different files)
- T039-T041 (Performance tests) can run in parallel (different test methods)
- T044-T047 (Documentation) can all run in parallel (different files)

---

## Parallel Example: User Story 1

```bash
# After Foundational phase, these can run together:

# Parallel batch 1: Rule migration + Test setup
Task: "Migrate LongMethodRule to add Severity and Category properties"
Task: "Update LongMethodRuleTests to test multiple findings"
Task: "Add test for empty results returns empty enumerable"
Task: "Add test for severity and category metadata propagation"

# Parallel batch 2: CLI tests (after T012-T014 complete)
Task: "Update ScanCommandTests to verify severity filtering"
Task: "Add integration test for filtering by Error severity"
Task: "Add integration test for filtering by Warning severity"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only - Both P1)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003-T008) - CRITICAL
3. Complete Phase 3: User Story 1 (T009-T020)
4. Complete Phase 4: User Story 2 (T021-T026)
5. **STOP and VALIDATE**: Test severity filtering + multiple findings
6. Deploy/demo if ready (skip Phase 5 for MVP)

### Incremental Delivery

1. **Foundation**: Setup + Foundational → Core interfaces ready
2. **MVP (v2.0.0-alpha)**: US1 + US2 → Severity filtering + Multiple findings working
3. **Full Release (v2.0.0)**: + US3 → Add categorization and grouping
4. **Hardened (v2.0.1)**: + Phase 6-8 → Exception handling, performance validation, docs

### Parallel Team Strategy

With 2-3 developers (after Foundational complete):

- **Developer A**: User Story 1 - Severity-Based Filtering - T009-T020
- **Developer B**: User Story 2 - Multiple Findings Per File - T021-T026
- **Developer C**: Exception Handling (Phase 6) - T033-T038

Once stories complete:
- **All**: User Story 3 together (builds on US1/US2)
- **All**: Performance validation + Documentation

---

## Notes

- **Breaking Change**: This is a major version bump (v2.0.0) - IAnalyzerRule signature changes
- **Migration Guide**: quickstart.md provides complete before/after examples for rule developers
- **Constitutional Compliance**: All tasks align with Principle III (Rule Implementation Contract)
- **Test Coverage Target**: ≥95% for rule contract (FR requirement)
- **Performance Target**: ±10% of Feature 001 baseline (23K files/sec → ≥20K files/sec)
- **[P] markers**: Tasks that modify different files and can run in parallel
- **[Story] labels**: Map each task to user story for traceability and independent delivery
- Commit after each task or logical group for clean history
