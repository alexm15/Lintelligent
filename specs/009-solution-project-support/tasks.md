# Implementation Tasks: Solution & Project File Support

**Feature**: 009-solution-project-support  
**Branch**: `009-solution-project-support`  
**Created**: December 25, 2025  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Overview

This document breaks down Feature 009 into atomic, executable tasks organized by user story priority. Each user story is designed to be independently testable, allowing incremental delivery of value.

**Total Tasks**: 65  
**User Stories**: 5 (3 P1, 2 P2)  
**Estimated Complexity**: Medium-High (MSBuild integration, new abstractions)

## Task Format Legend

- `- [ ]` = Checkbox (all tasks start unchecked)
- `[T###]` = Task ID (sequential numbering)
- `[P]` = Parallelizable (can run concurrently with other [P] tasks in same phase)
- `[US#]` = User Story label (maps to spec.md user stories)
- File paths are absolute from repository root

## Phase 1: Setup & Infrastructure

**Goal**: Install dependencies, create directory structure, configure project files  
**Success Criteria**: Buildalyzer package installed, directory structure created, compilation succeeds

### Tasks

- [ ] T001 Add Buildalyzer NuGet package to Lintelligent.AnalyzerEngine.csproj (version 7.1.2 or latest stable)
- [ ] T002 Add Microsoft.Build.Construction reference to Lintelligent.AnalyzerEngine.csproj (if not already transitive)
- [ ] T003 Create directory src/Lintelligent.AnalyzerEngine/Abstractions/ (if not exists)
- [ ] T004 Create directory src/Lintelligent.AnalyzerEngine/ProjectModel/
- [ ] T005 Create directory src/Lintelligent.Cli/Providers/
- [ ] T006 Create directory tests/Lintelligent.AnalyzerEngine.Tests/ProjectModel/
- [ ] T007 Create directory tests/Lintelligent.AnalyzerEngine.Tests/Fixtures/
- [ ] T008 Create directory tests/Lintelligent.Cli.Tests/Providers/
- [ ] T009 Verify solution builds successfully with new dependencies (dotnet build)

**Independent Test**: Run `dotnet build` and verify 0 errors, Buildalyzer package appears in project references

---

## Phase 2: Foundational Components

**Goal**: Implement domain models and core abstractions that all user stories depend on  
**Success Criteria**: Domain models compile, abstractions defined, unit tests pass

### Domain Models (Block all user stories)

- [ ] T010 [P] Create TargetFramework.cs in src/Lintelligent.AnalyzerEngine/ProjectModel/ (Moniker, FrameworkFamily, Version properties, IEquatable implementation)
- [ ] T011 [P] Create CompileItemInclusionType.cs enum in src/Lintelligent.AnalyzerEngine/ProjectModel/ (DefaultGlob, ExplicitInclude, LinkedFile)
- [ ] T012 [P] Create CompileItem.cs in src/Lintelligent.AnalyzerEngine/ProjectModel/ (FilePath, InclusionType, OriginalIncludePath properties)
- [ ] T013 [P] Create ProjectReference.cs in src/Lintelligent.AnalyzerEngine/ProjectModel/ (ReferencedProjectPath, ReferencedProjectName properties)
- [ ] T014 Create Project.cs in src/Lintelligent.AnalyzerEngine/ProjectModel/ (all properties per data-model.md, constructor validation)
- [ ] T015 Create Solution.cs in src/Lintelligent.AnalyzerEngine/ProjectModel/ (FilePath, Name, Projects, Configurations, GetDependencyGraph method)

### Core Abstractions (Block all user stories)

- [ ] T016 [P] Create ISolutionProvider.cs in src/Lintelligent.AnalyzerEngine/Abstractions/ (ParseSolutionAsync method signature per contracts/ISolutionProvider.md)
- [ ] T017 [P] Create IProjectProvider.cs in src/Lintelligent.AnalyzerEngine/Abstractions/ (EvaluateProjectAsync and EvaluateAllProjectsAsync per contracts/IProjectProvider.md)

### Unit Tests for Domain Models

- [ ] T018 [P] Create TargetFrameworkTests.cs in tests/Lintelligent.AnalyzerEngine.Tests/ProjectModel/ (test Moniker parsing, FrameworkFamily detection, IsModernDotNet/IsNetFramework/IsNetStandard properties)
- [ ] T019 [P] Create CompileItemTests.cs in tests/Lintelligent.AnalyzerEngine.Tests/ProjectModel/ (test constructor validation, InclusionType scenarios)
- [ ] T020 [P] Create ProjectReferenceTests.cs in tests/Lintelligent.AnalyzerEngine.Tests/ProjectModel/ (test constructor validation, path normalization)
- [ ] T021 [P] Create ProjectTests.cs in tests/Lintelligent.AnalyzerEngine.Tests/ProjectModel/ (test constructor validation, IsMultiTargeted property, all properties correctly assigned)
- [ ] T022 [P] Create SolutionTests.cs in tests/Lintelligent.AnalyzerEngine.Tests/ProjectModel/ (test GetDependencyGraph method, constructor validation)

**Independent Test**: Run `dotnet test` on Lintelligent.AnalyzerEngine.Tests and verify all domain model tests pass

---

## Phase 3: User Story 1 - Analyze Projects from Solution (P1)

**Goal**: Enable `lintelligent scan MySolution.sln` to discover and analyze all projects  
**Success Criteria**: 
- Solution files can be parsed to extract project paths
- Projects are discovered automatically
- All projects analyzed with aggregated results
- Test with 3-project solution, verify all projects analyzed

### Test Fixtures (US1)

- [ ] T023 [US1] Create test solution file tests/Lintelligent.Cli.Tests/Fixtures/TestSolution.sln with 3 projects (ProjectA, ProjectB, ProjectC)
- [ ] T024 [US1] Create tests/Lintelligent.Cli.Tests/Fixtures/ProjectA/ProjectA.csproj (simple C# library targeting net8.0, 2-3 source files)
- [ ] T025 [US1] Create tests/Lintelligent.Cli.Tests/Fixtures/ProjectB/ProjectB.csproj (simple C# library targeting net8.0, 2-3 source files)
- [ ] T026 [US1] Create tests/Lintelligent.Cli.Tests/Fixtures/ProjectC/ProjectC.csproj (simple C# library targeting net8.0, 2-3 source files)
- [ ] T027 [US1] Add simple .cs source files to each test project (at least one file with intentional LNT001 violation for testing)

### ISolutionProvider Implementation (US1)

- [ ] T028 [US1] Create BuildalyzerSolutionProvider.cs in src/Lintelligent.Cli/Providers/ (implement ISolutionProvider using Microsoft.Build.Construction.SolutionFile.Parse)
- [ ] T029 [US1] Implement ParseSolutionAsync method in BuildalyzerSolutionProvider (extract project paths, configurations, create Solution entity)
- [ ] T030 [US1] Add error handling for missing .sln files (FileNotFoundException with clear message)
- [ ] T031 [US1] Add error handling for malformed .sln files (InvalidOperationException with line number if available)
- [ ] T032 [US1] Add logging for solution discovery (ILogger, log solution name, project count, configurations)

### ISolutionProvider Tests (US1)

- [ ] T033 [P] [US1] Create BuildalyzerSolutionProviderTests.cs in tests/Lintelligent.Cli.Tests/Providers/
- [ ] T034 [US1] Write test: ParseSolutionAsync_ValidSolution_ReturnsAllProjects (use TestSolution.sln, verify 3 projects)
- [ ] T035 [US1] Write test: ParseSolutionAsync_MissingSolution_ThrowsFileNotFoundException
- [ ] T036 [US1] Write test: ParseSolutionAsync_MalformedSolution_ThrowsInvalidOperationException
- [ ] T037 [US1] Write test: ParseSolutionAsync_NestedFolders_DiscoverAllProjects (if test fixture supports)

### CLI Integration (US1)

- [ ] T038 [US1] Update ScanCommand.cs in src/Lintelligent.Cli/Commands/ to accept .sln file paths (detect .sln extension)
- [ ] T039 [US1] Add ISolutionProvider and IProjectProvider injection to ScanCommand constructor
- [ ] T040 [US1] Implement solution path handling in ScanCommand.Execute: if .sln, call ParseSolutionAsync, then EvaluateAllProjectsAsync
- [ ] T041 [US1] Update Bootstrapper.cs in src/Lintelligent.Cli/ to register BuildalyzerSolutionProvider as ISolutionProvider (singleton or transient)

### CLI Tests (US1)

- [ ] T042 [US1] Create ScanCommandSolutionTests.cs in tests/Lintelligent.Cli.Tests/Commands/
- [ ] T043 [US1] Write test: ScanCommand_SolutionPath_AnalyzesAllProjects (verify 3 projects analyzed, diagnostics aggregated)
- [ ] T044 [US1] Write test: ScanCommand_SolutionPath_MissingProject_ContinuesWithOthers (verify graceful degradation)

**Independent Test for US1**: Run `lintelligent scan tests/Lintelligent.Cli.Tests/Fixtures/TestSolution.sln` and verify output shows 3 projects analyzed with aggregated diagnostics

---

## Phase 4: User Story 2 - Respect Conditional Compilation (P1)

**Goal**: Analyzer respects `#if DEBUG` blocks and DefineConstants from .csproj  
**Success Criteria**:
- Conditional symbols extracted from project configuration
- Symbols passed to Roslyn parser
- Debug-only code analyzed in Debug config, skipped in Release config
- Test with project containing `#if DEBUG` blocks

### Test Fixtures (US2)

- [ ] T045 [US2] Create tests/Lintelligent.Cli.Tests/Fixtures/ConditionalProject/ConditionalProject.csproj (target net8.0, Debug config with DEBUG;TRACE symbols, Release config with RELEASE symbol)
- [ ] T046 [US2] Create tests/Lintelligent.Cli.Tests/Fixtures/ConditionalProject/ConditionalCode.cs (contains `#if DEBUG` block with code, `#if RELEASE` block with different code)

### IProjectProvider Implementation (US2)

- [ ] T047 [US2] Create BuildalyzerProjectProvider.cs in src/Lintelligent.Cli/Providers/ (implement IProjectProvider using Buildalyzer)
- [ ] T048 [US2] Implement EvaluateProjectAsync method: use Buildalyzer AnalyzerManager to load project, call Build(configuration)
- [ ] T049 [US2] Extract ConditionalSymbols from AnalyzerResult.PreprocessorSymbols in BuildalyzerProjectProvider
- [ ] T050 [US2] Extract TargetFramework(s) from AnalyzerResult in BuildalyzerProjectProvider (create TargetFramework entities)
- [ ] T051 [US2] Extract Configuration and Platform from AnalyzerResult in BuildalyzerProjectProvider
- [ ] T052 [US2] Create Project entity in BuildalyzerProjectProvider with extracted metadata
- [ ] T053 [US2] Implement EvaluateAllProjectsAsync method: parallel evaluation of all projects with try-catch per project
- [ ] T054 [US2] Add error handling: log failed projects, continue with successful ones, create Solution with partial results
- [ ] T055 [US2] Add logging for project evaluation (project name, target framework, symbol count, source file count)
- [ ] T056 [US2] Update Bootstrapper.cs to register BuildalyzerProjectProvider as IProjectProvider

### ICodeProvider Integration (US2)

- [ ] T057 [US2] Update ICodeProvider implementation (likely FileSystemCodeProvider) to accept ConditionalSymbols parameter
- [ ] T058 [US2] Pass ConditionalSymbols to CSharpParseOptions.WithPreprocessorSymbols() when creating SyntaxTree in ICodeProvider
- [ ] T059 [US2] Update AnalyzerEngine.cs to receive Project entities and pass ConditionalSymbols to ICodeProvider

### IProjectProvider Tests (US2)

- [ ] T060 [P] [US2] Create BuildalyzerProjectProviderTests.cs in tests/Lintelligent.Cli.Tests/Providers/
- [ ] T061 [US2] Write test: EvaluateProjectAsync_DebugConfig_ExtractsDebugSymbols (verify DEBUG and TRACE in ConditionalSymbols)
- [ ] T062 [US2] Write test: EvaluateProjectAsync_ReleaseConfig_ExtractsReleaseSymbols (verify RELEASE in ConditionalSymbols, DEBUG not present)
- [ ] T063 [US2] Write test: EvaluateProjectAsync_InvalidProject_ThrowsInvalidOperationException
- [ ] T064 [US2] Write test: EvaluateAllProjectsAsync_OneProjectFails_ReturnsPartialResults (verify graceful degradation)

### Integration Tests (US2)

- [ ] T065 [US2] Write test in ScanCommandSolutionTests.cs: ScanCommand_DebugConfig_AnalyzesDebugCode (verify `#if DEBUG` block analyzed)
- [ ] T066 [US2] Write test in ScanCommandSolutionTests.cs: ScanCommand_ReleaseConfig_SkipsDebugCode (verify `#if DEBUG` block NOT analyzed)

### CLI Flags (US2)

- [ ] T067 [US2] Add --configuration flag to ScanCommand.cs (default "Debug", accept any string)
- [ ] T068 [US2] Pass configuration parameter from CLI flag to EvaluateProjectAsync and EvaluateAllProjectsAsync calls
- [ ] T069 [US2] Add help text for --configuration flag (describe purpose, default value, example usage)

**Independent Test for US2**: Run `lintelligent scan ConditionalProject.csproj --configuration Debug` and verify DEBUG code analyzed, then run with `--configuration Release` and verify DEBUG code skipped

---

## Phase 5: User Story 3 - Handle Compile Includes/Excludes (P1)

**Goal**: Respect `<Compile Include>` and `<Compile Remove>` directives, glob patterns, linked files  
**Success Criteria**:
- Files excluded via `<Compile Remove>` are not analyzed
- Files included via `<Compile Include>` (including linked) are analyzed
- SDK-style glob patterns respected
- Test with project containing Remove directive and linked file

### Test Fixtures (US3)

- [ ] T070 [US3] Create tests/Lintelligent.Cli.Tests/Fixtures/CompileDirectivesProject/CompileDirectivesProject.csproj (SDK-style, `<Compile Remove="Generated/**/*.cs" />`, `<Compile Include="..\Shared\SharedCode.cs" Link="Shared\SharedCode.cs" />`)
- [ ] T071 [US3] Create tests/Lintelligent.Cli.Tests/Fixtures/CompileDirectivesProject/Generated/GeneratedFile.cs (should be excluded)
- [ ] T072 [US3] Create tests/Lintelligent.Cli.Tests/Fixtures/Shared/SharedCode.cs (should be included as linked file)
- [ ] T073 [US3] Create tests/Lintelligent.Cli.Tests/Fixtures/CompileDirectivesProject/IncludedFile.cs (normal file, should be included)

### IProjectProvider Enhancement (US3)

- [ ] T074 [US3] Extract SourceFiles from AnalyzerResult.SourceFiles in BuildalyzerProjectProvider (Buildalyzer has already evaluated globs)
- [ ] T075 [US3] Create CompileItem entities in BuildalyzerProjectProvider for each source file (determine InclusionType: DefaultGlob if in project dir, LinkedFile if outside, ExplicitInclude if in Compile items)
- [ ] T076 [US3] Store CompileItems in Project entity CompileItems property

### Tests (US3)

- [ ] T077 [US3] Write test in BuildalyzerProjectProviderTests.cs: EvaluateProjectAsync_CompileRemove_ExcludesFiles (verify Generated/**/*.cs excluded)
- [ ] T078 [US3] Write test in BuildalyzerProjectProviderTests.cs: EvaluateProjectAsync_CompileInclude_IncludesLinkedFiles (verify ..\Shared\SharedCode.cs included)
- [ ] T079 [US3] Write test in BuildalyzerProjectProviderTests.cs: EvaluateProjectAsync_SDKGlobs_IncludesAllCsFiles (verify default **/*.cs pattern works)

**Independent Test for US3**: Run `lintelligent scan CompileDirectivesProject.csproj` and verify Generated/GeneratedFile.cs is NOT in analyzed files, Shared/SharedCode.cs IS analyzed

---

## Phase 6: User Story 4 - Multi-Project Analysis Aggregation (P2)

**Goal**: Aggregate results across projects, capture dependency graph, show per-project breakdown  
**Success Criteria**:
- Results clearly indicate which diagnostics belong to which project
- Per-project diagnostic count shown in output
- Dependency graph available (GetDependencyGraph method works)
- Test with solution where ProjectA references ProjectB

### Test Fixtures (US4)

- [ ] T080 [US4] Update tests/Lintelligent.Cli.Tests/Fixtures/ProjectA/ProjectA.csproj to add `<ProjectReference Include="..\ProjectB\ProjectB.csproj" />` (ProjectA → ProjectB dependency)

### IProjectProvider Enhancement (US4)

- [ ] T081 [US4] Extract ProjectReferences from AnalyzerResult.ProjectReferences in BuildalyzerProjectProvider
- [ ] T082 [US4] Create ProjectReference entities in BuildalyzerProjectProvider for each referenced project
- [ ] T083 [US4] Store ProjectReferences in Project entity ProjectReferences property
- [ ] T084 [US4] Implement Solution.GetDependencyGraph() method (iterate Projects, build dictionary mapping project path to referenced project paths)

### Reporting Integration (US4)

- [ ] T085 [US4] Update ReportGenerator.cs in src/Lintelligent.Reporting/ to include project metadata in reports (project name, path, diagnostic count per project)
- [ ] T086 [US4] Add per-project breakdown section to JSON output format (if using Feature 006 JSON formatter)
- [ ] T087 [US4] Add per-project breakdown section to Markdown output format (if using Feature 006 Markdown formatter)
- [ ] T088 [US4] Add per-project breakdown section to Console output format

### Tests (US4)

- [ ] T089 [US4] Write test in BuildalyzerProjectProviderTests.cs: EvaluateProjectAsync_ProjectReferences_CapturesReferences (verify ProjectReference entities created)
- [ ] T090 [US4] Write test in SolutionTests.cs: GetDependencyGraph_WithReferences_ReturnsDependencyMap (verify ProjectA → [ProjectB] mapping)
- [ ] T091 [US4] Write test in ScanCommandSolutionTests.cs: ScanCommand_MultiProject_ShowsPerProjectBreakdown (verify output distinguishes ProjectA vs ProjectB diagnostics)

**Independent Test for US4**: Run `lintelligent scan TestSolution.sln --format json` and verify JSON output has per-project sections with correct diagnostic counts

---

## Phase 7: User Story 5 - Target Framework Awareness (P2)

**Goal**: Support multi-targeted projects, respect target framework for analysis  
**Success Criteria**:
- Multi-targeted projects can be analyzed (default: first target)
- --target-framework CLI flag allows selecting specific target
- TargetFramework metadata captured in results
- Test with project targeting net472;net8.0

### Test Fixtures (US5)

- [ ] T092 [US5] Create tests/Lintelligent.Cli.Tests/Fixtures/MultiTargetProject/MultiTargetProject.csproj (`<TargetFrameworks>net472;net8.0</TargetFrameworks>`)
- [ ] T093 [US5] Add source file with framework-conditional code to MultiTargetProject (`#if NET8_0_OR_GREATER` block)

### IProjectProvider Enhancement (US5)

- [ ] T094 [US5] Update EvaluateProjectAsync to handle multi-targeted projects: if targetFramework parameter null, select first from AnalyzerResults, else filter to specified target
- [ ] T095 [US5] Store AllTargetFrameworks in Project entity (all targets from AnalyzerResults)
- [ ] T096 [US5] Store selected TargetFramework in Project entity
- [ ] T097 [US5] Add validation: if targetFramework specified but not found in project, throw InvalidOperationException with available frameworks listed

### CLI Flags (US5)

- [ ] T098 [US5] Add --target-framework flag to ScanCommand.cs (optional, no default)
- [ ] T099 [US5] Pass targetFramework parameter from CLI flag to EvaluateProjectAsync and EvaluateAllProjectsAsync calls
- [ ] T100 [US5] Add help text for --target-framework flag (describe purpose, example: net8.0, note: optional)

### Tests (US5)

- [ ] T101 [US5] Write test in BuildalyzerProjectProviderTests.cs: EvaluateProjectAsync_MultiTarget_SelectsFirstByDefault (verify net472 selected when no targetFramework specified)
- [ ] T102 [US5] Write test in BuildalyzerProjectProviderTests.cs: EvaluateProjectAsync_MultiTarget_SelectsSpecifiedTarget (verify net8.0 selected when --target-framework net8.0)
- [ ] T103 [US5] Write test in BuildalyzerProjectProviderTests.cs: EvaluateProjectAsync_MultiTarget_InvalidTarget_ThrowsException (verify exception lists available targets)
- [ ] T104 [US5] Write test in ScanCommandSolutionTests.cs: ScanCommand_MultiTarget_WithFlag_AnalyzesSpecifiedTarget

**Independent Test for US5**: Run `lintelligent scan MultiTargetProject.csproj` (verify net472 analyzed), then `lintelligent scan MultiTargetProject.csproj --target-framework net8.0` (verify net8.0 analyzed)

---

## Phase 8: Polish & Cross-Cutting Concerns

**Goal**: Documentation, logging, error messages, edge case handling  
**Success Criteria**: All error scenarios have clear messages, logging is comprehensive, README updated

### Documentation

- [ ] T105 Update README.md with solution file support (add examples of `lintelligent scan MySolution.sln`)
- [ ] T106 Update README.md with --configuration flag documentation
- [ ] T107 Update README.md with --target-framework flag documentation
- [ ] T108 Add troubleshooting section to README.md (missing SDK, restore required, etc.)

### Error Messages

- [ ] T109 [P] Enhance error message for missing .sln file (include searched path, suggest checking path)
- [ ] T110 [P] Enhance error message for malformed .sln file (include line number if available, suggest checking XML syntax)
- [ ] T111 [P] Enhance error message for missing .csproj file (include project name, suggest restore or path check)
- [ ] T112 [P] Enhance error message for MSBuild evaluation failure (include project name, suggest SDK installation or restore)

### Logging

- [ ] T113 [P] Add structured logging for solution parsing (solution name, project count, configurations)
- [ ] T114 [P] Add structured logging for project evaluation (project name, target framework, symbol count, source file count, duration)
- [ ] T115 [P] Add logging for failed projects (project name, error message, continue/skip indicator)
- [ ] T116 [P] Add logging for dependency graph generation (project count, reference count)

### Edge Cases

- [ ] T117 Handle empty solution (0 projects) - log warning, exit gracefully with code 0
- [ ] T118 Handle solution with all projects failing evaluation - log error, exit with code 1 (or 2 for evaluation failure)
- [ ] T119 Handle circular project references - Buildalyzer should handle, but add defensive check and logging if detected
- [ ] T120 Handle platform-specific project conditions (AnyCPU, x64, ARM) - use default platform, log if multiple detected

### Performance

- [ ] T121 Verify parallel project evaluation works (EvaluateAllProjectsAsync uses Task.WhenAll or Parallel.ForEach)
- [ ] T122 Add performance logging for solution-level operations (total evaluation time, analysis time)

---

## Dependencies Between User Stories

### Completion Order (Dependency Graph)

```
Phase 1 (Setup)
  ↓
Phase 2 (Foundational)
  ↓
├─→ Phase 3 (US1) ←─── Blocks all other stories
│     ↓
├─→ Phase 4 (US2) ←─┐
│     ↓             │ Independent after US1
├─→ Phase 5 (US3) ←─┤ Can be done in parallel
│     ↓             │
├─→ Phase 6 (US4) ←─┘
│     ↓
└─→ Phase 7 (US5)
      ↓
   Phase 8 (Polish)
```

**Critical Path**: Setup → Foundational → US1 → US2/US3/US4 (parallel) → US5 → Polish

**MVP Minimum**: Complete Phase 1, 2, 3 (US1) for basic solution support

**Parallel Opportunities**:
- Phase 2: Tasks T010-T013, T016-T017, T018-T022 can run in parallel (domain models + abstractions + tests)
- Phase 4-6: US2, US3, US4 can be implemented in parallel after US1 completes
- Phase 8: Most polish tasks (T109-T116) can run in parallel

---

## Implementation Strategy

### MVP First (Minimum Viable Product)

**Phases 1-3** deliver core value:
- Solution file parsing
- Project discovery and evaluation
- Basic analysis of all projects
- Aggregated results

**Estimated effort**: 40-50% of total work

### Incremental Delivery

**Phase 4** adds critical correctness:
- Conditional compilation support
- Prevents false positives from inactive code

**Phase 5** adds completeness:
- Glob pattern handling
- Linked file support

**Phases 6-7** add enterprise features:
- Dependency graph
- Multi-targeting support

**Phase 8** adds polish:
- Better UX, logging, error handling

### Testing Strategy

- **Unit tests**: Domain models (Phase 2, Tasks T018-T022)
- **Integration tests**: Provider implementations (Tasks T033-T037, T060-T064, T077-T079, T089-T091, T101-T104)
- **End-to-end tests**: ScanCommand with test solutions (Tasks T042-T044, T065-T066, T091, T104)
- **Manual testing**: Real-world solutions during development

### Validation Criteria

Each phase includes "Independent Test" criteria. Feature is complete when:

1. ✅ All 122 tasks checked off
2. ✅ All unit tests pass (Tasks T018-T022, others)
3. ✅ All integration tests pass (Provider tests, ScanCommand tests)
4. ✅ Manual testing with real solutions succeeds
5. ✅ All 8 success criteria from spec.md verified (SC-001 through SC-008)
6. ✅ All 14 functional requirements from spec.md verified (FR-001 through FR-014)
7. ✅ All 5 user stories' acceptance scenarios pass

---

## Task Summary

| Phase | Tasks | Parallelizable | Story | Critical Path |
|-------|-------|----------------|-------|---------------|
| 1: Setup | T001-T009 (9) | 0 | - | Yes |
| 2: Foundational | T010-T022 (13) | 11 | - | Yes |
| 3: US1 (P1) | T023-T044 (22) | 1 | US1 | Yes |
| 4: US2 (P1) | T045-T069 (25) | 2 | US2 | Partial |
| 5: US3 (P1) | T070-T079 (10) | 0 | US3 | Partial |
| 6: US4 (P2) | T080-T091 (12) | 0 | US4 | No |
| 7: US5 (P2) | T092-T104 (13) | 0 | US5 | No |
| 8: Polish | T105-T122 (18) | 8 | - | No |
| **Total** | **122 tasks** | **22 parallelizable** | **5 stories** | - |

**Estimated Timeline** (1 developer, full-time):
- Phase 1: 0.5 day
- Phase 2: 1.5 days
- Phase 3 (US1): 3 days
- Phase 4 (US2): 3 days
- Phase 5 (US3): 1.5 days
- Phase 6 (US4): 2 days
- Phase 7 (US5): 2 days
- Phase 8: 1.5 days

**Total**: ~15 days (3 weeks with reviews, testing, documentation)

With parallelization (2 developers): ~10 days (2 weeks)
