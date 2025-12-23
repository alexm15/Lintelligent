# Data Model: Enhanced Rule Contract

**Feature**: 002-rule-contract-enhancement  
**Date**: 2025-12-23  
**Status**: Phase 1

## Overview

This document defines the data model for the enhanced rule contract, including entities, relationships, validation rules, and state transitions. All entities are designed to maintain immutability and constitutional compliance.

---

## Core Entities

### 1. Severity (Enum)

**Purpose**: Represents the importance/impact level of a diagnostic finding.

**Definition**:
```csharp
namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
/// Severity level of a diagnostic finding.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Critical issues that block release (e.g., null reference errors, security vulnerabilities).
    /// </summary>
    Error = 0,

    /// <summary>
    /// Issues that should be fixed but are non-blocking (e.g., code smells, maintainability issues).
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Informational suggestions for improvement (e.g., style recommendations).
    /// </summary>
    Info = 2
}
```

**Properties**:
- **Type**: Enum (byte underlying type for memory efficiency)
- **Values**: Error (0), Warning (1), Info (2)
- **Immutable**: Enum values cannot be modified at runtime

**Validation Rules**:
- Must be one of defined values (validated via `Enum.IsDefined()`)
- No "Unknown" or default value - forces explicit categorization
- Numeric ordering supports sorting (Error < Warning < Info for descending severity)

**Usage**:
- Exposed as read-only property on `IAnalyzerRule`
- Passed to `DiagnosticResult` constructor for metadata propagation
- Used for filtering results in CLI and reporting layers

**Constraints**:
- Fixed set of values (no runtime extensibility)
- Future severity levels require library version bump (breaking/minor change)

---

### 2. IAnalyzerRule (Interface - Enhanced)

**Purpose**: Contract for all analysis rules, defining metadata and analysis behavior.

**Definition**:
```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

/// <summary>
/// Contract for code analysis rules. Rules must be stateless and deterministic.
/// </summary>
public interface IAnalyzerRule
{
    /// <summary>
    /// Unique identifier for the rule (e.g., "LongMethod", "CA1001").
    /// Must not be null or whitespace.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable description of what the rule checks.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Severity level of findings produced by this rule.
    /// Must be a defined Severity enum value.
    /// </summary>
    Severity Severity { get; }  // NEW

    /// <summary>
    /// Category for grouping related rules (e.g., "Maintainability", "Performance", "Security").
    /// Must not be null or whitespace. Use common constants from DiagnosticCategories when applicable.
    /// </summary>
    string Category { get; }  // NEW

    /// <summary>
    /// Analyzes a syntax tree and returns zero or more diagnostic findings.
    /// Must be deterministic (same input produces same output).
    /// Must not throw exceptions under normal operation.
    /// </summary>
    /// <param name="tree">The syntax tree to analyze.</param>
    /// <returns>
    /// Enumerable of diagnostic results. Return Enumerable.Empty() if no findings, never null.
    /// </returns>
    IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);  // CHANGED: was DiagnosticResult?
}
```

**Properties**:
- **Id**: Unique string identifier (non-null, non-empty)
- **Description**: Human-readable rule description
- **Severity**: Severity enum value (Error/Warning/Info)
- **Category**: Grouping category (string, non-null, non-empty)

**Method**:
- **Analyze(SyntaxTree)**: Returns `IEnumerable<DiagnosticResult>` (changed from `DiagnosticResult?`)

**Validation Rules**:
- `Id` must not be null, empty, or whitespace
- `Severity` must be defined enum value (`Enum.IsDefined(typeof(Severity), value)`)
- `Category` must not be null, empty, or whitespace
- `Analyze()` must return non-null enumerable (empty collection allowed, null not allowed)

**Relationships**:
- Implemented by concrete rule classes (e.g., `LongMethodRule`)
- Registered with `AnalyzerManager` for validation
- Produces `DiagnosticResult` entities via `Analyze()` method

**State Transitions**: N/A (rules are stateless, no internal state changes)

**Immutability**:
- Properties (Id, Description, Severity, Category) expected to be immutable after construction
- Enforced via readonly properties in implementing classes

---

### 3. DiagnosticResult (Record - Enhanced)

**Purpose**: Represents a single diagnostic finding from a rule at a specific code location.

**Definition**:
```csharp
namespace Lintelligent.AnalyzerEngine.Results;

/// <summary>
/// Represents a single diagnostic finding from an analyzer rule.
/// Immutable after construction.
/// </summary>
public record DiagnosticResult(
    string FilePath,
    string RuleId,
    string Message,
    int LineNumber,
    Severity Severity,    // NEW
    string Category       // NEW
);
```

**Properties**:
| Property | Type | Required | Description |
|----------|------|----------|-------------|
| FilePath | string | Yes | Absolute or relative path to analyzed file |
| RuleId | string | Yes | Identifier of rule that produced this finding (matches IAnalyzerRule.Id) |
| Message | string | Yes | Human-readable description of the finding |
| LineNumber | int | Yes | Line number where issue was found (1-based) |
| Severity | Severity | Yes | Severity level inherited from producing rule |
| Category | string | Yes | Category inherited from producing rule |

**Validation Rules**:
- All parameters required (no null values)
- `LineNumber` must be ≥ 1 (1-based line numbering)
- `Severity` must be defined enum value
- `Category` must not be null/empty (inherited from rule, already validated)
- `RuleId` must not be null/empty (inherited from rule, already validated)

**Relationships**:
- Produced by `IAnalyzerRule.Analyze()` method
- Contains metadata (Severity, Category) from producing rule
- Consumed by `AnalyzerEngine` for aggregation
- Consumed by `ReportGenerator` for formatting

**Immutability**:
- Implemented as C# record (structural equality, immutable after construction)
- No setters, no mutable collections

**Lifecycle**:
1. Rule creates DiagnosticResult during Analyze() call
2. AnalyzerEngine collects results from all rules
3. Results passed to reporting/CLI layers for output
4. No state changes after creation

---

### 4. DiagnosticCategories (Static Class - New)

**Purpose**: Provides common category constants for consistency across rules.

**Definition**:
```csharp
namespace Lintelligent.AnalyzerEngine.Results;

/// <summary>
/// Common diagnostic category constants. Rules may use custom categories if needed.
/// </summary>
public static class DiagnosticCategories
{
    /// <summary>Code maintainability issues (e.g., long methods, complex conditionals).</summary>
    public const string Maintainability = "Maintainability";

    /// <summary>Performance concerns (e.g., inefficient loops, excessive allocations).</summary>
    public const string Performance = "Performance";

    /// <summary>Security vulnerabilities (e.g., SQL injection, hardcoded secrets).</summary>
    public const string Security = "Security";

    /// <summary>Code style and formatting issues.</summary>
    public const string Style = "Style";

    /// <summary>Design pattern violations or architectural concerns.</summary>
    public const string Design = "Design";

    /// <summary>General issues not fitting other categories.</summary>
    public const string General = "General";
}
```

**Usage**:
- Optional guidance for rule implementers
- Not enforced (rules can use custom categories)
- Promotes consistency across built-in and third-party rules

**Extensibility**:
- Rules can define custom categories (e.g., "DomainLogic", "DataAccess")
- Category is string property (not enum) to allow extensibility

---

## Relationships

```
┌─────────────────────┐
│  IAnalyzerRule      │
│  ┌────────────────┐ │
│  │ Id: string     │ │
│  │ Description    │ │
│  │ Severity       │◄────┐
│  │ Category       │     │
│  └────────────────┘ │   │ Provides metadata
│         │            │   │
│         │ Analyze()  │   │
│         ▼            │   │
│  ┌────────────────┐ │   │
│  │ Returns:       │ │   │
│  │ IEnumerable<   │ │   │
│  │ DiagnosticRes> │─┼───┘
│  └────────────────┘ │
└─────────────────────┘
         │
         │ 0..*
         ▼
┌─────────────────────┐
│ DiagnosticResult    │
│ ┌────────────────┐  │
│ │ FilePath       │  │
│ │ RuleId         │  │
│ │ Message        │  │
│ │ LineNumber     │  │
│ │ Severity ◄─────┼──┼─ Inherited from rule
│ │ Category ◄─────┼──┼─ Inherited from rule
│ └────────────────┘  │
└─────────────────────┘
         │
         │ Consumed by
         ▼
┌─────────────────────┐
│ AnalyzerEngine      │
│ ReportGenerator     │
│ CLI (filtering)     │
└─────────────────────┘
```

**Cardinality**:
- 1 IAnalyzerRule → 0..* DiagnosticResult (one rule can produce many findings)
- 1 AnalyzerEngine → N IAnalyzerRule (engine orchestrates multiple rules)
- 1 DiagnosticResult → 1 Severity (every result has exactly one severity)
- 1 DiagnosticResult → 1 Category (every result has exactly one category)

---

## Validation Rules

### At Rule Registration (AnalyzerManager)

```csharp
public void RegisterRule(IAnalyzerRule rule)
{
    // FR-013: Validate metadata at registration time
    if (rule == null)
        throw new ArgumentNullException(nameof(rule));
    
    if (string.IsNullOrWhiteSpace(rule.Id))
        throw new ArgumentException("Rule ID cannot be null or empty", nameof(rule));
    
    if (!Enum.IsDefined(typeof(Severity), rule.Severity))
        throw new ArgumentException(
            $"Rule '{rule.Id}' has undefined severity: {rule.Severity}", 
            nameof(rule));
    
    if (string.IsNullOrWhiteSpace(rule.Category))
        throw new ArgumentException(
            $"Rule '{rule.Id}' has null or empty category", 
            nameof(rule));
    
    // Additional check: duplicate IDs
    if (_registeredRules.Any(r => r.Id == rule.Id))
        throw new InvalidOperationException(
            $"Rule with ID '{rule.Id}' is already registered");
    
    _registeredRules.Add(rule);
}
```

### At DiagnosticResult Creation

```csharp
// Enforced via constructor parameters (all required)
// Optional: Add validation in constructor if needed
public record DiagnosticResult(
    string FilePath,
    string RuleId,
    string Message,
    int LineNumber,
    Severity Severity,
    string Category)
{
    // Optional constructor validation
    public DiagnosticResult
    {
        if (LineNumber < 1)
            throw new ArgumentOutOfRangeException(
                nameof(LineNumber), 
                "Line number must be >= 1");
        
        if (!Enum.IsDefined(typeof(Severity), Severity))
            throw new ArgumentException(
                $"Undefined severity: {Severity}", 
                nameof(Severity));
    }
}
```

---

## State Transitions

### Rule Lifecycle

```
┌───────────────┐
│ Instantiated  │
│ (constructor) │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│  Registered   │ ◄─── Validation occurs here (FR-013)
│ (validated)   │
└───────┬───────┘
        │
        │ (multiple calls)
        ▼
┌───────────────┐
│  Analyzing    │ ◄─── Stateless: no internal state changes
│ (Analyze())   │      Same input → same output
└───────┬───────┘
        │
        │ Returns findings
        ▼
┌───────────────┐
│   Complete    │
│ (no cleanup)  │
└───────────────┘
```

**Notes**:
- Rules have no internal state transitions (stateless per Constitution Principle III)
- Analyze() can be called multiple times with different inputs
- No cleanup/disposal required (rules are pure logic)

### DiagnosticResult Lifecycle

```
┌───────────────┐
│   Created     │ ◄─── Constructor called by rule
│ (immutable)   │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│  Collected    │ ◄─── Added to AnalyzerEngine results
│ (enumerated)  │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│   Filtered    │ ◄─── CLI/Reporting applies severity filters
│ (LINQ queries)│
└───────┬───────┘
        │
        ▼
┌───────────────┐
│   Reported    │ ◄─── Formatted and displayed
│ (consumed)    │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│   Disposed    │ ◄─── Garbage collected (no manual cleanup)
└───────────────┘
```

**Notes**:
- DiagnosticResult is immutable (no state changes after creation)
- Filtering/grouping creates new collections, doesn't mutate originals
- No explicit disposal needed (simple value object)

---

## Migration Impact

### Existing Entity Changes

| Entity | Change Type | Breaking? | Migration Required |
|--------|-------------|-----------|-------------------|
| IAnalyzerRule | Added properties (Severity, Category) | Yes | Implement new properties in all rules |
| IAnalyzerRule.Analyze() | Return type changed | Yes | Change return type to IEnumerable |
| DiagnosticResult | Added constructor parameters | Yes | Update all creation sites to pass new params |
| AnalyzerEngine | Internal logic change | No | No external API changes for consumers |
| AnalyzerManager | Added validation logic | No | New behavior (fail-fast), no API change |

### New Entities

- **Severity** (enum): New public type
- **DiagnosticCategories** (static class): New public constants (optional)

**Versioning**: Breaking changes to IAnalyzerRule and DiagnosticResult require major version bump (2.0.0).

---

## Edge Cases

### Large Result Sets
- **Scenario**: Rule emits 1000+ findings for a single file
- **Handling**: IEnumerable allows lazy evaluation - AnalyzerEngine can stream/filter without materializing full list
- **Memory**: Bounded by materialization point (only when .ToList() called in reporting layer)

### No Findings
- **Scenario**: Rule finds no issues in a file
- **Handling**: Return `Enumerable.Empty<DiagnosticResult>()` (never null)
- **Validation**: AnalyzerEngine checks for null return and logs error if encountered

### Rule Exception During Analysis
- **Scenario**: Rule throws exception in Analyze() method
- **Handling**: AnalyzerEngine catches exception, logs error, continues with next rule (FR-011)
- **Result**: Partial results returned (findings from non-failing rules)

### Invalid Severity Value
- **Scenario**: Rule property returns undefined enum value (via unsafe cast)
- **Handling**: Caught at registration time via `Enum.IsDefined()` check
- **Failure Mode**: ArgumentException thrown, rule not registered

### Empty Category String
- **Scenario**: Rule returns empty string for Category property
- **Handling**: Caught at registration time via `string.IsNullOrWhiteSpace()` check
- **Failure Mode**: ArgumentException thrown, rule not registered

---

## Performance Considerations

### Memory Efficiency
- **Severity**: Enum uses 1 byte (byte underlying type) - minimal overhead
- **DiagnosticResult**: Record type - stack-allocated when possible, efficient allocation
- **IEnumerable**: Lazy evaluation prevents allocating large arrays upfront

### Computational Efficiency
- **Validation**: Registration-time validation (one-time cost at startup)
- **Filtering**: LINQ `Where()` on IEnumerable - O(n) with lazy evaluation
- **Sorting**: Severity enum numeric values support efficient sorting

### Benchmarks (Target)
- **Throughput**: ≥20,000 files/sec (±10% from Feature 001 baseline of 23K files/sec)
- **Memory Growth**: <50MB for 10,000 files analyzed
- **Validation Overhead**: <10ms for registering 50 rules

---

## References

- **Research**: [research.md](research.md) - Design decisions and alternatives considered
- **Specification**: [spec.md](spec.md) - Functional requirements (FR-001 through FR-014)
- **Constitution**: Principle III (Rule Implementation Contract), Principle VII (Determinism)
- **Feature 001**: IO Boundary Refactor - established streaming patterns and performance baselines
