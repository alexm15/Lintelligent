# Implementation Plan: Core Rule Library

**Branch**: `005-core-rule-library` | **Date**: December 24, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-core-rule-library/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement 7 new code quality rules and enhance the existing LongMethodRule to provide comprehensive C# code smell detection. Rules detect: Long Parameter List (>5 parameters), Complex Conditional (nested if >3), Magic Numbers (hardcoded literals), God Class (>500 LOC or >15 methods), Dead Code (unused private members), Exception Swallowing (empty catch), and Missing XML Documentation (public APIs). Each rule is independently testable, provides actionable diagnostic messages, and adheres to Constitutional Principle III (stateless, deterministic, implements IAnalyzerRule). All rules integrate with existing AnalyzerEngine infrastructure without engine modifications.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: Microsoft.CodeAnalysis.CSharp (Roslyn), existing IAnalyzerRule interface, DiagnosticResult class, DiagnosticCategories constants  
**Storage**: N/A (rules are stateless, no persistence required)  
**Testing**: xUnit 2.9.3, FluentAssertions 6.8.0 (existing test infrastructure)  
**Target Platform**: Cross-platform .NET CLI tool (Windows/Linux/macOS)
**Project Type**: Single project (Lintelligent.AnalyzerEngine with new rules in Rules/ directory)  
**Performance Goals**: <500ms per rule for analyzing 1000-line file, maintain O(n) complexity relative to syntax tree size  
**Constraints**: Rules must be stateless and deterministic (Constitutional Principle III), no external dependencies beyond Roslyn, no DI usage (rules instantiated directly)  
**Scale/Scope**: 8 rules total (7 new: LongParameterListRule, ComplexConditionalRule, MagicNumberRule, GodClassRule, DeadCodeRule, ExceptionSwallowingRule, MissingXmlDocumentationRule; 1 enhanced: LongMethodRule)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Initial Check (Before Phase 0)**: ✅ ALL PASSED

**Post-Design Re-Check (After Phase 1)**: ✅ ALL PASSED

- [X] **Layered Architecture**: ✅ PASS - All rules reside in AnalyzerEngine/Rules layer, no dependencies on CLI or Reporting. Verified in data-model.md and contracts - all rules are stateless classes in Rules/ directory.
- [X] **DI Boundaries**: ✅ PASS - Rules are stateless classes instantiated directly (no DI), only CLI layer uses DI. Confirmed in quickstart.md - rules instantiated with `new LongParameterListRule()` etc.
- [X] **Rule Contracts**: ✅ PASS - All 8 rules implement IAnalyzerRule, are stateless (no mutable fields), deterministic (same input → same output). Detailed contracts in contracts/rule-contracts.md verify stateless design.
- [X] **Explicit Execution**: ✅ PASS - No CLI changes required, rules integrate with existing AnalyzerEngine execution model. Quickstart confirms no AnalyzerEngine modifications needed.
- [X] **Testing Discipline**: ✅ PASS - Each rule testable in isolation with synthetic syntax trees, no DI or full app required. Test pattern in quickstart uses CSharpSyntaxTree.ParseText() for in-memory testing.
- [X] **Determinism**: ✅ PASS - All rules use deterministic Roslyn syntax tree traversal, no randomness or time-based logic. Research confirms all Roslyn APIs are deterministic.
- [X] **Extensibility**: ✅ PASS - Uses existing IAnalyzerRule interface (stable public API), no breaking changes to DiagnosticResult or AnalyzerEngine. Two new DiagnosticCategories constants added (backward compatible).

*No violations. Feature fully compliant with constitutional principles after design phase.*

**Design Validation Notes**:
- All 8 rules follow existing LongMethodRule pattern (proven constitutional compliance)
- No new abstractions introduced - reuses IAnalyzerRule, DiagnosticResult, DiagnosticCategories
- Performance validated: <500ms per rule, <2s combined (research.md section 7)
- Test strategy validated: In-memory syntax trees, no external dependencies (quickstart.md Step 3)
- Integration pattern validated: AnalyzerEngine collects results from all rules (data-model.md data flow)

**Conclusion**: Design maintains full constitutional compliance. Ready for implementation.

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
│   ├── Rules/
│   │   ├── IAnalyzerRule.cs              (existing)
│   │   ├── LongMethodRule.cs             (existing, to be enhanced)
│   │   ├── LongParameterListRule.cs      (new - US1)
│   │   ├── ComplexConditionalRule.cs     (new - US2)
│   │   ├── MagicNumberRule.cs            (new - US3)
│   │   ├── GodClassRule.cs               (new - US4)
│   │   ├── DeadCodeRule.cs               (new - US5)
│   │   ├── ExceptionSwallowingRule.cs    (new - US6)
│   │   └── MissingXmlDocumentationRule.cs (new - US7)
│   ├── Results/
│   │   ├── DiagnosticResult.cs           (existing)
│   │   └── DiagnosticCategories.cs       (existing, may need new constants)
│   └── ...
├── Lintelligent.Cli/                     (no changes)
└── Lintelligent.Reporting/               (no changes)

tests/
├── Lintelligent.AnalyzerEngine.Tests/
│   ├── Rules/
│   │   ├── LongMethodRuleTests.cs             (existing)
│   │   ├── LongParameterListRuleTests.cs      (new - US1)
│   │   ├── ComplexConditionalRuleTests.cs     (new - US2)
│   │   ├── MagicNumberRuleTests.cs            (new - US3)
│   │   ├── GodClassRuleTests.cs               (new - US4)
│   │   ├── DeadCodeRuleTests.cs               (new - US5)
│   │   ├── ExceptionSwallowingRuleTests.cs    (new - US6)
│   │   └── MissingXmlDocumentationRuleTests.cs (new - US7)
│   └── ...
└── Lintelligent.Cli.Tests/               (no changes)
```

**Structure Decision**: Single project structure (Option 1). All rules reside in existing `Lintelligent.AnalyzerEngine/Rules/` directory. Each rule is a standalone class file implementing IAnalyzerRule. Tests mirror the production structure in `tests/Lintelligent.AnalyzerEngine.Tests/Rules/` directory. No changes to CLI or Reporting layers required - this feature is purely additive to the AnalyzerEngine core.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
