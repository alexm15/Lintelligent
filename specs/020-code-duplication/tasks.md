# Tasks: Code Duplication Detection

**Input**: Design documents from `/specs/020-code-duplication/`  
**Prerequisites**: ✅ plan.md, ✅ spec.md, ✅ research.md, ✅ data-model.md, ✅ contracts/

**Tests**: Test tasks included per TDD workflow (write tests → approve → fail → implement)

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **Checkbox**: Markdown checkbox for tracking completion
- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1, US2, US3, etc.) - REQUIRED for user story phases
- **File paths**: Exact paths included in descriptions

---

## Phase 1: Setup

**Purpose**: Project initialization and workspace analyzer infrastructure

- [X] T001 Create `WorkspaceAnalyzers/` directory in src/Lintelligent.AnalyzerEngine/
- [X] T002 Create `WorkspaceAnalyzers/CodeDuplication/` subdirectory for duplication detection implementation
- [X] T003 Create test directory tests/Lintelligent.AnalyzerEngine.Tests/WorkspaceAnalyzers/CodeDuplication/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core abstractions and infrastructure needed by ALL user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Create `IWorkspaceAnalyzer` interface in src/Lintelligent.AnalyzerEngine/Abstractions/IWorkspaceAnalyzer.cs
- [X] T005 Create `WorkspaceContext` class in src/Lintelligent.AnalyzerEngine/Abstractions/WorkspaceContext.cs
- [X] T006 [P] Create `DuplicationInstance` record in src/Lintelligent.AnalyzerEngine/WorkspaceAnalyzers/CodeDuplication/DuplicationInstance.cs
- [X] T007 [P] Create `DuplicationGroup` class in src/Lintelligent.AnalyzerEngine/WorkspaceAnalyzers/CodeDuplication/DuplicationGroup.cs
- [X] T008 [P] Create `TokenHasher` utility class in src/Lintelligent.AnalyzerEngine/Utilities/TokenHasher.cs (Rabin-Karp rolling hash)
- [X] T009 Create `WorkspaceAnalyzerEngine` orchestrator in src/Lintelligent.AnalyzerEngine/Analysis/WorkspaceAnalyzerEngine.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Exact Code Duplication Detection (Priority: P1) 🎯 MVP

**Goal**: Detect and report identical code blocks across multiple files using token-based hashing

**Independent Test**: Analyze solution with intentional duplications, verify all instances reported with accurate locations

### Tests for User Story 1

> **TDD Workflow**: Write these tests FIRST, ensure they FAIL before implementation

- [X] T010 [P] [US1] Test: `TokenHasher_IdenticalCode_ProducesSameHash` in tests/Lintelligent.AnalyzerEngine.Tests/WorkspaceAnalyzers/CodeDuplication/TokenHasherTests.cs
- [X] T011 [P] [US1] Test: `TokenHasher_WhitespaceOnlyDifferences_ProducesSameHash` in TokenHasherTests.cs
- [X] T012 [P] [US1] Test: `ExactDuplicationFinder_TwoIdenticalMethods_ReturnsOneDuplicationGroup` in ExactDuplicationFinderTests.cs
- [X] T013 [P] [US1] Test: `ExactDuplicationFinder_ThreeIdenticalClasses_GroupsAllThreeInstances` in ExactDuplicationFinderTests.cs
- [X] T014 [P] [US1] Test: `DuplicationDetector_TwoIdentical15LineMethods_ReportsOneDuplication` in DuplicationDetectorTests.cs

### Implementation for User Story 1

- [X] T015 [P] [US1] Implement `TokenHasher.HashTokens()` method (Rabin-Karp rolling hash algorithm)
- [X] T016 [P] [US1] Implement `TokenHasher.ExtractTokens()` method (from SyntaxTree, excluding trivia/comments)
- [X] T017 [US1] Create `ExactDuplicationFinder` class in src/Lintelligent.AnalyzerEngine/WorkspaceAnalyzers/CodeDuplication/ExactDuplicationFinder.cs
- [X] T018 [US1] Implement `ExactDuplicationFinder.FindDuplicates()` method (two-pass: hash all, then compare matches)
- [X] T019 [US1] Implement `DuplicationGroup.GetSeverityScore()` method (instances.Count × LineCount)
- [X] T020 [US1] Create `DuplicationDetector` class implementing `IWorkspaceAnalyzer` in src/Lintelligent.AnalyzerEngine/WorkspaceAnalyzers/CodeDuplication/DuplicationDetector.cs
- [X] T021 [US1] Implement `DuplicationDetector.Analyze()` method (orchestrates token extraction, hashing, duplication finding)
- [X] T022 [US1] Implement conversion from `DuplicationGroup` to `DiagnosticResult` with message formatting (e.g., "Code duplicated in 3 files: ...")

**Checkpoint**: Exact duplication detection working - can detect identical code blocks and report locations

---

## Phase 4: User Story 3 - Solution-Wide Multi-Project Analysis (Priority: P1)

**Goal**: Analyze entire solutions with multiple projects, discover cross-project duplications

**Independent Test**: Create multi-project solution with cross-project duplications, verify all discovered regardless of project boundaries

### Tests for User Story 3

- [X] T023 [P] [US3] Test: `WorkspaceContext_FiveProjects_AllProjectsIncluded` in WorkspaceAnalyzerEngineTests.cs
- [X] T024 [P] [US3] Test: `DuplicationDetector_CrossProjectDuplication_IdentifiesProjectNames` in DuplicationDetectorTests.cs
- [X] T025 [P] [US3] Test: `DuplicationDetector_ConditionalCompilation_RespectsSymbols` in DuplicationDetectorTests.cs

### Implementation for User Story 3

- [X] T026 [US3] Implement `WorkspaceContext` factory method `CreateFromSolution()` that builds ProjectsByPath dictionary
- [X] T027 [US3] Implement `WorkspaceAnalyzerEngine.Analyze()` method accepting Solution and IWorkspaceAnalyzer instances
- [X] T028 [US3] Enhance `DuplicationInstance` to include `ProjectName` property from workspace context
- [X] T029 [US3] Implement `DuplicationGroup.GetAffectedProjects()` method returning unique project names
- [X] T030 [US3] Update `ScanCommand.ExecuteAsync()` in src/Lintelligent.Cli/Commands/ScanCommand.cs to orchestrate workspace analysis after single-file rules
- [X] T031 [US3] Register `DuplicationDetector` in src/Lintelligent.Cli/Bootstrapper.cs DI container

**Checkpoint**: Multi-project analysis working - cross-project duplications detected and reported with project context

---

## Phase 5: User Story 2 - Configurable Duplication Thresholds (Priority: P2)

**Goal**: Allow developers to configure minimum line/token thresholds to filter trivial duplications

**Independent Test**: Analyze codebase with various duplication sizes, verify only blocks meeting thresholds are reported

### Tests for User Story 2

- [ ] T032 [P] [US2] Test: `DuplicationDetector_8LineDuplication_MinThreshold10_NoReport` in DuplicationDetectorTests.cs
- [ ] T033 [P] [US2] Test: `DuplicationDetector_ShortTokenDense_MinTokenThreshold_Reported` in DuplicationDetectorTests.cs
- [ ] T034 [P] [US2] Test: `ScanCommand_MinDuplicationLinesFlag_FiltersByLineCount` in tests/Lintelligent.Cli.Tests/ScanCommandTests.cs
- [ ] T035 [P] [US2] Test: `ScanCommand_CLIFlagOverridesConfig_CLITakesPrecedence` in ScanCommandTests.cs

### Implementation for User Story 2

- [ ] T036 [P] [US2] Create `DuplicationOptions` class in src/Lintelligent.AnalyzerEngine/Configuration/DuplicationOptions.cs with MinLines and MinTokens properties
- [ ] T037 [US2] Update `DuplicationDetector` constructor to accept `DuplicationOptions` parameter
- [ ] T038 [US2] Implement threshold filtering in `ExactDuplicationFinder.FindDuplicates()` method
- [ ] T039 [US2] Add `--min-duplication-lines <n>` option to ScanCommand in src/Lintelligent.Cli/Commands/ScanCommand.cs
- [ ] T040 [US2] Add `--min-duplication-tokens <n>` option to ScanCommand
- [ ] T041 [US2] Extend configuration file schema in src/Lintelligent.Cli/Configuration/ to support duplicationDetection section
- [ ] T042 [US2] Implement CLI flag precedence over config file in ScanCommand option binding

**Checkpoint**: Threshold configuration working - developers can filter duplications by size via CLI or config file

---

## Phase 6: User Story 5 - Detailed Duplication Reports (Priority: P2)

**Goal**: Comprehensive duplication reports in console, JSON, and markdown formats with grouping and severity sorting

**Independent Test**: Analyze codebase, verify all formats contain complete duplication information (locations, counts, severity)

### Tests for User Story 5

- [ ] T043 [P] [US5] Test: `ConsoleFormatter_TenDuplications_GroupedByHash` in tests/Lintelligent.Reporting.Tests/ConsoleFormatterTests.cs
- [ ] T044 [P] [US5] Test: `JsonFormatter_DuplicationResults_IncludesLocationsAndTokenCounts` in JsonFormatterTests.cs
- [ ] T045 [P] [US5] Test: `MarkdownFormatter_DuplicationResults_IncludesCodeSnippets` in MarkdownFormatterTests.cs
- [ ] T046 [P] [US5] Test: `ReportGenerator_50DuplicationGroups_SortedBySeverity` in ReportGeneratorTests.cs

### Implementation for User Story 5

- [ ] T047 [P] [US5] Update `ConsoleFormatter` in src/Lintelligent.Reporting/Formatters/ConsoleFormatter.cs to handle duplication diagnostics with grouping
- [ ] T048 [P] [US5] Update `JsonFormatter` in src/Lintelligent.Reporting/Formatters/JsonFormatter.cs to serialize duplication metadata (token counts, line ranges)
- [ ] T049 [P] [US5] Update `MarkdownFormatter` in src/Lintelligent.Reporting/Formatters/MarkdownFormatter.cs to include collapsible sections with code snippets
- [ ] T050 [US5] Implement severity-based sorting in `ReportGenerator.GenerateReport()` in src/Lintelligent.Reporting/ReportGenerator.cs
- [ ] T051 [US5] Add duplication-specific message formatting helpers (e.g., "3 instances across 2 projects")

**Checkpoint**: Reporting complete - all formats display comprehensive duplication information with proper grouping and sorting

---

## Phase 7: User Story 4 - Structural Similarity Detection (Priority: P3)

**Goal**: Detect structurally similar code with minor variations (different identifiers, reordered statements)

**Independent Test**: Create code blocks with identical structure but different names, verify similarity percentages calculated

**⚠️ NOTE**: This is P3 (lowest priority) - can be deferred to future release if time constrained

### Tests for User Story 4

- [ ] T052 [P] [US4] Test: `ASTNormalizer_IdenticalControlFlow_DifferentVariables_95PercentSimilar` in SimilarityDetectorTests.cs
- [ ] T053 [P] [US4] Test: `ASTNormalizer_ReorderedStatements_IdentifiedAsSimilar` in SimilarityDetectorTests.cs
- [ ] T054 [P] [US4] Test: `SimilarityDetector_85PercentThreshold_OnlyMeetingBlocksReported` in SimilarityDetectorTests.cs

### Implementation for User Story 4

- [ ] T055 [P] [US4] Create `ASTNormalizer` class in src/Lintelligent.AnalyzerEngine/WorkspaceAnalyzers/CodeDuplication/ASTNormalizer.cs
- [ ] T056 [US4] Implement `ASTNormalizer.NormalizeIdentifiers()` method (rename all identifiers to canonical names)
- [ ] T057 [US4] Implement `ASTNormalizer.NormalizeLiterals()` method (replace literals with type placeholders)
- [ ] T058 [US4] Create `SimilarityDetector` class in src/Lintelligent.AnalyzerEngine/WorkspaceAnalyzers/CodeDuplication/SimilarityDetector.cs
- [ ] T059 [US4] Implement `SimilarityDetector.CalculateSimilarity()` method (normalized AST comparison)
- [ ] T060 [US4] Integrate `SimilarityDetector` into `DuplicationDetector.Analyze()` workflow
- [ ] T061 [US4] Add `--enable-structural-similarity` and `--min-similarity <percent>` CLI flags to ScanCommand
- [ ] T062 [US4] Extend `DuplicationOptions` with structural similarity settings

**Checkpoint**: Structural similarity working - near-duplicates with different identifiers detected and reported

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final integration, performance optimization, and documentation

- [ ] T063 [P] Add XML documentation to all public APIs in `IWorkspaceAnalyzer`, `WorkspaceContext`, and implementation classes
- [ ] T064 [P] Create performance benchmark test in tests/Lintelligent.AnalyzerEngine.Tests/PerformanceTests.cs for 100k LOC solution (<30s requirement)
- [ ] T065 [P] Implement exclusion pattern matching for generated code files (.g.cs, .designer.cs) in `DuplicationDetector`
- [ ] T066 Verify all 219 existing tests still pass (no regression)
- [ ] T067 Update README.md with duplication detection feature examples and configuration guide
- [ ] T068 Create feature documentation in docs/features/code-duplication-detection.md with usage examples

---

## Dependencies & Execution Order

**Critical Path** (must complete in order):
1. Phase 1 (Setup) → Phase 2 (Foundational)
2. Phase 2 → Phase 3 (US1 - Exact Duplication)
3. Phase 3 → Phase 4 (US3 - Multi-Project)

**Independent User Stories** (can be implemented in parallel after Phase 2):
- User Story 2 (Thresholds) - independent of US1/US3
- User Story 5 (Reporting) - depends on US1 results existing

**Deferred** (lowest priority, can ship without):
- User Story 4 (Structural Similarity) - P3 priority, advanced feature

**Suggested MVP Scope** (P1 stories only):
- User Story 1: Exact duplication detection
- User Story 3: Multi-project analysis
- = Delivers core value proposition of workspace-level duplication detection

---

## Parallel Execution Opportunities

**Within Phase 2 (Foundational)**:
- T006, T007, T008 can run in parallel (different files)

**Within Phase 3 (User Story 1 Tests)**:
- T010, T011, T012, T013, T014 can run in parallel (independent test files)

**Within Phase 3 (User Story 1 Implementation)**:
- T015, T016 can run in parallel (different methods in TokenHasher)

**Within Phase 4 (User Story 3 Tests)**:
- T023, T024, T025 can run in parallel (independent test scenarios)

**Within Phase 5 (User Story 2)**:
- T036, T037, T038 can run in parallel (DuplicationOptions + filtering logic)
- T039, T040 can run in parallel (CLI flag additions)

**Within Phase 6 (User Story 5)**:
- T047, T048, T049 can run in parallel (different formatters)

**Within Phase 7 (User Story 4)**:
- T055, T058 can run in parallel (different classes)
- T056, T057 can run in parallel (different normalization methods)

**Within Phase 8 (Polish)**:
- T063, T064, T065, T067, T068 can run in parallel (independent documentation/optimization tasks)

---

## Implementation Strategy

**MVP First** (Phase 1-4):
- Setup + Foundational infrastructure
- User Story 1: Exact duplication detection
- User Story 3: Multi-project analysis
- **Result**: Core functionality delivering immediate value

**Incremental Delivery** (Phase 5-6):
- User Story 2: Configurable thresholds (reduces noise)
- User Story 5: Enhanced reporting (improves UX)
- **Result**: Production-ready feature with full usability

**Advanced Features** (Phase 7):
- User Story 4: Structural similarity (nice-to-have)
- **Result**: Differentiated capability for power users

**Total Tasks**: 68 tasks
- Setup: 3 tasks
- Foundational: 6 tasks
- User Story 1 (P1): 13 tasks
- User Story 3 (P1): 9 tasks
- User Story 2 (P2): 11 tasks
- User Story 5 (P2): 9 tasks
- User Story 4 (P3): 11 tasks
- Polish: 6 tasks
