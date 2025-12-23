# Research: Enhanced Rule Contract

**Feature**: 002-rule-contract-enhancement  
**Date**: 2025-12-23  
**Status**: Phase 0 Complete

## Overview

This document consolidates research for enhancing the `IAnalyzerRule` contract to support severity metadata, categorization, and multiple findings per file. All design decisions are informed by the clarification session documented in [spec.md](spec.md#clarifications).

## Research Questions & Decisions

### 1. Interface Design for Multiple Findings

**Question**: How should IAnalyzerRule expose multiple findings while maintaining backward compatibility for AnalyzerEngine consumers?

**Decision**: Change return type from `DiagnosticResult?` to `IEnumerable<DiagnosticResult>`

**Rationale**:
- **Lazy Evaluation**: IEnumerable allows streaming/deferred execution, preventing memory overhead for large result sets (FR-003, clarification Q3)
- **Consistency**: Aligns with Feature 001 streaming architecture (ICodeProvider returns IEnumerable<SyntaxTree>)
- **Null Safety**: Empty collection is clearer than null (FR-010) - `Enumerable.Empty<DiagnosticResult>()` instead of `null`
- **Constitutional Alignment**: Maintains determinism (Principle VII) - iteration order is stable

**Alternatives Considered**:
- `List<DiagnosticResult>`: Rejected - forces eager evaluation, violates streaming principle
- `DiagnosticResult[]`: Rejected - same memory concerns as List, less composable than IEnumerable
- `IAsyncEnumerable<DiagnosticResult>`: Rejected - analysis is CPU-bound, async adds complexity without benefit

**Implementation Note**: Rules yield findings using `yield return`, AnalyzerEngine consumes via `.ToList()` only when materialization needed.

---

### 2. Severity Enum Design

**Question**: Should Severity be an enum, interface, or extensible hierarchy?

**Decision**: Fixed enum with Error/Warning/Info values only (no Unknown, no extensibility)

**Rationale**:
- **Clarity**: Explicit categorization prevents lazy "Unknown" defaults (FR-014, clarification Q5)
- **Filtering Logic**: Simple enum enables efficient LINQ filtering (`results.Where(r => r.Severity == Severity.Error)`)
- **Constitutional Alignment**: Determinism (Principle VII) - no ambiguity in severity interpretation
- **Versioning**: Future severity levels added via minor version bump, not runtime extensibility

**Alternatives Considered**:
- Interface (ISeverity): Rejected - over-engineering, violates "boring and stable" principle (Constitution §VI)
- Enum with "Unknown": Rejected - permits lazy categorization, reduces value of metadata (clarification Q5)
- String-based: Rejected - no compile-time safety, typos lead to filtering failures

**Enum Definition**:
```csharp
public enum Severity
{
    Error = 0,   // Critical issues blocking release
    Warning = 1, // Should fix, non-blocking
    Info = 2     // Suggestions, informational
}
```

**Ordering**: Numeric values support sorting by severity (Error < Warning < Info for "most severe first").

---

### 3. Category Design

**Question**: Should Category be an enum or string?

**Decision**: String-based property (not enum)

**Rationale**:
- **Extensibility**: Third-party rule packs can define custom categories without recompiling core library (Constitution §VI)
- **Domain-Specific**: Different domains have different category vocabularies (e.g., "Security" for web apps, "Memory Safety" for embedded)
- **No Validation Needed**: Categories are for reporting/grouping, not business logic - invalid categories don't break analysis
- **Best Practices**: Microsoft.CodeAnalysis.DiagnosticDescriptor uses string Category property, not enum

**Alternatives Considered**:
- Enum: Rejected - prevents third-party extensibility, requires core library updates for new categories
- Interface: Rejected - unnecessary complexity for simple grouping metadata
- Pre-defined constants: Acceptable as guidance (e.g., `Categories.Maintainability`), but not enforced

**Implementation Note**: Provide `DiagnosticCategories` static class with common constants (Maintainability, Performance, Security, Style, Design) as guidance, but accept any string.

---

### 4. Metadata Flow to DiagnosticResult

**Question**: How should severity and category propagate from rule to diagnostic result?

**Decision**: DiagnosticResult constructor requires severity and category as parameters (explicit flow)

**Rationale**:
- **Type Safety**: Compiler enforces metadata provision at creation (clarification Q2)
- **Immutability**: DiagnosticResult remains a record (immutable after construction) - FR-009
- **No Magic**: Explicit parameters prevent forgotten metadata, clearer than implicit property copying
- **Testability**: Easy to construct test fixtures with specific metadata

**Alternatives Considered**:
- Auto-populate from rule: Rejected - requires DiagnosticResult to hold rule reference (coupling), breaks immutability
- Separate SetMetadata() method: Rejected - violates immutability, allows invalid intermediate states
- DiagnosticResult builder: Rejected - over-engineering for simple data structure

**Updated Constructor Signature**:
```csharp
public record DiagnosticResult(
    string FilePath,
    string RuleId,
    string Message,
    int LineNumber,
    Severity Severity,    // NEW
    string Category       // NEW
);
```

---

### 5. Rule Metadata Validation Strategy

**Question**: When should invalid rule metadata (empty ID, undefined severity) be detected?

**Decision**: Validate at registration time in AnalyzerManager (fail-fast)

**Rationale**:
- **Early Detection**: Fails during app startup, not mid-analysis (clarification Q4)
- **Better DX**: Clear error message when registering rule, not cryptic runtime failure
- **Constitutional Alignment**: Explicit execution model (Constitution §IV) - build phase validates, execute phase runs
- **No Partial State**: Prevents analysis from starting with invalid configuration

**Alternatives Considered**:
- Validate during analysis: Rejected - wastes user time running analysis before discovering config error
- Allow invalid metadata: Rejected - violates FR-013, reduces metadata usefulness
- Constructor validation: Acceptable as secondary defense, but registration-time is primary gate

**Validation Rules**:
```csharp
// In AnalyzerManager.RegisterRule()
if (string.IsNullOrWhiteSpace(rule.Id))
    throw new ArgumentException("Rule ID cannot be null or empty", nameof(rule));
if (!Enum.IsDefined(typeof(Severity), rule.Severity))
    throw new ArgumentException($"Rule {rule.Id} has undefined severity: {rule.Severity}", nameof(rule));
if (string.IsNullOrWhiteSpace(rule.Category))
    throw new ArgumentException($"Rule {rule.Id} has null/empty category", nameof(rule));
```

---

### 6. Exception Handling in AnalyzerEngine

**Question**: How should AnalyzerEngine handle rule exceptions during analysis?

**Decision**: Continue analysis, skip failing rule, collect exceptions for end-of-analysis reporting

**Rationale**:
- **Resilience**: Single buggy rule doesn't block entire analysis (clarification Q1)
- **Observability**: All exceptions collected and reported together, easier debugging than fail-fast
- **User Value**: Partial results better than no results - user gets findings from non-failing rules
- **Constitutional Alignment**: Determinism maintained (same rules fail consistently), explicit error reporting

**Alternatives Considered**:
- Fail entire analysis: Rejected - poor user experience, single bad rule blocks all work
- Silent skip: Rejected - hides bugs, violates observability principle
- Retry logic: Rejected - analysis is deterministic, retries won't help

**Implementation Pattern**:
```csharp
var exceptions = new List<Exception>();
foreach (var rule in rules)
{
    try
    {
        var findings = rule.Analyze(tree);
        results.AddRange(findings);
    }
    catch (Exception ex)
    {
        exceptions.Add(new RuleExecutionException(rule.Id, ex));
    }
}
// After all rules: report exceptions to user
```

---

### 7. Performance Impact of Multiple Findings

**Question**: Will emitting multiple findings per file degrade performance?

**Decision**: No significant impact - lazy IEnumerable evaluation prevents eager allocation

**Rationale**:
- **Streaming**: IEnumerable allows AnalyzerEngine to process findings as yielded, not batch-allocate
- **Benchmark Target**: ±10% variance acceptable (SC-005) - Feature 001 achieved 23K files/sec, target ≥20K files/sec
- **Memory Budget**: <50MB growth for 10K files maintained via streaming (same constraint as Feature 001)
- **Existing Architecture**: Feature 001 already uses streaming (ICodeProvider), this feature extends pattern to results

**Measurement Plan**:
- Benchmark rule emitting 5 findings/file vs 1 finding/file
- Measure memory growth using same methodology as Feature 001 (PerformanceAndComplianceTests.cs)
- Validate throughput remains ≥20K files/sec

**Optimization**: If performance degrades, use `yield return` in rules to defer allocation until enumeration.

---

### 8. Migration Path for Existing Rules

**Question**: How do we migrate LongMethodRule and future custom rules?

**Decision**: Update IAnalyzerRule interface (breaking change), provide migration guide in quickstart.md

**Rationale**:
- **Constitutional Requirement**: Principle III requires severity/category metadata - interface must enforce
- **Controlled Scope**: Only 1 existing rule (LongMethodRule), manageable migration effort
- **Semantic Versioning**: Breaking change justified - bump major version (2.0.0), document in changelog
- **Future-Proofing**: Clean contract now prevents technical debt later

**Migration Steps** (to be detailed in quickstart.md):
1. Add Severity and Category properties to rule class
2. Change Analyze return type from `DiagnosticResult?` to `IEnumerable<DiagnosticResult>`
3. Update Analyze implementation to `yield return` findings (or return `Enumerable.Empty<DiagnosticResult>()`)
4. Update DiagnosticResult construction to pass severity and category

**Example Migration**:
```csharp
// Before
public DiagnosticResult? Analyze(SyntaxTree tree) {
    var longMethod = FindFirstLongMethod(tree);
    return longMethod == null ? null : new DiagnosticResult(...);
}

// After
public Severity Severity => Severity.Warning;
public string Category => "Maintainability";

public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree) {
    foreach (var method in FindAllLongMethods(tree)) {
        yield return new DiagnosticResult(
            tree.FilePath, Id, $"Method too long: {method.Name}", method.Line,
            Severity, Category  // NEW: explicit metadata
        );
    }
}
```

---

## Best Practices from Microsoft.CodeAnalysis

**Research Source**: Roslyn DiagnosticAnalyzer API patterns

**Findings**:
1. **Severity Mapping**: Roslyn uses DiagnosticSeverity enum (Error, Warning, Info, Hidden) - we omit Hidden (not relevant for CLI tool)
2. **Category Strings**: Roslyn uses string Category in DiagnosticDescriptor, not enum - validates our decision
3. **Multiple Diagnostics**: Roslyn analyzers report multiple diagnostics via `context.ReportDiagnostic()` in loop - validates IEnumerable approach
4. **Metadata Immutability**: DiagnosticDescriptor is immutable - validates our FR-009 requirement
5. **Rule IDs**: Roslyn uses format "CA1234" (prefix + number) - we use "LongMethod" (descriptive) - both acceptable

**Adoption**: Use Roslyn patterns where applicable, deviate where CLI context differs (e.g., no "Hidden" severity for command-line tool).

---

## Technology Choices

### Core Technologies (Existing)
- **.NET 10.0**: Target framework (established in Feature 001)
- **Microsoft.CodeAnalysis.CSharp 4.12.0**: Roslyn for syntax analysis (existing dependency)
- **xUnit 2.9.3**: Test framework (existing)
- **FluentAssertions**: Test assertions (existing)

### New Dependencies
- **None**: Feature uses only existing dependencies

---

## Open Questions Resolved

| Question | Answer | Source |
|----------|--------|--------|
| Exception handling strategy | Continue analysis, collect exceptions | Clarification Q1 |
| Metadata flow mechanism | Constructor parameters | Clarification Q2 |
| Large findings handling | Streaming (IEnumerable) | Clarification Q3 |
| Metadata validation timing | Registration time (fail-fast) | Clarification Q4 |
| Severity extensibility | Fixed enum (Error/Warning/Info only) | Clarification Q5 |
| Performance impact | ±10% acceptable, streaming prevents overhead | Spec SC-005 |
| Backward compatibility | Breaking change to IAnalyzerRule, AnalyzerEngine API stable | Spec SC-006 |

**Status**: All NEEDS CLARIFICATION items resolved. Ready for Phase 1 design.

---

## References

- **Feature 001**: IO Boundary Refactor - established streaming patterns, performance benchmarks
- **Constitution**: Principles III (Rule Contract), VI (Extensibility), VII (Determinism)
- **Roslyn API**: Microsoft.CodeAnalysis.DiagnosticDescriptor, DiagnosticSeverity patterns
- **Specification**: [spec.md](spec.md) - functional requirements, clarifications, success criteria
