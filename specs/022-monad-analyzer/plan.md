# Implementation Plan: Language-Ext Monad Detection Analyzer

**Branch**: `022-monad-analyzer` | **Date**: December 26, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/022-monad-analyzer/spec.md`

## Summary

Implement an opt-in analyzer that detects opportunities to use functional monads from the language-ext C# library. The analyzer will suggest replacing nullable types with Option<T>, try/catch blocks with Either<L, R>, and sequential validation with Validation<T>. Each suggestion includes educational explanations with before/after code examples to help developers learn functional programming patterns.

**Technical Approach**: Extend the existing Roslyn analyzer bridge (Feature 019) with new IAnalyzerRule implementations that perform semantic analysis to detect nullable patterns, exception handling, and validation sequences. Configuration is handled via EditorConfig (`language_ext_monad_detection = true`) to make the feature opt-in by default.

## Technical Context

**Language/Version**: C# 12, .NET 10.0 (target framework for implementation), netstandard2.0 (Roslyn analyzer compatibility)  
**Primary Dependencies**: Roslyn SDK (Microsoft.CodeAnalysis 4.12.0), language-ext (detection target, not analyzer dependency)  
**Storage**: N/A (stateless rules)  
**Testing**: xUnit 2.10.3, FluentAssertions 7.1.1 (existing test infrastructure)  
**Target Platform**: Cross-platform (.NET CLI, Visual Studio, Rider, VS Code)
**Project Type**: Library (analyzer rules) + NuGet package distribution  
**Performance Goals**: <10% overhead when enabled, <100ms per file analysis  
**Constraints**: Roslyn analyzer must target netstandard2.0, rules must be stateless/deterministic  
**Scale/Scope**: 4 monad detection rules (Option, Either, Validation, Try), ~1500 LOC total

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Layered Architecture**: Rules will be in `Lintelligent.AnalyzerEngine.Rules` namespace, consumed by Roslyn analyzer bridge in `Lintelligent.Analyzers`. Respects CLI → AnalyzerEngine/Analyzers → Rules hierarchy.
- [x] **DI Boundaries**: No dependency injection needed. Rules are stateless classes, instantiated directly by analyzer discovery mechanism.
- [x] **Rule Contracts**: All monad detection rules implement `IAnalyzerRule`, are stateless (no fields), deterministic (same input → same output), and return `IEnumerable<DiagnosticResult>`.
- [x] **Explicit Execution**: Analyzer runs within Roslyn's compilation pipeline (not CLI-specific). No background services or long-running processes.
- [x] **Testing Discipline**: Rules can be tested in isolation with `CSharpSyntaxTree.ParseText()` without full compilation or DI container.
- [x] **Determinism**: Syntax/semantic analysis is deterministic. Same code structure → same diagnostics.
- [x] **Extensibility**: Rules use stable `IAnalyzerRule` contract. EditorConfig opt-in ensures backward compatibility (no impact when disabled).

*Violations*: **NONE**

## Project Structure

### Documentation (this feature)

```text
specs/022-monad-analyzer/
├── plan.md              # This file
├── spec.md              # User scenarios and requirements
├── research.md          # Phase 0 research findings
├── data-model.md        # Rule metadata and diagnostic formats
├── contracts/           # Phase 1 API contracts
│   ├── IMonadPattern.cs           # Abstraction for detected monad opportunities
│   └── MonadDetectionOptions.cs   # Configuration model
├── quickstart.md        # Usage examples
└── checklists/          # Quality gates
    ├── requirements.md  # Spec validation
    ├── design.md        # Phase 1 design review
    └── implementation.md # Phase 2 code review
```

### Source Code (repository root)

```text
src/Lintelligent.AnalyzerEngine/
└── Rules/
    ├── Monad/                                # NEW: Monad detection rules
    │   ├── NullableToOptionRule.cs           # LNT200: Option<T> suggestions
    │   ├── TryCatchToEitherRule.cs           # LNT201: Either<L, R> suggestions
    │   ├── SequentialValidationRule.cs       # LNT202: Validation<T> suggestions
    │   └── TryMonadDetectionRule.cs          # LNT203: Try<T> suggestions (optional)
    └── (existing rules: LongMethodRule, etc.)

src/Lintelligent.Analyzers/
└── LintelligentDiagnosticAnalyzer.cs         # MODIFIED: Add EditorConfig check

tests/Lintelligent.AnalyzerEngine.Tests/
└── Rules/
    └── Monad/                                # NEW: Rule tests
        ├── NullableToOptionRuleTests.cs
        ├── TryCatchToEitherRuleTests.cs
        ├── SequentialValidationRuleTests.cs
        └── TryMonadDetectionRuleTests.cs

tests/Lintelligent.Analyzers.Tests/
└── Integration/
    └── MonadDetectionIntegrationTests.cs     # NEW: EditorConfig integration tests
```

**Structure Decision**: Extends existing analyzer rule infrastructure. No new projects needed.  
Monad detection rules are isolated in `Rules/Monad/` subdirectory for organizational clarity.  
Integration with existing Roslyn analyzer bridge (LintelligentDiagnosticAnalyzer) via rule discovery.

## Complexity Tracking

*No constitutional violations - section not applicable.*

## Phase 0: Research & Discovery

**Objective**: Resolve all NEEDS CLARIFICATION markers from Technical Context. Research language-ext patterns and Roslyn semantic analysis APIs.

### Research Tasks

1. **language-ext Type System Analysis**
   - **Task**: Document language-ext monad type signatures (Option<T>, Either<L, R>, Validation<T>, Try<T>)
   - **Why**: Need to recognize these types in semantic analysis to avoid false positives on existing monad usage
   - **Output**: Type name mappings in research.md (e.g., `LanguageExt.Option<T>`, `LanguageExt.Common.Result<T>`)

2. **Roslyn Semantic Model API Research**
   - **Task**: Identify APIs for detecting nullable types, try/catch blocks, and sequential validation patterns
   - **Why**: Must understand how to traverse syntax trees and query semantic model for type information
   - **Output**: Code examples in research.md showing:
     - Detecting nullable return types (`SemanticModel.GetTypeInfo().Type.NullableAnnotation`)
     - Finding try/catch statements (`SyntaxNode.DescendantNodes().OfType<TryStatementSyntax>()`)
     - Identifying validation chains (sequential if statements with error returns)

3. **NuGet Package Reference Detection**
   - **Task**: Research how to check if language-ext is referenced in a project from a Roslyn analyzer
   - **Why**: FR-008 requires checking package references before reporting diagnostics
   - **Output**: API pattern in research.md (likely `Compilation.ReferencedAssemblyNames` or `MetadataReference`)

4. **EditorConfig Integration Pattern**
   - **Task**: Document how to read custom EditorConfig settings from Roslyn analyzer context
   - **Why**: FR-001 requires opt-in via `language_ext_monad_detection = true`
   - **Output**: Code example in research.md using `AnalyzerConfigOptions.TryGetValue()`

5. **Diagnostic Message Best Practices**
   - **Task**: Research Roslyn diagnostic message formatting for educational content (FR-006, FR-007)
   - **Why**: Need to include multi-line explanations with code examples in diagnostic descriptions
   - **Output**: Examples in research.md showing how to format before/after code blocks in diagnostic messages

### Research Deliverables

- `research.md` with findings for all 5 tasks above
- Decision: Which language-ext types to detect (confirm Option, Either, Validation, Try)
- Decision: Minimum complexity thresholds (FR-013) - e.g., "only suggest Option for methods with 3+ null checks"

## Phase 1: Design & Contracts

**Objective**: Design rule contracts, diagnostic message templates, and EditorConfig integration.

### Design Tasks

1. **Data Model** (`data-model.md`)
   - Diagnostic IDs and severity levels
     ```
     LNT200: Nullable → Option<T> (Info severity)
     LNT201: Try/Catch → Either<L, R> (Info severity)
     LNT202: Sequential Validation → Validation<T> (Info severity)
     LNT203: Exception-based flow → Try<T> (Info severity, optional)
     ```
   - Diagnostic message templates with placeholders for code examples
   - Configuration schema: `language_ext_monad_detection`, `language_ext_min_complexity`

2. **API Contracts** (`contracts/`)
   - **IMonadPattern.cs**: Abstract pattern detection interface
     ```csharp
     public interface IMonadPattern
     {
         MonadType Type { get; } // Option, Either, Validation, Try
         string CurrentCode { get; }  // Before snippet
         string SuggestedCode { get; } // After snippet with monad
         string Explanation { get; } // Educational text
     }
     ```
   - **MonadDetectionOptions.cs**: Configuration model
     ```csharp
     public record MonadDetectionOptions(
         bool Enabled,
         int MinComplexity, // Minimum null checks / error paths
         HashSet<MonadType> EnabledTypes
     );
     ```

3. **Quick Start** (`quickstart.md`)
   - Example .editorconfig setup
   - Example code showing before/after for each monad type
   - How to disable specific monad types if needed

### Design Deliverables

- `data-model.md` with diagnostic specifications
- `contracts/IMonadPattern.cs` (pattern detection abstraction)
- `contracts/MonadDetectionOptions.cs` (configuration model)
- `quickstart.md` with usage examples
- Updated Constitution Check (confirm no violations after design)

## Phase 2: Implementation Planning (This Phase)

**Objective**: Generate task breakdown by user story. Define test-first implementation order.

**Note**: This section documents the plan structure. Actual task generation happens via `/speckit.tasks` command.

### Implementation Strategy

**Priority Order** (matches spec user story priorities):
1. **US1 (P1)**: EditorConfig opt-in integration
2. **US5 (P2)**: Diagnostic message templates with educational content
3. **US2 (P2)**: NullableToOptionRule implementation
4. **US3 (P3)**: TryCatchToEitherRule implementation
5. **US4 (P4)**: SequentialValidationRule implementation

**Test-First Workflow** (per rule):
1. Write failing unit tests with expected diagnostics
2. Implement rule to make tests pass
3. Add integration test with EditorConfig enabled/disabled
4. Performance test (ensure <100ms per file)

### Task Breakdown Structure

Tasks will be organized by user story in `tasks.md` (generated by `/speckit.tasks`):

```markdown
# Tasks: Language-Ext Monad Detection Analyzer

## US1: Enable Monad Detection for Codebase
- [ ] Task 1.1: Add EditorConfig setting reader to LintelligentDiagnosticAnalyzer
- [ ] Task 1.2: Skip monad rules when setting is false or missing
- [ ] Task 1.3: Integration test: Enabled via EditorConfig
- [ ] Task 1.4: Integration test: Disabled by default

## US2: Detect Nullable to Option Opportunities
- [ ] Task 2.1: Unit test: Nullable return type detected
- [ ] Task 2.2: Unit test: Multiple null checks trigger suggestion
- [ ] Task 2.3: Implement NullableToOptionRule.Analyze()
- [ ] Task 2.4: Unit test: Option<T> usage not flagged
- [ ] Task 2.5: Integration test: Full diagnostic with explanation

## US3: Detect Try/Catch to Either Opportunities
[Similar structure]

## US4: Detect Sequential Validation to Validation<T>
[Similar structure]

## US5: Provide Educational Explanations
- [ ] Task 5.1: Create diagnostic message template helper
- [ ] Task 5.2: Add before/after code examples for each monad type
- [ ] Task 5.3: Unit test: Message includes explanation + example
```

### Testing Strategy

**Unit Tests** (per rule):
- Positive cases: Detect patterns that should trigger diagnostics
- Negative cases: Code already using monads, simple cases below complexity threshold
- Edge cases: Async methods, nested try/catch, complex validation chains

**Integration Tests**:
- EditorConfig enabled → diagnostics appear
- EditorConfig disabled → no diagnostics
- language-ext not referenced → no diagnostics
- Performance: 1000-file project completes in <10% overhead

**Test Coverage Target**: 95%+ for new monad detection rules

### Performance Considerations

- **Lazy Evaluation**: Use `yield return` in rule implementations
- **Semantic Model Caching**: Roslyn caches compilation-level semantic info
- **Complexity Thresholds**: FR-013 prevents noise on trivial cases
- **Benchmark**: Measure analysis time with/without monad detection enabled

## Exit Criteria

Before merging to main:

- [x] Constitution Check passed (Phase 1)
- [ ] All research tasks completed (Phase 0)
- [ ] Data model documented (Phase 1)
- [ ] Contracts defined (Phase 1)
- [ ] Quick start guide written (Phase 1)
- [ ] All unit tests passing (implementation)
- [ ] Integration tests passing (EditorConfig, package reference check)
- [ ] Performance tests passing (<10% overhead)
- [ ] Code review completed
- [ ] Documentation updated (README, roadmap)

## Notes

- **Dependency on Feature 019**: Requires Roslyn analyzer bridge infrastructure
- **Dependency on Feature 007**: EditorConfig integration pattern (if implemented)
- **Out of Scope**: Code fixes (auto-refactoring) - deferred to future iteration
- **Risk**: Complexity of semantic analysis for validation patterns may require advanced heuristics

---

---

## Constitution Re-Check (Post-Design)

**Evaluation Date**: 2024-12-26  
**Phase**: After Phase 1 Design (research.md, data-model.md, contracts/, quickstart.md complete)

### Re-Evaluation Against Core Principles

**I. Layered Architecture (COMPLIANT ✅)**
- Design maintains clean layer separation:
  - `IMonadPattern` contract in AnalyzerEngine layer
  - `MonadDetectionOptions` configuration model (no cross-layer violations)
  - Rules implement `IAnalyzerRule` (established contract)
  - No circular dependencies introduced

**II. DI Boundaries (COMPLIANT ✅)**
- No DI required:
  - `MonadDetectionOptions.Parse()` is static method (no services)
  - Rules are stateless (no constructor dependencies)
  - EditorConfig read via Roslyn `AnalyzerConfigOptions` (framework-provided)

**III. Rule Contracts (COMPLIANT ✅)**
- All monad rules implement `IAnalyzerRule`:
  - `NullableToOptionRule`, `TryCatchToEitherRule`, `SequentialValidationRule`, `TryMonadDetectionRule`
  - Contracts defined: `IMonadPattern` (pattern abstraction), `MonadDetectionOptions` (config model)
  - No deviations from established rule interface

**IV. Explicit Execution (COMPLIANT ✅)**
- Execution model unchanged:
  - Rules discovered via reflection (existing AnalyzerManager pattern)
  - Invoked by `LintelligentDiagnosticAnalyzer` (Roslyn pipeline)
  - EditorConfig check happens before rule execution (explicit opt-in)

**V. Testing (COMPLIANT ✅)**
- Design supports unit testing:
  - Rules can be tested with `CSharpSyntaxTree.ParseText()` (no compilation required)
  - `MonadDetectionOptions.Parse()` is testable with mock `AnalyzerConfigOptions`
  - Integration tests can use .editorconfig files in test fixtures

**VI. Determinism (COMPLIANT ✅)**
- All rules remain deterministic:
  - Same code → same syntax tree → same diagnostics
  - No randomness, no external API calls
  - Configuration is file-based (EditorConfig), not runtime-dynamic

**VII. Extensibility (COMPLIANT ✅)**
- Design preserves stability:
  - `IAnalyzerRule` contract unchanged
  - New contracts (`IMonadPattern`, `MonadDetectionOptions`) are additive
  - Future monad types can be added without breaking changes (enum extension)

### Violations

**NONE** - All 7 constitutional principles satisfied after design phase.

### Design Decision Rationale

1. **EditorConfig Integration**: Uses Roslyn's built-in `AnalyzerConfigOptions` (no custom configuration infrastructure needed)
2. **Stateless Rules**: No state stored in rule instances (pure functions over syntax trees)
3. **Contract Abstraction**: `IMonadPattern` enables future code fix generation without changing rule interface
4. **Lazy Evaluation**: Rules only execute when EditorConfig enables them (performance consideration)

**Status**: Constitution re-check complete. No violations. Design approved for implementation.

---

**Implementation Plan Complete**

This plan documents:
- ✅ Phase 0 Research: language-ext types, Roslyn APIs, EditorConfig integration patterns
- ✅ Phase 1 Design: Diagnostic specifications, contracts, configuration schema, quick start guide
- ✅ Phase 2 Planning: Implementation strategy, task structure, testing approach

**Next Steps**:
1. Run `/speckit.tasks` to generate task breakdown (tasks.md)
2. Implement per task order (US1 → US5 → US2 → US3 → US4)
3. Test-first workflow for each rule

**Branch**: 022-monad-analyzer  
**Plan Location**: specs/022-monad-analyzer/plan.md  
**Artifacts Generated**:
- research.md (Phase 0)
- data-model.md (Phase 1)
- contracts/IMonadPattern.cs (Phase 1)
- contracts/MonadDetectionOptions.cs (Phase 1)
- quickstart.md (Phase 1)
- .github/agents/copilot-instructions.md (updated)

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
