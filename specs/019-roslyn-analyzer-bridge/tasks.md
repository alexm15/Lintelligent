# Tasks: Roslyn Analyzer Bridge

**Input**: Design documents from `specs/019-roslyn-analyzer-bridge/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Feature Branch**: `019-roslyn-analyzer-bridge`  
**Date**: December 24, 2025

**Organization**: Tasks organized by user story to enable independent implementation and testing. Each phase represents a complete, testable increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Infrastructure)

**Purpose**: Initialize Lintelligent.Analyzers project and configure NuGet packaging

**Duration**: ~15 minutes

- [ ] T001 Create Lintelligent.Analyzers project in src/Lintelligent.Analyzers/ targeting netstandard2.0
- [ ] T002 Configure .csproj for NuGet analyzer packaging: Add <IncludeBuildOutput>false</IncludeBuildOutput>, <DevelopmentDependency>true</DevelopmentDependency>, <TargetsForTfmSpecificContentInPackage>$(TargetsForTfmSpecificContentInPackage);PackAnalyzer</TargetsForTfmSpecificContentInPackage>
- [ ] T003 [P] Add PackageReference: Microsoft.CodeAnalysis.CSharp 4.12.0 (PrivateAssets=all)
- [ ] T004 [P] Add PackageReference: Microsoft.CodeAnalysis.Analyzers 3.11.0 (PrivateAssets=all)
- [ ] T005 Add ProjectReference to Lintelligent.AnalyzerEngine (for IAnalyzerRule access)
- [ ] T006 Add solution reference: dotnet sln add src/Lintelligent.Analyzers/Lintelligent.Analyzers.csproj
- [ ] T007 Verify build succeeds: dotnet build src/Lintelligent.Analyzers/ -c Release

**Checkpoint**: Project compiles successfully, NuGet package structure configured

---

## Phase 2: Foundational (Core Adapters)

**Purpose**: Core utilities that ALL user stories depend on (severity mapping, descriptor factory, diagnostic converter)

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

**Duration**: ~30 minutes

- [ ] T008 [P] Create SeverityMapper in src/Lintelligent.Analyzers/Metadata/SeverityMapper.cs (ToRoslynSeverity, FromEditorConfigSeverity, IsSuppressed)
- [ ] T009 [P] Create RuleDescriptorFactory in src/Lintelligent.Analyzers/Adapters/RuleDescriptorFactory.cs (Create, GetHelpLinkUri, GetCustomTags)
- [ ] T010 [P] Create DiagnosticConverter in src/Lintelligent.Analyzers/Adapters/DiagnosticConverter.cs (Convert, CreateLocation)
- [ ] T011 Create rule ID → anchor mapping dictionary in RuleDescriptorFactory (LNT001-LNT008 anchors)
- [ ] T012 Implement help link URI generation with GitHub docs base URL + rule-specific anchors
- [ ] T012a Implement logging for analyzer initialization errors in LintelligentDiagnosticAnalyzer (rule discovery failures, descriptor creation errors) using MSBuild diagnostic output

**Checkpoint**: Foundation ready - all adapters available for analyzer implementation

---

## Phase 3: User Story 3 - NuGet Package Distribution (Priority: P1) 🎯 MVP Core

**Goal**: Enable developers to add Lintelligent analysis via `dotnet add package Lintelligent.Analyzers` with zero-config setup

**Independent Test**: Install package in test project, build, verify analyzer discovers all 8 rules without manual configuration

### Tests for User Story 3

- [ ] T013 [P] [US3] Create Lintelligent.Analyzers.Tests project in tests/Lintelligent.Analyzers.Tests/ (xUnit)
- [ ] T014 [P] [US3] Add PackageReference: Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit 1.1.2
- [ ] T015 [P] [US3] Add PackageReference: Microsoft.CodeAnalysis.CSharp.Workspaces 4.12.0
- [ ] T016 [P] [US3] Add ProjectReference to Lintelligent.Analyzers

### Implementation for User Story 3

- [ ] T017 [US3] Create LintelligentDiagnosticAnalyzer skeleton in src/Lintelligent.Analyzers/LintelligentDiagnosticAnalyzer.cs
- [ ] T018 [US3] Add [DiagnosticAnalyzer(LanguageNames.CSharp)] attribute to LintelligentDiagnosticAnalyzer
- [ ] T019 [US3] Implement DiscoverRules() method using reflection to find IAnalyzerRule types in AnalyzerEngine assembly
- [ ] T020 [US3] Implement CreateDescriptors() method using RuleDescriptorFactory.Create() for each rule
- [ ] T021 [US3] Implement SupportedDiagnostics property returning ImmutableArray of descriptors
- [ ] T022 [US3] Add static initialization: _rules = DiscoverRules(), _descriptors = CreateDescriptors(_rules)
- [ ] T023 [US3] Create _descriptorMap dictionary for fast rule ID → descriptor lookup
- [ ] T024 [US3] Verify rule discovery finds exactly 8 rules (LNT001-LNT008)

**Checkpoint**: Analyzer discovers all rules, descriptors created with metadata

---

## Phase 4: User Story 1 - Build-Time Analysis (Priority: P1) 🎯 MVP Functionality

**Goal**: Execute all 8 Lintelligent rules during Roslyn analysis pass, report diagnostics to IDE Error List and build output

**Independent Test**: Build project with code violating LNT001 (long method), verify diagnostic appears in IDE Error List

### Tests for User Story 1

- [ ] T025 [P] [US1] Create AllRulesIntegrationTests.cs in tests/Lintelligent.Analyzers.Tests/Integration/
- [ ] T026 [P] [US1] Write test: Analyze_MethodWith30Statements_ProducesLNT001Diagnostic
- [ ] T027 [P] [US1] Write test: Analyze_MethodWith6Parameters_ProducesLNT002Diagnostic
- [ ] T028 [P] [US1] Write test: Analyze_NestedConditionalDepth4_ProducesLNT003Diagnostic
- [ ] T029 [P] [US1] Write test: Analyze_MagicNumber_ProducesLNT004Diagnostic
- [ ] T030 [P] [US1] Write test: Analyze_GodClass_ProducesLNT005Diagnostic
- [ ] T031 [P] [US1] Write test: Analyze_UnusedPrivateMethod_ProducesLNT006Diagnostic
- [ ] T032 [P] [US1] Write test: Analyze_EmptyCatchBlock_ProducesLNT007Diagnostic
- [ ] T033 [P] [US1] Write test: Analyze_MissingXmlDoc_ProducesLNT008Diagnostic

### Implementation for User Story 1

- [ ] T034 [US1] Implement Initialize() method in LintelligentDiagnosticAnalyzer
- [ ] T035 [US1] Call context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None) to skip generated code
- [ ] T036 [US1] Call context.EnableConcurrentExecution() for parallel analysis
- [ ] T037 [US1] Call context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree) to register analysis callback
- [ ] T038 [US1] Implement AnalyzeSyntaxTree() method signature
- [ ] T039 [US1] Add foreach loop over _rules in AnalyzeSyntaxTree()
- [ ] T040 [US1] For each rule: call rule.Analyze(context.Tree) to get DiagnosticResult[]
- [ ] T041 [US1] For each DiagnosticResult: lookup descriptor from _descriptorMap
- [ ] T042 [US1] For each DiagnosticResult: call DiagnosticConverter.Convert() to create Roslyn Diagnostic
- [ ] T043 [US1] For each Diagnostic: call context.ReportDiagnostic() to report to Roslyn
- [ ] T044 [US1] Add try-catch around rule execution with error logging (ReportInternalError helper)
- [ ] T045 [US1] Implement ReportInternalError() method (creates LINT999 diagnostic for analyzer errors)
- [ ] T046 [US1] Run all integration tests, verify diagnostics reported with correct IDs and messages

**Checkpoint**: All 8 rules execute during build, diagnostics visible in IDE

---

## Phase 5: User Story 4 - Diagnostic Location Mapping (Priority: P2)

**Goal**: Ensure IDE navigation works correctly (F8 to next diagnostic, Ctrl+Click jumps to exact line)

**Independent Test**: Build project with LNT005 (god class), double-click diagnostic in Error List, verify IDE jumps to class declaration line

### Tests for User Story 4

- [ ] T047 [P] [US4] Create DiagnosticConverterTests.cs in tests/Lintelligent.Analyzers.Tests/Unit/
- [ ] T048 [P] [US4] Write test: Convert_LineNumber10_CreatesLocationAtRoslynLine9 (1-indexed → 0-indexed)
- [ ] T049 [P] [US4] Write test: Convert_LineNumberOutOfRange_ClampsToFileLength
- [ ] T050 [P] [US4] Write test: Convert_EmptyFile_ReturnsLocationNone
- [ ] T051 [P] [US4] Write test: CreateLocation_FirstLine_ReturnsLine0
- [ ] T052 [P] [US4] Write test: CreateLocation_LastLine_ReturnsCorrectSpan

### Implementation for User Story 4

- [ ] T053 [US4] Enhance DiagnosticConverter.CreateLocation() to handle 1-indexed → 0-indexed conversion (lineNumber - 1)
- [ ] T054 [US4] Add bounds checking: clamp line number to [0, text.Lines.Count - 1]
- [ ] T055 [US4] Handle empty file edge case: return Location.None if Lines.Count == 0
- [ ] T056 [US4] Get TextLine from tree.GetText().Lines[roslynLine]
- [ ] T057 [US4] Create Location from Location.Create(tree, textLine.Span)
- [ ] T058 [US4] Verify DiagnosticConverter.Convert() passes correct location to Diagnostic.Create()
- [ ] T059 [US4] Run unit tests, verify location mapping correctness
- [ ] T060 [US4] Manual test: Build project with violation, F8 navigation jumps to correct line

**Checkpoint**: Diagnostic locations accurate, IDE navigation works

---

## Phase 6: User Story 2 - EditorConfig Rule Configuration (Priority: P2)

**Goal**: Allow developers to configure rule severity (Info/Warning/Error/None) per project via .editorconfig

**Independent Test**: Set `dotnet_diagnostic.LNT004.severity = error` in .editorconfig, build project with magic number, verify build fails

### Tests for User Story 2

- [ ] T061 [P] [US2] Create EditorConfigIntegrationTests.cs in tests/Lintelligent.Analyzers.Tests/Integration/
- [ ] T062 [P] [US2] Write test: Analyze_EditorConfigSeverityNone_SuppressesDiagnostic
- [ ] T063 [P] [US2] Write test: Analyze_EditorConfigSeverityError_ProducesErrorDiagnostic
- [ ] T064 [P] [US2] Write test: Analyze_EditorConfigSeverityWarning_ProducesWarningDiagnostic
- [ ] T065 [P] [US2] Write test: Analyze_EditorConfigSeveritySuggestion_ProducesInfoDiagnostic
- [ ] T066 [P] [US2] Write test: Analyze_NoEditorConfig_UsesDefaultSeverity

### Implementation for User Story 2

- [ ] T067 [US2] In AnalyzeSyntaxTree(), get AnalyzerConfigOptionsProvider from context.Options
- [ ] T068 [US2] For each rule, query: GetOptions(context.Tree).TryGetValue($"dotnet_diagnostic.{rule.Id}.severity", out var severity)
- [ ] T069 [US2] If severity found and IsSuppressed(severity) returns true, skip rule execution (continue to next rule)
- [ ] T070 [US2] If severity found and not suppressed, apply override: use FromEditorConfigSeverity() for effective severity
- [ ] T071 [US2] Update Diagnostic.Create() to use effective severity instead of default descriptor severity
- [ ] T072 [US2] Run EditorConfig integration tests, verify all severity levels work correctly
- [ ] T073 [US2] Manual test: Create .editorconfig with various severity settings, verify build behavior

**Checkpoint**: EditorConfig severity overrides functional, all levels supported

---

## Phase 7: User Story 5 - Roslyn Analyzer Metadata (Priority: P3)

**Goal**: Provide proper analyzer metadata (help links, categories, tags) for professional IDE integration

**Independent Test**: Hover over diagnostic in IDE, verify tooltip shows help URL linking to rules-documentation.md

### Tests for User Story 5

- [ ] T074 [P] [US5] Create RuleDescriptorFactoryTests.cs in tests/Lintelligent.Analyzers.Tests/Unit/
- [ ] T075 [P] [US5] Write test: Create_LNT001_ReturnsDescriptorWithCorrectHelpLink
- [ ] T076 [P] [US5] Write test: GetHelpLinkUri_AllRules_ReturnsValidUrlWithAnchor
- [ ] T077 [P] [US5] Write test: GetCustomTags_MaintainabilityCategory_IncludesMaintainabilityTag
- [ ] T078 [P] [US5] Write test: Create_AllRules_ReturnsDescriptorsWithCodeQualityTag

### Implementation for User Story 5

- [ ] T079 [P] [US5] Verify RuleDescriptorFactory includes all rule anchors in RuleAnchors dictionary (LNT001-LNT008)
- [ ] T080 [P] [US5] Update GitHub base URL in RuleDescriptorFactory (replace [ORG] placeholder with actual org)
- [ ] T081 [P] [US5] Verify DiagnosticDescriptor.HelpLinkUri format: base URL + # + anchor
- [ ] T082 [P] [US5] Verify DiagnosticDescriptor.CustomTags includes "CodeQuality" for all rules
- [ ] T083 [P] [US5] Verify category-specific tags added correctly (Maintainability, CodeSmell, Documentation)
- [ ] T084 [US5] Run RuleDescriptorFactory tests, verify metadata completeness
- [ ] T085 [US5] Manual test: Hover over diagnostic in IDE, verify help link clickable and navigates to docs

**Checkpoint**: All metadata present, help links functional

---

## Phase 8: NuGet Packaging & Distribution

**Purpose**: Build and validate NuGet package structure for distribution

**Duration**: ~20 minutes

- [ ] T086 Build NuGet package: dotnet pack src/Lintelligent.Analyzers/ -c Release
- [ ] T087 Verify package structure: inspect bin/Release/Lintelligent.Analyzers.1.0.0.nupkg
- [ ] T088 Verify analyzers/dotnet/cs/ contains Lintelligent.Analyzers.dll
- [ ] T089 Verify analyzers/dotnet/cs/ contains Lintelligent.AnalyzerEngine.dll (dependency included)
- [ ] T090 Verify lib/ directory is empty (IncludeBuildOutput=false)
- [ ] T091 Verify .nuspec metadata: developmentDependency=true, correct package ID/version/description
- [ ] T092 Create test console project: dotnet new console -n AnalyzerTestApp
- [ ] T093 Add local package source: dotnet nuget add source ./bin/Release --name LocalAnalyzers
- [ ] T094 Install package in test app: dotnet add AnalyzerTestApp package Lintelligent.Analyzers
- [ ] T095 Write code violating LNT001 in test app Program.cs
- [ ] T096 Build test app: dotnet build AnalyzerTestApp
- [ ] T097 Verify diagnostic appears in build output: "warning LNT001: Method 'X' has Y statements (max: 20)"
- [ ] T098 Test EditorConfig suppression in test app: add .editorconfig with dotnet_diagnostic.LNT001.severity = none
- [ ] T099 Rebuild test app, verify LNT001 diagnostic suppressed
- [ ] T100 Clean up test app, verify package uninstall

**Checkpoint**: NuGet package builds correctly, installs successfully, analyzers execute

---

## Phase 9: Performance & Edge Case Testing

**Purpose**: Validate performance requirements (<10% build overhead) and edge case handling

**Duration**: ~1 hour

- [ ] T101 [P] Create PerformanceTests.cs in tests/Lintelligent.Analyzers.Tests/Integration/
- [ ] T102 [P] Create MultiTargetingTests.cs in tests/Lintelligent.Analyzers.Tests/Integration/
- [ ] T103 [P] Write test: Analyze_100FilesSolution_AddsLessThan2SecondsOverhead (measure baseline build time, then with analyzer, verify delta <2s per SC-003)
- [ ] T104 [P] Write test: Analyze_GeneratedCodeFile_SkipsAnalysis
- [ ] T105 [P] Write test: Analyze_PartialClassAcrossFiles_AnalyzesIndependently
- [ ] T106 [P] Write test: Analyze_RuleThrowsException_ReportsInternalErrorContinuesAnalysis
- [ ] T107 [P] Write test: Analyze_MultiTargetFramework_RunsForAllTFMs
- [ ] T108 Create 100-file test solution for performance benchmarking
- [ ] T109 Measure baseline build time (without analyzer)
- [ ] T110 Measure build time with analyzer enabled
- [ ] T111 Calculate overhead percentage, verify <10%
- [ ] T112 If overhead >10%, profile with dotnet build /clp:PerformanceSummary
- [ ] T113 Test generated code skip: create .g.cs file, verify no diagnostics
- [ ] T114 Test multi-targeting: create project with <TargetFrameworks>net8.0;net472</TargetFrameworks>, verify analyzer runs for both
- [ ] T115 Test exception handling: inject failing rule, verify LINT999 error reported
- [ ] T116 Run all edge case tests, verify graceful degradation

**Checkpoint**: Performance requirements met, edge cases handled correctly

---

## Phase 10: Polish & Documentation

**Purpose**: Final quality improvements, documentation, and cross-cutting concerns

**Duration**: ~30 minutes

- [ ] T117 [P] Update README.md with analyzer installation instructions
- [ ] T118 [P] Create ANALYZER_GUIDE.md in specs/019-roslyn-analyzer-bridge/ documenting EditorConfig usage
- [ ] T119 [P] Add XML documentation comments to all public APIs (LintelligentDiagnosticAnalyzer, RuleDescriptorFactory, etc.)
- [ ] T120 [P] Add code examples to XML docs showing analyzer usage
- [ ] T121 Run dotnet build --configuration Release across entire solution
- [ ] T122 Run dotnet test across entire solution, verify all 155 + new analyzer tests pass
- [ ] T123 Verify no compiler warnings in Lintelligent.Analyzers project
- [ ] T124 Run code coverage report, verify ≥95% coverage for new code
- [ ] T125 Update .github/agents/copilot-instructions.md if needed (already done in planning)
- [ ] T126 Create release notes for Lintelligent.Analyzers 1.0.0 package
- [ ] T127 Final constitutional compliance check (all principles still satisfied)
- [ ] T128 Git commit all changes with message: "feat: Add Roslyn analyzer bridge (Feature 019)"

**Checkpoint**: Feature complete, all tests passing, ready for merge

---

## Task Summary

**Total Tasks**: 128

**By Phase**:
- Phase 1 (Setup): 7 tasks
- Phase 2 (Foundational): 5 tasks
- Phase 3 (US3 - NuGet Distribution): 12 tasks
- Phase 4 (US1 - Build-Time Analysis): 22 tasks
- Phase 5 (US4 - Location Mapping): 14 tasks
- Phase 6 (US2 - EditorConfig): 13 tasks
- Phase 7 (US5 - Metadata): 12 tasks
- Phase 8 (Packaging): 15 tasks
- Phase 9 (Performance): 16 tasks
- Phase 10 (Polish): 12 tasks

**Parallelization Opportunities**: 42 tasks marked [P] (33% parallelizable)

**User Story Coverage**:
- US1 (Build-Time Analysis): 22 tasks ✅
- US2 (EditorConfig): 13 tasks ✅
- US3 (NuGet Distribution): 12 tasks ✅
- US4 (Location Mapping): 14 tasks ✅
- US5 (Metadata): 12 tasks ✅

**Test Coverage**: 38 test tasks (30% of total) - validates all user stories

---

## Dependency Graph

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational - adapters)
    ↓
Phase 3 (US3 - Analyzer core + NuGet setup)
    ↓
Phase 4 (US1 - Analysis execution) ← PRIMARY MVP
    ↓
Phase 5 (US4 - Location mapping)
    ↓
Phase 6 (US2 - EditorConfig)
    ↓
Phase 7 (US5 - Metadata polish)
    ↓
Phase 8 (Packaging validation)
    ↓
Phase 9 (Performance & edge cases)
    ↓
Phase 10 (Documentation & release)
```

**Critical Path**: Phase 1 → Phase 2 → Phase 3 → Phase 4 (MVP complete at T046)

**Independent Stories** (after Phase 4):
- Phase 5, 6, 7 can be implemented in any order
- Phase 8-10 sequential (final validation)

---

## Parallel Execution Strategy

**Phase 1-2**: Sequential (foundational setup)

**Phase 3 (US3)**: 
- T013-T016 (test setup) parallel with T017-T024 (analyzer skeleton)

**Phase 4 (US1)**:
- T025-T033 (tests) fully parallel (9 independent test files)
- T034-T046 (implementation) sequential (modifies same file)

**Phase 5 (US4)**:
- T047-T052 (tests) fully parallel (6 independent test methods)
- T053-T060 (implementation) sequential

**Phase 6 (US2)**:
- T061-T066 (tests) fully parallel (6 test methods)
- T067-T073 (implementation) sequential

**Phase 7 (US5)**:
- T074-T078 (tests) fully parallel (5 test methods)
- T079-T085 (implementation) mostly parallel (independent verification tasks)

**Phase 8-10**: Mix of parallel (documentation, testing) and sequential (builds, validation)

**Maximum Parallelism**: 9 concurrent tasks (Phase 4 tests)

---

## MVP Scope

**Minimum Viable Product** = Phases 1-4 complete (T001-T046)

**Delivers**:
- ✅ Analyzer project setup and NuGet packaging
- ✅ All 8 rules discovered and executed
- ✅ Diagnostics reported to IDE Error List
- ✅ Basic location mapping (whole line)
- ✅ NuGet package installable via `dotnet add package`

**Deferred to Post-MVP**:
- Precise location mapping (Phase 5)
- EditorConfig configuration (Phase 6)
- Metadata polish (Phase 7)
- Performance optimization (Phase 9)

**MVP Completion**: ~4 hours (Phases 1-4)
**Full Feature**: ~8-12 hours (all phases)

---

## Implementation Strategy

**Recommended Approach**: TDD (Test-Driven Development)

1. **Write tests first** (all [P] test tasks can run in parallel)
2. **Verify tests fail** (red)
3. **Implement minimum code** to pass tests (green)
4. **Refactor** for quality (clean)
5. **Move to next task**

**Checkpoint Validation**: After each phase, run:
- `dotnet build` (verify compilation)
- `dotnet test` (verify tests pass)
- Manual smoke test (verify analyzer works in IDE)

**Risk Mitigation**: 
- Foundational phase (Phase 2) blocks all stories - prioritize correctness over speed
- Performance validation (Phase 9) may reveal optimization needs - budget extra time
- NuGet packaging (Phase 8) may require .nuspec tweaks - test early and often
