# Tasks: Structured Output Formats

**Input**: Design documents from `/specs/006-structured-output-formats/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Unit tests included for all formatters and infrastructure components. Tests are part of the implementation strategy to ensure quality.

**Organization**: Tasks organized by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and dependency setup

- [X] T001 Add Microsoft.CodeAnalysis.Sarif v4.x NuGet package to src/Lintelligent.Reporting/Lintelligent.Reporting.csproj (⚠️ DEFERRED to Phase 4 - package incompatible with .NET 10.0, will use System.Text.Json for manual SARIF serialization)
- [X] T002 Create src/Lintelligent.Reporting/Formatters/ directory structure
- [X] T003 Create src/Lintelligent.Cli/Infrastructure/ directory structure
- [X] T004 Create tests/Lintelligent.Reporting.Tests/Formatters/ directory structure

**Checkpoint**: Directory structure ready for implementation

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core abstractions and infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 Create IReportFormatter interface in src/Lintelligent.Reporting/Formatters/IReportFormatter.cs
- [X] T006 Create OutputConfiguration record in src/Lintelligent.Reporting/Formatters/OutputConfiguration.cs
- [X] T007 Implement OutputWriter class in src/Lintelligent.Cli/Infrastructure/OutputWriter.cs
- [X] T008 Add --format flag parsing (json|sarif|markdown) to src/Lintelligent.Cli/Commands/ScanCommand.cs
- [X] T009 Add --output flag parsing (file path or `-` for stdout) to src/Lintelligent.Cli/Commands/ScanCommand.cs
- [X] T010 [P] Write test for OutputWriter file creation in tests/Lintelligent.Cli.Tests/Infrastructure/OutputWriterTests.cs
- [X] T011 [P] Write test for OutputWriter stdout handling (`--output -`) in tests/Lintelligent.Cli.Tests/Infrastructure/OutputWriterTests.cs
- [X] T012 [P] Write test for OutputWriter path validation in tests/Lintelligent.Cli.Tests/Infrastructure/OutputWriterTests.cs
- [X] T013 [P] Write test for OutputWriter read-only path error in tests/Lintelligent.Cli.Tests/Infrastructure/OutputWriterTests.cs
- [X] T014 [P] Write test for OutputWriter file overwrite warning in tests/Lintelligent.Cli.Tests/Infrastructure/OutputWriterTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - CI/CD Pipeline Integration with JSON Output (Priority: P1) 🎯 MVP

**Goal**: Enable DevOps engineers to integrate Lintelligent into CI/CD pipelines with machine-readable JSON output for automated quality gates and dashboards

**Independent Test**: Run `lintelligent scan /path/to/project --format json` and verify valid JSON output with all diagnostic information. Parse JSON programmatically and extract violation counts by severity.

### Implementation for User Story 1

- [ ] T015 [P] [US1] Create JsonOutputModel class for strong typing in src/Lintelligent.Reporting/Formatters/JsonOutputModel.cs
- [ ] T016 [US1] Implement JsonFormatter.Format() method using System.Text.Json in src/Lintelligent.Reporting/Formatters/JsonFormatter.cs
- [ ] T017 [US1] Generate JSON structure with status, summary (total + bySeverity), and violations array in src/Lintelligent.Reporting/Formatters/JsonFormatter.cs
- [ ] T018 [US1] Handle empty result sets (FR-014) in JsonFormatter in src/Lintelligent.Reporting/Formatters/JsonFormatter.cs
- [ ] T019 [US1] Implement special character escaping (FR-012) for quotes, newlines, Unicode in src/Lintelligent.Reporting/Formatters/JsonFormatter.cs
- [ ] T020 [P] [US1] Write test for valid JSON schema conformance in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T021 [P] [US1] Write test for 0 violations edge case in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T022 [P] [US1] Write test for 1 violation baseline in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T023 [P] [US1] Write test for 5 violations with all severity types in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T024 [P] [US1] Write test for severity filtering (Error/Warning/Info) in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T025 [P] [US1] Write test for category grouping (8 categories from Feature 019) in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T026 [P] [US1] Write test for special character escaping (quotes, newlines, Unicode) in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T027 [P] [US1] Write test for jq parsing compatibility (SC-004) in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T028 [P] [US1] Write test for PowerShell ConvertFrom-Json compatibility (SC-004) in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T029 [P] [US1] Write test for 10,000 violations performance <10 seconds (SC-008) in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T030 [P] [US1] Write test for status field values (success/error) in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T031 [P] [US1] Write test for summary counts accuracy in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T032 [P] [US1] Write test for UTF-8 encoding in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T033 [P] [US1] Write test for camelCase naming convention in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs
- [ ] T034 [P] [US1] Write test for deterministic output (same input → same output) in tests/Lintelligent.Reporting.Tests/Formatters/JsonFormatterTests.cs

**Checkpoint**: User Story 1 complete - JSON formatter fully functional and independently testable

---

## Phase 4: User Story 2 - SARIF for IDE and Security Tool Integration (Priority: P2)

**Goal**: Enable security teams and IDE developers to integrate Lintelligent with VS Code, GitHub Code Scanning, and other SARIF-consuming tools using standardized SARIF 2.1.0 output

**Independent Test**: Run `lintelligent scan --format sarif` and validate output against SARIF schema v2.1.0. Import SARIF file into VS Code and verify violations appear in Problems panel.

### Implementation for User Story 2

- [ ] T035 [US2] Implement SarifFormatter.Format() using Microsoft.CodeAnalysis.Sarif object model in src/Lintelligent.Reporting/Formatters/SarifFormatter.cs
- [ ] T036 [US2] Map DiagnosticResult → SarifResult with physicalLocation (line-only region, no columns) in src/Lintelligent.Reporting/Formatters/SarifFormatter.cs
- [ ] T037 [US2] Generate SARIF tool metadata (driver, name, version) in src/Lintelligent.Reporting/Formatters/SarifFormatter.cs
- [ ] T038 [US2] Generate rules array with all triggered rule definitions in src/Lintelligent.Reporting/Formatters/SarifFormatter.cs
- [ ] T039 [US2] Add helpUri for each rule (FR-015) pointing to rule documentation in src/Lintelligent.Reporting/Formatters/SarifFormatter.cs
- [ ] T040 [US2] Map Severity (Error/Warning/Info) to SARIF level (error/warning/note) in src/Lintelligent.Reporting/Formatters/SarifFormatter.cs
- [ ] T041 [US2] Handle empty result sets (FR-014) in SarifFormatter in src/Lintelligent.Reporting/Formatters/SarifFormatter.cs
- [ ] T042 [US2] Convert file paths to URI format (file:///C:/path/to/file.cs) in src/Lintelligent.Reporting/Formatters/SarifFormatter.cs
- [ ] T043 [P] [US2] Write test for SARIF 2.1.0 schema conformance in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T044 [P] [US2] Write test for official SARIF validator compliance (SC-002) in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T045 [P] [US2] Write test for VS Code import compatibility (SC-003) in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T046 [P] [US2] Write test for GitHub Code Scanning format in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T047 [P] [US2] Write test for 0 violations edge case in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T048 [P] [US2] Write test for single violation baseline in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T049 [P] [US2] Write test for multiple rules metadata generation in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T050 [P] [US2] Write test for helpUri presence for all rules (FR-015) in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T051 [P] [US2] Write test for physicalLocation region (line-only, no columns) in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T052 [P] [US2] Write test for file URI format (file:///) in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T053 [P] [US2] Write test for severity level mapping (Error→error, Warning→warning, Info→note) in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T054 [P] [US2] Write test for 10,000 violations performance <10 seconds (SC-008) in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T055 [P] [US2] Write test for UTF-8 encoding in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T056 [P] [US2] Write test for special character escaping in messages in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs
- [ ] T057 [P] [US2] Write test for deterministic output (same input → same output) in tests/Lintelligent.Reporting.Tests/Formatters/SarifFormatterTests.cs

**Checkpoint**: User Story 2 complete - SARIF formatter fully functional and IDE-compatible

---

## Phase 5: User Story 3 - Output to File for Report Archival (Priority: P2)

**Goal**: Enable build engineers to save analysis results to files for archival, trend tracking, and artifact storage in CI systems

**Independent Test**: Run `lintelligent scan --format json --output results.json` and verify file is created with correct content. Verify stdout remains clean (no diagnostic output mixed with JSON).

### Implementation for User Story 3

- [ ] T058 [US3] Integrate OutputWriter file writing logic in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T059 [US3] Implement stdout separation (FR-011) - progress to stdout only when --output file used in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T060 [US3] Add file overwrite warning message in src/Lintelligent.Cli/Infrastructure/OutputWriter.cs
- [ ] T061 [US3] Implement error handling for non-existent directory paths in src/Lintelligent.Cli/Infrastructure/OutputWriter.cs
- [ ] T062 [US3] Implement error handling for read-only paths in src/Lintelligent.Cli/Infrastructure/OutputWriter.cs
- [ ] T063 [P] [US3] Write integration test for JSON output to file in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T064 [P] [US3] Write integration test for SARIF output to file in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T065 [P] [US3] Write integration test for markdown output to file in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T066 [P] [US3] Write integration test for --output - (explicit stdout) in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T067 [P] [US3] Write integration test for stdout clean separation (SC-006) in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T068 [P] [US3] Write integration test for file overwrite scenario in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T069 [P] [US3] Write integration test for non-existent directory error in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T070 [P] [US3] Write integration test for read-only path error in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs

**Checkpoint**: User Story 3 complete - File output fully functional with clean stdout separation

---

## Phase 6: User Story 4 - Enhanced Markdown for Human Review (Priority: P3)

**Goal**: Improve developer experience for manual report review with enhanced markdown formatting including summary statistics, color-coded severity, and file grouping

**Independent Test**: Run `lintelligent scan --format markdown` and verify output includes summary table with counts by severity and category, followed by violations grouped by file.

### Implementation for User Story 4

- [ ] T071 [US4] Create MarkdownFormatter class implementing IReportFormatter in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T072 [US4] Extract markdown generation logic from ReportGenerator to MarkdownFormatter in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T073 [US4] Add summary statistics table (total, by severity) - SC-005 in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T074 [US4] Add summary statistics by category (8 categories) in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T075 [US4] Implement ANSI color code generation (Error=red, Warning=yellow, Info=cyan) in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T076 [US4] Implement color auto-detection using Console.IsOutputRedirected in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T077 [US4] Implement NO_COLOR environment variable support in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T078 [US4] Implement FORCE_COLOR environment variable support in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T079 [US4] Add file grouping (violations grouped by FilePath) in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T080 [US4] Support existing --group-by category flag in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs
- [ ] T081 [US4] Update ReportGenerator to delegate to MarkdownFormatter in src/Lintelligent.Reporting/ReportGenerator.cs
- [ ] T082 [P] [US4] Write test for summary table format (SC-005) in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T083 [P] [US4] Write test for summary by category in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T084 [P] [US4] Write test for ANSI color codes presence in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T085 [P] [US4] Write test for color suppression on redirect in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T086 [P] [US4] Write test for NO_COLOR environment variable in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T087 [P] [US4] Write test for FORCE_COLOR environment variable in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T088 [P] [US4] Write test for file grouping in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T089 [P] [US4] Write test for category grouping (--group-by category) in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T090 [P] [US4] Write test for empty results handling in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T091 [P] [US4] Write test for 10,000 violations performance <10 seconds (SC-008) in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T092 [P] [US4] Write test for UTF-8 encoding in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs
- [ ] T093 [P] [US4] Write test for deterministic output (same input → same output) in tests/Lintelligent.Reporting.Tests/Formatters/MarkdownFormatterTests.cs

**Checkpoint**: User Story 4 complete - Enhanced markdown with summary tables and color support

---

## Phase 7: CLI Integration & Orchestration (All User Stories)

**Purpose**: Wire all formatters into ScanCommand with dependency injection

**Dependencies**: Phases 3, 4, 5, 6 (all formatters implemented)

- [ ] T094 Register JsonFormatter in DI container in src/Lintelligent.Cli/Bootstrapper.cs
- [ ] T095 Register SarifFormatter in DI container in src/Lintelligent.Cli/Bootstrapper.cs
- [ ] T096 Register MarkdownFormatter in DI container in src/Lintelligent.Cli/Bootstrapper.cs
- [ ] T097 Register OutputWriter in DI container in src/Lintelligent.Cli/Bootstrapper.cs
- [ ] T098 Inject IEnumerable&lt;IReportFormatter&gt; into ScanCommand in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T099 Implement formatter selection logic based on --format flag in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T100 Wire OutputWriter for file/stdout output in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T101 [P] Write integration test for --format json end-to-end in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T102 [P] Write integration test for --format sarif end-to-end in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T103 [P] Write integration test for --format markdown end-to-end in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T104 [P] Write integration test for invalid --format value error in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs
- [ ] T105 [P] Write integration test for default format (markdown) in tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs

**Checkpoint**: All formatters integrated - CLI can select any format via --format flag

---

## Phase 8: Performance Validation & Edge Cases

**Purpose**: Validate performance targets and edge case handling across all user stories

**Dependencies**: Phase 7 (full integration)

- [ ] T106 Write performance test for 10K violations with JSON formatter <10s (SC-008) in tests/Lintelligent.Reporting.Tests/Performance/FormatterPerformanceTests.cs
- [ ] T107 Write performance test for 10K violations with SARIF formatter <10s (SC-008) in tests/Lintelligent.Reporting.Tests/Performance/FormatterPerformanceTests.cs
- [ ] T108 Write performance test for 10K violations with Markdown formatter <10s (SC-008) in tests/Lintelligent.Reporting.Tests/Performance/FormatterPerformanceTests.cs
- [ ] T109 [P] Write edge case test for disk full simulation in tests/Lintelligent.Cli.Tests/EdgeCases/OutputWriterEdgeCaseTests.cs
- [ ] T110 [P] Write edge case test for partial file cleanup on error in tests/Lintelligent.Cli.Tests/EdgeCases/OutputWriterEdgeCaseTests.cs
- [ ] T111 [P] Document concurrent write behavior (no special handling) in specs/006-structured-output-formats/EDGE_CASES.md
- [ ] T112 Verify all formatters handle empty results (FR-014) in tests/Lintelligent.Reporting.Tests/Formatters/EmptyResultsTests.cs
- [ ] T113 Verify fidelity across all 3 formats (SC-009) - same data in all outputs in tests/Lintelligent.Reporting.Tests/Integration/FidelityTests.cs

**Checkpoint**: Performance targets met, edge cases documented and tested

---

## Phase 9: Documentation & Examples

**Purpose**: Create comprehensive user-facing documentation

**Dependencies**: Phase 8 (feature complete)

- [ ] T114 Document --format flag usage in README.md
- [ ] T115 Document --output flag usage in README.md
- [ ] T116 Add JSON output examples to README.md
- [ ] T117 Add SARIF output examples to README.md
- [ ] T118 Add enhanced Markdown examples to README.md
- [ ] T119 Create USAGE_GUIDE.md with detailed format explanations in specs/006-structured-output-formats/USAGE_GUIDE.md
- [ ] T120 Add jq parsing examples for JSON in specs/006-structured-output-formats/USAGE_GUIDE.md
- [ ] T121 Add PowerShell parsing examples for JSON in specs/006-structured-output-formats/USAGE_GUIDE.md
- [ ] T122 Add VS Code SARIF import workflow in specs/006-structured-output-formats/USAGE_GUIDE.md
- [ ] T123 Add GitHub Actions CI/CD integration example in specs/006-structured-output-formats/USAGE_GUIDE.md
- [ ] T124 Add Azure Pipelines CI/CD integration example in specs/006-structured-output-formats/USAGE_GUIDE.md
- [ ] T125 Add GitLab CI integration example in specs/006-structured-output-formats/USAGE_GUIDE.md
- [ ] T126 Add troubleshooting section (common errors, schema validation failures) in specs/006-structured-output-formats/USAGE_GUIDE.md

**Checkpoint**: Comprehensive documentation with copy-paste ready examples

---

## Phase 10: Schema Validation & Compliance

**Purpose**: Final validation against published schemas and external tools

**Dependencies**: Phase 9 (documentation complete)

- [ ] T127 Validate JSON output against published JSON schema (SC-001) using JSON Schema validator
- [ ] T128 Validate SARIF output with official SARIF 2.1.0 validator (SC-002) from https://sarifweb.azurewebsites.net/Validation
- [ ] T129 Import SARIF into VS Code and verify Problems panel display (SC-003) - manual test
- [ ] T130 Test SARIF upload to GitHub Code Scanning in mock repository - manual test
- [ ] T131 Verify jq parsing compatibility (SC-004) with real jq commands
- [ ] T132 Verify PowerShell ConvertFrom-Json compatibility (SC-004) with real PowerShell session
- [ ] T133 Document validation results in specs/006-structured-output-formats/VALIDATION_REPORT.md

**Checkpoint**: All schema validation success criteria passing

---

## Phase 11: Release Preparation

**Purpose**: Finalize release artifacts and merge to main

**Dependencies**: Phase 10 (validation complete)

- [ ] T134 Write RELEASE_NOTES.md summarizing all 3 formats in specs/006-structured-output-formats/RELEASE_NOTES.md
- [ ] T135 Update CHANGELOG.md with feature entry (JSON, SARIF, enhanced Markdown formatters)
- [ ] T136 Verify all 170+ tests passing (existing + new 50+ tests)
- [ ] T137 Final constitution check - re-verify Phase 1 design against 7 principles
- [ ] T138 Run full test suite on CI (GitHub Actions or local)
- [ ] T139 Create PR from 006-structured-output-formats → main
- [ ] T140 Merge PR after review
- [ ] T141 Tag release (v1.1.0 - new feature, backward compatible)

**Checkpoint**: Feature merged, released, documented

---

## Dependencies & Execution Order

### Phase Dependencies

1. **Phase 1 (Setup)**: No dependencies - start immediately
2. **Phase 2 (Foundational)**: Depends on Phase 1 - **BLOCKS all user stories**
3. **Phase 3 (US1 - JSON)**: Depends on Phase 2 - can start after foundation
4. **Phase 4 (US2 - SARIF)**: Depends on Phase 2 - **PARALLEL with Phase 3**
5. **Phase 5 (US3 - File Output)**: Depends on Phase 2 - **PARALLEL with Phase 3, 4**
6. **Phase 6 (US4 - Markdown)**: Depends on Phase 2 - **PARALLEL with Phase 3, 4, 5**
7. **Phase 7 (Integration)**: Depends on Phases 3, 4, 5, 6 - all formatters ready
8. **Phase 8 (Performance)**: Depends on Phase 7 - full integration tested
9. **Phase 9 (Documentation)**: Depends on Phase 8 - feature complete
10. **Phase 10 (Validation)**: Depends on Phase 9 - docs ready
11. **Phase 11 (Release)**: Depends on Phase 10 - validation passed

### User Story Dependencies

- **US1 (JSON)**: Independent - can start after Phase 2 ✅
- **US2 (SARIF)**: Independent - can start after Phase 2 ✅
- **US3 (File Output)**: Uses US1/US2 formatters but independently testable ✅
- **US4 (Markdown)**: Independent - can start after Phase 2 ✅

### Parallelization Strategy

**After Phase 2 completes**, these can run in parallel:
- US1 tasks (T015-T034) - 20 tasks
- US2 tasks (T035-T057) - 23 tasks
- US3 tasks (T058-T070) - 13 tasks
- US4 tasks (T071-T093) - 23 tasks

**Total parallel opportunities**: 79 tasks across 4 user stories

### Within Each User Story

**US1 Parallel Opportunities** (after T016 implementation):
- All 15 unit tests (T020-T034) can run in parallel

**US2 Parallel Opportunities** (after T035 implementation):
- All 15 unit tests (T043-T057) can run in parallel

**US3 Parallel Opportunities** (after T058-T062 implementation):
- All 8 integration tests (T063-T070) can run in parallel

**US4 Parallel Opportunities** (after T071-T081 implementation):
- All 12 unit tests (T082-T093) can run in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

**Fastest path to value**: JSON output for CI/CD integration

1. ✅ Phase 0 (Research) - Complete
2. ✅ Phase 1 (Design) - Complete
3. Complete Phase 1 (Setup) - Tasks T001-T004
4. Complete Phase 2 (Foundational) - Tasks T005-T014
5. Complete Phase 3 (US1 - JSON) - Tasks T015-T034
6. **STOP and VALIDATE**: Test JSON formatter independently
7. Deploy/demo MVP with JSON output only

**Timeline**: ~20 tasks for MVP (excluding research/design already complete)

### Incremental Delivery (Recommended)

**Deliver value progressively**:

1. Complete Phases 1-2 (Setup + Foundation) → Infrastructure ready
2. Complete Phase 3 (US1) → **MVP: JSON output** → Deploy 🎯
3. Complete Phase 4 (US2) → **SARIF for IDEs** → Deploy 🎯
4. Complete Phase 5 (US3) → **File output** → Deploy 🎯
5. Complete Phase 6 (US4) → **Enhanced Markdown** → Deploy 🎯
6. Complete Phases 7-11 → Full integration, validation, release

Each deployment adds value without breaking previous functionality.

### Parallel Team Strategy

**With 4 developers**:

1. Team completes Phases 1-2 together (Setup + Foundation)
2. Once Phase 2 done:
   - **Developer A**: Phase 3 (US1 - JSON)
   - **Developer B**: Phase 4 (US2 - SARIF)
   - **Developer C**: Phase 5 (US3 - File Output)
   - **Developer D**: Phase 6 (US4 - Markdown)
3. Merge all user stories → Phase 7 (Integration)
4. Complete Phases 8-11 together

**Timeline**: ~40% faster with parallel execution

---

## Task Summary

| Phase | Purpose | Task Range | Count | Parallel? |
|-------|---------|------------|-------|-----------|
| 1 | Setup | T001-T004 | 4 | No |
| 2 | Foundation | T005-T014 | 10 | Partial (tests) |
| 3 | US1 (JSON) | T015-T034 | 20 | Yes (tests) |
| 4 | US2 (SARIF) | T035-T057 | 23 | Yes (tests) |
| 5 | US3 (File Output) | T058-T070 | 13 | Yes (tests) |
| 6 | US4 (Markdown) | T071-T093 | 23 | Yes (tests) |
| 7 | Integration | T094-T105 | 12 | Partial |
| 8 | Performance | T106-T113 | 8 | Partial |
| 9 | Documentation | T114-T126 | 13 | No |
| 10 | Validation | T127-T133 | 7 | Partial |
| 11 | Release | T134-T141 | 8 | No |
| **Total** | | **T001-T141** | **141** | **79 parallel** |

---

## Notes

- **[P]** tasks = different files, no dependencies - safe to parallelize
- **[Story]** labels (US1-US4) map tasks to user stories for traceability
- Each user story is independently testable and deliverable
- Tests are integral to quality - 50+ tests across formatters and integration
- Stop at any checkpoint to validate independently
- Commit after each logical task group
- MVP = Just Phase 1-3 (Setup + Foundation + US1) = **34 tasks**
