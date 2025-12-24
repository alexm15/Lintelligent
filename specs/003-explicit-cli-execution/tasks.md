# Tasks: Explicit CLI Execution Model

**Feature Branch**: `003-explicit-cli-execution`  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)  
**Prerequisites**: Phase 0 (Research) ✅ | Phase 1 (Design) ✅

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **[P]**: Parallelizable (different files, no blocking dependencies)
- **[Story]**: User story label (US1, US2, US3) - ONLY for user story phases
- File paths are absolute from repository root

---

## Phase 1: Setup

**Purpose**: Project structure and infrastructure preparation

- [ ] T001 Create Infrastructure folder in src/Lintelligent.Cli/Infrastructure/ for execution model types
- [ ] T002 Update Lintelligent.Cli.csproj to remove Microsoft.Extensions.Hosting package reference

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core execution types that ALL user stories depend on

**⚠️ CRITICAL**: These MUST be complete before any user story implementation

- [ ] T003 [P] Create CommandResult record in src/Lintelligent.Cli/Infrastructure/CommandResult.cs
- [ ] T004 [P] Create ICommand interface in src/Lintelligent.Cli/Commands/ICommand.cs
- [ ] T005 [P] Create IAsyncCommand interface in src/Lintelligent.Cli/Commands/IAsyncCommand.cs
- [ ] T006 Create CliApplicationBuilder class in src/Lintelligent.Cli/Infrastructure/CliApplicationBuilder.cs
- [ ] T007 Create CliApplication class in src/Lintelligent.Cli/Infrastructure/CliApplication.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Explicit Build-Execute Flow (Priority: P1) 🎯 MVP

**Goal**: Implement CliApplicationBuilder → Build() → Execute() → Exit pattern in Program.cs, replacing hosting framework with explicit execution

**Independent Test**: Create CliApplicationBuilder, call Build(), execute a test command, verify synchronous exit code return without background tasks

### Implementation for User Story 1

- [ ] T008 [US1] Implement CliApplicationBuilder.ConfigureServices() method for DI registration
- [ ] T009 [US1] Implement CliApplicationBuilder.AddCommand<TCommand>() method for command registration
- [ ] T010 [US1] Implement CliApplicationBuilder.Build() method to create CliApplication with service provider
- [ ] T011 [US1] Implement CliApplication.Execute(string[] args) method with command resolution and exception handling
- [ ] T012 [US1] Update Program.cs to use CliApplicationBuilder instead of Host.CreateDefaultBuilder
- [ ] T013 [US1] Update Bootstrapper.cs to be a static configuration method (remove IServiceCollection parameter, return Action<IServiceCollection>)
- [ ] T014 [US1] Verify Program.Main() signature is `static int Main(string[] args)` (synchronous, not async)
- [ ] T015 [US1] Add console output for CommandResult.Output and CommandResult.Error in Program.cs

**Checkpoint**: Program.cs builds and runs with explicit execution pattern; hosting framework removed

---

## Phase 4: User Story 2 - Testable Command Execution (Priority: P1)

**Goal**: Enable in-memory testing of ScanCommand without process spawning by returning CommandResult instead of writing to console directly

**Independent Test**: Create CliApplication in test, execute ScanCommand with test arguments, assert on CommandResult properties - all in-memory

### Implementation for User Story 2

- [ ] T016 [US2] Update ScanCommand to implement IAsyncCommand interface
- [ ] T017 [US2] Refactor ScanCommand.ExecuteAsync to return Task<CommandResult> instead of Task
- [ ] T018 [US2] Capture report output in string variable instead of Console.WriteLine in ScanCommand
- [ ] T019 [US2] Return CommandResult.Success(report) for successful analysis in ScanCommand
- [ ] T020 [US2] Add exception handling to ScanCommand (ArgumentException → exit code 2, others → exit code 1)
- [ ] T021 [US2] Remove 'await Task.CompletedTask' from ScanCommand (no longer needed)
- [ ] T022 [P] [US2] Create CliApplicationTests.cs in tests/Lintelligent.Cli.Tests/ for builder and execute flow tests
- [ ] T023 [P] [US2] Write test for CliApplicationBuilder.Build() returns valid CliApplication
- [ ] T024 [P] [US2] Write test for CliApplication.Execute() with valid command returns exit code 0
- [ ] T025 [P] [US2] Write test for CliApplication.Execute() with ArgumentException returns exit code 2
- [ ] T026 [US2] Update ScanCommandTests.cs to use in-memory CliApplication.Execute() instead of process spawning
- [ ] T027 [US2] Write test verifying CommandResult.Output contains analysis report
- [ ] T028 [US2] Write test verifying CommandResult.Error is empty on success

**Checkpoint**: ScanCommand testable in-memory; all tests pass without process spawning (execution time <50ms per test)

---

## Phase 5: User Story 3 - No Implicit Async Hosting (Priority: P2)

**Goal**: Remove all traces of hosting framework and async entry point; verify synchronous execution path

**Independent Test**: Verify Program.Main() is synchronous, no Microsoft.Extensions.Hosting references exist, no background tasks after execution

### Implementation for User Story 3

- [ ] T029 [P] [US3] Verify Microsoft.Extensions.Hosting package is removed from Lintelligent.Cli.csproj (should be done in T002)
- [ ] T030 [P] [US3] Remove 'using Microsoft.Extensions.Hosting' from Program.cs
- [ ] T031 [P] [US3] Verify no 'async' keyword in Program.Main() signature
- [ ] T032 [P] [US3] Verify no 'await' calls at Program.Main() level (Execute() is synchronous)
- [ ] T033 [US3] Run 'dotnet list package' to confirm zero hosting framework dependencies
- [ ] T034 [US3] Verify CliApplication.Dispose() properly cleans up service provider (no lingering resources)

**Checkpoint**: Zero hosting framework dependencies; Program.Main() is fully synchronous; no background tasks

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, edge cases, and final cleanup

- [ ] T035 [P] Add XML documentation comments to CliApplicationBuilder public methods
- [ ] T036 [P] Add XML documentation comments to CliApplication.Execute method
- [ ] T037 [P] Add XML documentation comments to CommandResult factory methods
- [ ] T038 [P] Update README.md with new CLI execution pattern example
- [ ] T039 [P] Add quickstart example to project documentation showing builder usage
- [ ] T040 Test edge case: CliApplicationBuilder.Build() with no commands registered throws InvalidOperationException
- [ ] T041 Test edge case: CliApplication.Execute() with unrecognized command returns error exit code
- [ ] T042 Test edge case: CommandResult with exit code outside 0-255 range throws ArgumentOutOfRangeException
- [ ] T043 Verify all 84+ existing tests still pass after refactoring
- [ ] T044 Run manual smoke test: 'dotnet run --project src/Lintelligent.Cli -- scan ./src' returns exit code 0

---

## Dependencies & Execution Order

### User Story Completion Order

```text
Phase 1 (Setup)
     ↓
Phase 2 (Foundational) ← BLOCKING: Must complete before any user story
     ↓
     ├──→ Phase 3 (US1: Explicit Build-Execute Flow) ← MVP
     ├──→ Phase 4 (US2: Testable Command Execution) ← Can start after US1
     └──→ Phase 5 (US3: No Implicit Async Hosting) ← Can start after US1
          ↓
     Phase 6 (Polish)
```

### Parallel Execution Opportunities

**Phase 2 Foundational** (all [P] tasks can run in parallel):
- T003, T004, T005 can be implemented simultaneously (different files, no dependencies)

**Phase 3 User Story 1**:
- Sequential implementation required (T008 → T009 → T010 → T011 → T012-T015)

**Phase 4 User Story 2**:
- T016-T021 (ScanCommand refactoring) must be sequential
- T022-T025 (CliApplicationTests) can be parallel with each other
- T026-T028 (ScanCommandTests updates) can be parallel with T022-T025

**Phase 5 User Story 3**:
- T029-T032 can all run in parallel (verification tasks, different files)
- T033-T034 depend on prior cleanup

**Phase 6 Polish**:
- T035-T039 (documentation) can all run in parallel
- T040-T042 (edge case tests) can run in parallel
- T043-T044 must run sequentially after all implementation

---

## Implementation Strategy

### MVP First (Minimum Viable Product)

**MVP Scope** = Phase 1 + Phase 2 + Phase 3 (User Story 1)

This delivers the core value:
- ✅ Explicit build → execute → exit pattern
- ✅ Hosting framework removed
- ✅ Synchronous Program.Main()
- ✅ Basic CliApplication functional

**After MVP**, incrementally add:
1. Phase 4 (US2): Testable command execution
2. Phase 5 (US3): Async cleanup verification
3. Phase 6: Polish and edge cases

### Incremental Delivery

Each user story phase should be:
1. Implemented completely
2. Tested independently
3. Committed to version control
4. Verified with existing tests passing

**Do NOT** mix user story implementations - complete one story before starting the next.

---

## Success Criteria

- [ ] All 44 tasks complete
- [ ] Program.Main() signature is `static int Main(string[] args)` (synchronous)
- [ ] Zero dependencies on Microsoft.Extensions.Hosting
- [ ] CommandResult enables in-memory testing (<50ms per test)
- [ ] ScanCommand returns CommandResult instead of void
- [ ] All 84+ existing tests pass
- [ ] New CliApplicationTests and updated ScanCommandTests pass
- [ ] Manual CLI execution: `dotnet run -- scan ./src` works and returns exit code 0
- [ ] Constitutional Principle IV verified: Explicit execution model implemented

---

## Task Summary

- **Total Tasks**: 44
- **Setup**: 2 tasks
- **Foundational**: 5 tasks (blocking)
- **User Story 1 (P1)**: 8 tasks (MVP core)
- **User Story 2 (P1)**: 13 tasks (testability)
- **User Story 3 (P2)**: 6 tasks (cleanup/verification)
- **Polish**: 10 tasks (documentation, edge cases, final validation)

**Estimated Effort**: 6-8 hours (1 day) for MVP, 10-12 hours (1.5 days) total

**Parallelizable Tasks**: 16 tasks marked with [P] can run simultaneously with others in their phase
