# Tasks: Refactor AnalyzerEngine IO Boundary

**Feature**: 001-io-boundary-refactor  
**Input**: Design documents from `/specs/001-io-boundary-refactor/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ICodeProvider.cs

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `- [ ] [ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and structural changes

- [X] T001 Create `src/Lintelligent.AnalyzerEngine/Abstractions/` directory for ICodeProvider interface
- [X] T002 Create `src/Lintelligent.Cli/Providers/` directory for concrete provider implementations

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core abstractions that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Create ICodeProvider interface in `src/Lintelligent.AnalyzerEngine/Abstractions/ICodeProvider.cs` with GetSyntaxTrees() method returning IEnumerable<SyntaxTree>
- [X] T004 Add XML documentation to ICodeProvider specifying contract requirements (valid trees, error handling, lazy evaluation)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - In-Memory Testing of Analysis Rules (Priority: P1) 🎯 MVP

**Goal**: Enable AnalyzerEngine to be tested with in-memory syntax trees without file system dependencies, resolving Constitution Principle I violation.

**Independent Test**: Create in-memory syntax trees, pass to refactored AnalyzerEngine.Analyze(), verify results without any file system operations.

### Characterization Tests (Preserve Existing Behavior)

> **NOTE: Write these tests FIRST to establish baseline before refactoring**

- [ ] T005 [P] [US1] Create characterization test suite in `tests/Lintelligent.AnalyzerEngine.Tests/CharacterizationTests.cs` capturing current Analyze() behavior with real files
- [ ] T006 [P] [US1] Document current Analyze() signature and behavior for regression validation

### Implementation for User Story 1

- [X] T007 [US1] Refactor AnalyzerEngine.Analyze() signature from `Analyze(string projectPath)` to `Analyze(IEnumerable<SyntaxTree> syntaxTrees)` in `src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs`
- [X] T008 [US1] Remove all file system IO operations from AnalyzerEngine.Analyze() (Directory.GetFiles, File.ReadAllText, CSharpSyntaxTree parsing)
- [X] T009 [US1] Implement streaming analysis logic: foreach syntax tree → manager.Analyze(tree) → yield results in `src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs`
- [X] T010 [US1] Update AnalyzerEngine constructor to remove any file path or IO configuration parameters in `src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs`

### Unit Tests for User Story 1

> **NOTE: Write these AFTER refactoring to validate in-memory capability**

- [X] T011 [P] [US1] Create in-memory unit test: parse in-memory syntax tree, pass to Analyze(), verify diagnostics without file system in `tests/Lintelligent.AnalyzerEngine.Tests/Analysis/AnalyzerEngineTests.cs`
- [X] T012 [P] [US1] Create empty collection test: pass empty IEnumerable<SyntaxTree>, verify empty results in `tests/Lintelligent.AnalyzerEngine.Tests/Analysis/AnalyzerEngineTests.cs`
- [X] T013 [P] [US1] Create determinism test: same in-memory trees yield identical results regardless of source in `tests/Lintelligent.AnalyzerEngine.Tests/Analysis/AnalyzerEngineTests.cs`
- [X] T014 [P] [US1] Create rule violation test: in-memory trees with known violations, verify diagnostics match expectations in `tests/Lintelligent.AnalyzerEngine.Tests/Analysis/AnalyzerEngineTests.cs`

**Checkpoint**: At this point, AnalyzerEngine has zero file system dependencies and can be fully unit tested with in-memory syntax trees. Constitution Principle I violation is resolved.

---

## Phase 4: User Story 2 - File System Code Provider for CLI (Priority: P2)

**Goal**: Restore CLI functionality by implementing FileSystemCodeProvider that discovers and analyzes C# files from disk.

**Independent Test**: Point CLI at directory with .cs files, verify all files discovered and analyzed with expected diagnostic output.

### Implementation for User Story 2

- [X] T015 [P] [US2] Create FileSystemCodeProvider class in `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs` implementing ICodeProvider
- [X] T016 [US2] Implement constructor accepting string rootPath parameter with validation (ArgumentException for null/empty) in `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`
- [X] T017 [US2] Implement GetSyntaxTrees() with file discovery logic: detect directory vs file path in `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`
- [X] T018 [US2] Add directory enumeration: Directory.EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories) for lazy evaluation in `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`
- [X] T019 [US2] Add single file handling: return single SyntaxTree if rootPath is file in `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`
- [X] T020 [US2] Implement file reading and parsing: File.ReadAllText → CSharpSyntaxTree.ParseText with file path in `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`
- [X] T021 [US2] Add error handling: try-catch for FileNotFoundException, UnauthorizedAccessException, PathTooLongException - log and skip problematic files in `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`
- [X] T022 [US2] Add yield return for each successfully parsed SyntaxTree in `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`
- [X] T023 [US2] Refactor ScanCommand to instantiate FileSystemCodeProvider, call GetSyntaxTrees(), pass to AnalyzerEngine.Analyze() in `src/Lintelligent.Cli/Commands/ScanCommand.cs`
- [X] T024 [US2] Update ScanCommand to remove direct file system operations (moved to provider) in `src/Lintelligent.Cli/Commands/ScanCommand.cs`

### Unit Tests for User Story 2

- [X] T025 [P] [US2] Create FileSystemCodeProvider directory test: verify recursive .cs file discovery in `tests/Lintelligent.Cli.Tests/Providers/FileSystemCodeProviderTests.cs`
- [X] T026 [P] [US2] Create FileSystemCodeProvider single file test: verify single SyntaxTree returned for file path in `tests/Lintelligent.Cli.Tests/Providers/FileSystemCodeProviderTests.cs`
- [X] T027 [P] [US2] Create FileSystemCodeProvider empty directory test: verify empty collection for directory without .cs files in `tests/Lintelligent.Cli.Tests/Providers/FileSystemCodeProviderTests.cs`
- [X] T028 [P] [US2] Create FileSystemCodeProvider mixed files test: verify only .cs files processed, others ignored in `tests/Lintelligent.Cli.Tests/Providers/FileSystemCodeProviderTests.cs`
- [X] T029 [P] [US2] Create FileSystemCodeProvider error handling test: verify FileNotFoundException logged and skipped in `tests/Lintelligent.Cli.Tests/Providers/FileSystemCodeProviderTests.cs`

### Integration Tests for User Story 2

- [X] T030 [US2] Create CLI integration test: scan real project directory, verify all .cs files analyzed in `tests/Lintelligent.Cli.Tests/ScanCommandTests.cs`
- [X] T031 [US2] Create CLI integration test: verify existing CLI behavior preserved (backward compatibility) in `tests/Lintelligent.Cli.Tests/ScanCommandTests.cs`
- [X] T032 [US2] Create CLI integration test: scan large codebase (1000+ files), verify no memory exhaustion in `tests/Lintelligent.Cli.Tests/ScanCommandTests.cs`

**Checkpoint**: At this point, CLI functionality is fully restored with FileSystemCodeProvider. Users can scan projects from command line exactly as before, but core engine is now decoupled from IO.

---

## Phase 5: User Story 3 - Alternative Code Providers for IDE Integration (Priority: P3)

**Goal**: Prove extensibility by implementing alternative ICodeProvider for IDE integration scenarios (in-memory editor buffers).

**Independent Test**: Create mock IDE code provider yielding in-memory editor buffers, verify AnalyzerEngine processes them identically to file-based trees.

### Implementation for User Story 3

- [ ] T033 [P] [US3] Create InMemoryCodeProvider class in `tests/Lintelligent.AnalyzerEngine.Tests/TestUtilities/InMemoryCodeProvider.cs` implementing ICodeProvider (for testing/demonstration)
- [ ] T034 [US3] Implement InMemoryCodeProvider constructor accepting Dictionary<string, string> (filePath → sourceCode) in `tests/Lintelligent.AnalyzerEngine.Tests/TestUtilities/InMemoryCodeProvider.cs`
- [ ] T035 [US3] Implement InMemoryCodeProvider.GetSyntaxTrees() yielding CSharpSyntaxTree.ParseText(source, path: filePath) for each entry in `tests/Lintelligent.AnalyzerEngine.Tests/TestUtilities/InMemoryCodeProvider.cs`
- [ ] T036 [P] [US3] Create FilteringCodeProvider class in `tests/Lintelligent.AnalyzerEngine.Tests/TestUtilities/FilteringCodeProvider.cs` demonstrating predicate-based filtering
- [ ] T037 [US3] Implement FilteringCodeProvider wrapping another ICodeProvider with filter predicate (e.g., modified files only) in `tests/Lintelligent.AnalyzerEngine.Tests/TestUtilities/FilteringCodeProvider.cs`

### Integration Tests for User Story 3

- [ ] T038 [P] [US3] Create InMemoryCodeProvider test: verify analysis results reflect in-memory content, not saved file content in `tests/Lintelligent.AnalyzerEngine.Tests/InMemoryCodeProviderTests.cs`
- [ ] T039 [P] [US3] Create FilteringCodeProvider test: verify only filtered files analyzed in `tests/Lintelligent.AnalyzerEngine.Tests/FilteringCodeProviderTests.cs`
- [ ] T040 [P] [US3] Create provider swapping test: different ICodeProvider implementations yield consistent AnalyzerEngine behavior in `tests/Lintelligent.AnalyzerEngine.Tests/CodeProviderIntegrationTests.cs`

**Checkpoint**: At this point, extensibility is proven with multiple ICodeProvider implementations. Future IDE plugins can implement ICodeProvider to analyze unsaved editor buffers.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, performance validation, and final quality checks

- [ ] T041 [P] Add XML documentation to AnalyzerEngine.Analyze() explaining new signature and usage in `src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs`
- [ ] T042 [P] Add XML documentation to FileSystemCodeProvider class in `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`
- [ ] T043 [P] Update README or project documentation with ICodeProvider usage examples and migration guide
- [ ] T044 Verify all existing integration tests pass with refactored architecture (characterization tests → regression validation)
- [ ] T045 Measure and validate code coverage ≥90% for AnalyzerEngine core (SC-003)
- [ ] T046 Performance benchmark: analyze 10,000+ file project, verify no memory exhaustion and ±5% execution time (SC-002, SC-004)
- [ ] T047 Validate Constitution Principle I compliance: zero System.IO dependencies in AnalyzerEngine project references (SC-006)
- [ ] T048 Final constitution check: verify all 7 principles pass with refactored architecture

---

## Implementation Strategy

### MVP Scope (Minimum Viable Product)

**Deliver User Story 1 FIRST** as the minimal constitutional compliance fix:
- Tasks T001-T014 only
- Proves AnalyzerEngine can be tested without file system
- Resolves Constitution Principle I violation
- Enables TDD for future rule development

**Checkpoint**: After US1, you have a working in-memory analyzer that satisfies the constitutional requirement, even though CLI functionality is temporarily broken.

### Incremental Delivery

1. **Phase 1-2 + US1 (T001-T014)**: Core refactor + in-memory testing → MVP ✅
2. **Phase 4 US2 (T015-T032)**: Restore CLI functionality → Feature complete for current users ✅
3. **Phase 5 US3 (T033-T040)**: Prove extensibility → Ready for IDE integration ✅
4. **Phase 6 (T041-T048)**: Polish → Production ready ✅

### Dependencies

#### User Story Dependencies
- **US1**: No dependencies (can implement immediately after Phase 2)
- **US2**: Depends on US1 completion (needs refactored AnalyzerEngine signature)
- **US3**: Depends on US1 completion (uses refactored architecture, independent of US2)

#### Phase Dependencies
- Phase 3 (US1) BLOCKS Phase 4 (US2) - cannot implement FileSystemCodeProvider until AnalyzerEngine signature is refactored
- Phase 5 (US3) can proceed in parallel with Phase 4 once Phase 3 is complete

#### Task Dependencies Within User Stories
- **US1**: T007 → T008 → T009 (signature change → remove IO → implement streaming)
- **US2**: T015 → T016 → T017 → T018-T022 (class creation → constructor → GetSyntaxTrees → implementation details)
- **US2**: T023 depends on T015-T022 complete (ScanCommand needs working FileSystemCodeProvider)

### Parallel Execution Opportunities

#### Within User Story 1 (after T010 complete):
- T011, T012, T013, T014 can all be written in parallel (different test scenarios)

#### Within User Story 2:
- After T015-T022 complete: T025-T029 (unit tests) can run in parallel
- After T023-T024 complete: T030-T032 (integration tests) can run in parallel

#### Within User Story 3:
- T033-T035, T036-T037 can run in parallel (two different test providers)
- T038, T039, T040 can run in parallel after their respective implementations

#### Cross-Story Parallelization:
- Once US1 complete (T014): US2 (T015-T032) and US3 (T033-T040) can proceed in parallel
- Phase 6 polish tasks (T041-T043) can run in parallel

### Validation Checkpoints

- **After Phase 2**: ICodeProvider interface compiles and has clear contract documentation
- **After Phase 3 (US1)**: All characterization tests pass, new in-memory tests pass, zero file IO in AnalyzerEngine
- **After Phase 4 (US2)**: All existing CLI integration tests pass, FileSystemCodeProvider handles 10k+ files
- **After Phase 5 (US3)**: At least 2 alternative ICodeProvider implementations exist and pass tests
- **After Phase 6**: Constitution check passes, coverage ≥90%, performance within ±5%

---

## Success Metrics

- **SC-001**: AnalyzerEngine tested with in-memory syntax trees, 0 file IO operations during core engine tests ✓
- **SC-002**: All existing CLI integration tests pass with ±5% execution time ✓
- **SC-003**: Code coverage for AnalyzerEngine ≥90% ✓
- **SC-004**: FileSystemCodeProvider analyzes 10,000+ files without memory exhaustion ✓
- **SC-005**: Mock IDE provider implementation in test suite proves extensibility ✓
- **SC-006**: Zero System.IO dependencies in AnalyzerEngine project references ✓
- **SC-007**: Determinism validated - same trees → identical results across providers ✓

---

**Total Tasks**: 48  
**Parallel Tasks**: 22 (marked with [P])  
**User Stories**: 3 (US1: P1 MVP, US2: P2 CLI restore, US3: P3 extensibility)  
**Estimated Complexity**: Medium (requires careful refactoring but clear design)
