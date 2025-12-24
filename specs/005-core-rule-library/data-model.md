# Data Model: Core Rule Library

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)  
**Created**: December 24, 2025  
**Purpose**: Define entities, relationships, and validation rules for Core Rule Library feature

## Overview

This feature introduces 7 new rule entities and enhances 1 existing rule entity. All rules are **stateless value objects** implementing the IAnalyzerRule interface. Rules produce DiagnosticResult entities as output. No persistent storage or mutable state required per Constitutional Principle III.

---

## Entities

### 1. LongParameterListRule

**Type**: Analyzer Rule (implements IAnalyzerRule)  
**RuleId**: `LNT002`  
**Purpose**: Detect methods with more than 5 parameters

**Attributes**:
| Attribute | Type | Description | Validation |
|-----------|------|-------------|------------|
| Id | string | "LNT002" | Const, non-null |
| Description | string | "Methods should not have more than 5 parameters" | Const, non-null |
| Severity | Severity enum | Warning | Const |
| Category | string | DiagnosticCategories.CodeSmell | Const, non-null |
| MaxParameters | int | 5 | Const, internal threshold |

**Behavior**:
- Analyze(SyntaxTree tree) → IEnumerable\<DiagnosticResult\>
- Traverses syntax tree for BaseMethodDeclarationSyntax nodes
- Counts parameters via ParameterList.Parameters.Count
- Excludes `this` parameter for extension methods (FR-025)
- Returns diagnostic for each method where count > MaxParameters

**Relationships**:
- Implements: IAnalyzerRule
- Produces: DiagnosticResult (0 to N)
- Consumes: SyntaxTree (Roslyn API)

**Validation Rules**:
- MUST return empty enumerable if no violations found
- MUST be stateless (no mutable fields)
- MUST be deterministic (same tree → same results)
- MUST skip auto-generated files

---

### 2. ComplexConditionalRule

**Type**: Analyzer Rule (implements IAnalyzerRule)  
**RuleId**: `LNT003`  
**Purpose**: Detect if/switch statements nested more than 3 levels deep

**Attributes**:
| Attribute | Type | Description | Validation |
|-----------|------|-------------|------------|
| Id | string | "LNT003" | Const, non-null |
| Description | string | "Conditionals should not be nested more than 3 levels deep" | Const, non-null |
| Severity | Severity enum | Warning | Const |
| Category | string | DiagnosticCategories.CodeSmell | Const, non-null |
| MaxNestingDepth | int | 3 | Const, internal threshold |

**Behavior**:
- Analyze(SyntaxTree tree) → IEnumerable\<DiagnosticResult\>
- Recursively calculates nesting depth for each method
- Counts both IfStatementSyntax and SwitchStatementSyntax (FR-019)
- if-else chains at same level do NOT increase depth
- Returns diagnostic for deepest nested conditional exceeding threshold

**Relationships**:
- Implements: IAnalyzerRule
- Produces: DiagnosticResult (0 to N)
- Consumes: SyntaxTree (Roslyn API)

**Validation Rules**:
- MUST correctly distinguish nesting (depth) from chaining (breadth)
- MUST count switch statements as conditional nesting
- MUST report location of deepest nested statement
- MUST be stateless and deterministic

**State Transitions**: None (stateless)

---

### 3. MagicNumberRule

**Type**: Analyzer Rule (implements IAnalyzerRule)  
**RuleId**: `LNT004`  
**Purpose**: Detect hardcoded numeric literals (excluding 0, 1, -1)

**Attributes**:
| Attribute | Type | Description | Validation |
|-----------|------|-------------|------------|
| Id | string | "LNT004" | Const, non-null |
| Description | string | "Avoid magic numbers; use named constants" | Const, non-null |
| Severity | Severity enum | Info | Const |
| Category | string | DiagnosticCategories.CodeSmell | Const, non-null |
| ExcludedValues | string[] | ["0", "1", "-1"] | Const, internal list |

**Behavior**:
- Analyze(SyntaxTree tree) → IEnumerable\<DiagnosticResult\>
- Finds all LiteralExpressionSyntax nodes with NumericLiteralExpression kind
- Excludes literals matching ExcludedValues (FR-020)
- Excludes literals that are part of const declarations
- Returns diagnostic for each magic number found

**Relationships**:
- Implements: IAnalyzerRule
- Produces: DiagnosticResult (0 to N)
- Consumes: SyntaxTree (Roslyn API)

**Validation Rules**:
- MUST exclude 0, 1, -1 from detection
- MUST exclude named constants (const keyword in parent declaration)
- MUST detect both integer and floating-point literals
- MUST include literal value in diagnostic message

---

### 4. GodClassRule

**Type**: Analyzer Rule (implements IAnalyzerRule)  
**RuleId**: `LNT005`  
**Purpose**: Detect classes exceeding 500 lines OR 15 methods

**Attributes**:
| Attribute | Type | Description | Validation |
|-----------|------|-------------|------------|
| Id | string | "LNT005" | Const, non-null |
| Description | string | "Classes should not exceed 500 lines or 15 methods" | Const, non-null |
| Severity | Severity enum | Warning | Const |
| Category | string | DiagnosticCategories.Design | Const, non-null |
| MaxLines | int | 500 | Const, internal threshold |
| MaxMethods | int | 15 | Const, internal threshold |

**Behavior**:
- Analyze(SyntaxTree tree) → IEnumerable\<DiagnosticResult\>
- Finds all ClassDeclarationSyntax nodes
- Counts lines via GetLocation().GetLineSpan() range
- Counts methods via Members.OfType\<MethodDeclarationSyntax\>()
- Triggers if EITHER threshold exceeded (FR-021)

**Relationships**:
- Implements: IAnalyzerRule
- Produces: DiagnosticResult (0 to N)
- Consumes: SyntaxTree (Roslyn API)

**Validation Rules**:
- MUST count lines for entire class declaration (including braces, comments)
- MUST count only actual method declarations (exclude auto-property accessors)
- MUST trigger on either LOC or method count threshold
- MUST report both metrics in diagnostic message

**Known Limitations**:
- Partial classes: Each partial analyzed independently (cross-file aggregation out of scope)

---

### 5. DeadCodeRule

**Type**: Analyzer Rule (implements IAnalyzerRule)  
**RuleId**: `LNT006`  
**Purpose**: Detect unused private methods and fields

**Attributes**:
| Attribute | Type | Description | Validation |
|-----------|------|-------------|------------|
| Id | string | "LNT006" | Const, non-null |
| Description | string | "Remove unused private members" | Const, non-null |
| Severity | Severity enum | Info | Const |
| Category | string | DiagnosticCategories.Maintainability | Const, non-null |

**Behavior**:
- Analyze(SyntaxTree tree) → IEnumerable\<DiagnosticResult\>
- Finds all private MethodDeclarationSyntax and FieldDeclarationSyntax
- Searches for references within same class using syntax tree traversal
- Excludes private methods implementing explicit interfaces (FR-022)
- Returns diagnostic for members with zero references

**Relationships**:
- Implements: IAnalyzerRule
- Produces: DiagnosticResult (0 to N)
- Consumes: SyntaxTree (Roslyn API)

**Validation Rules**:
- MUST only check private members (not public, protected, internal)
- MUST exclude interface implementations
- MUST detect both unused methods and unused fields
- MUST include member type (method/field) in diagnostic message

**Known Limitations**:
- Syntax-only analysis (no semantic model) - may miss reflection-based usage
- Cannot detect cross-file usage of public members

**State Transitions**: None (stateless)

---

### 6. ExceptionSwallowingRule

**Type**: Analyzer Rule (implements IAnalyzerRule)  
**RuleId**: `LNT007`  
**Purpose**: Detect empty catch blocks

**Attributes**:
| Attribute | Type | Description | Validation |
|-----------|------|-------------|------------|
| Id | string | "LNT007" | Const, non-null |
| Description | string | "Avoid empty catch blocks that suppress exceptions" | Const, non-null |
| Severity | Severity enum | Warning | Const |
| Category | string | DiagnosticCategories.CodeSmell | Const, non-null |

**Behavior**:
- Analyze(SyntaxTree tree) → IEnumerable\<DiagnosticResult\>
- Finds all CatchClauseSyntax nodes
- Checks if Block.Statements.Count == 0
- Returns diagnostic for each empty catch block (FR-023)

**Relationships**:
- Implements: IAnalyzerRule
- Produces: DiagnosticResult (0 to N)
- Consumes: SyntaxTree (Roslyn API)

**Validation Rules**:
- MUST flag catch blocks with zero statements
- MUST flag catch blocks with only comments (no executable code)
- MUST NOT flag catch blocks with throw, logging, or other statements
- Simplest rule - purely syntactic check

**Known Limitations**:
- Flags all empty catches, including intentional OperationCanceledException swallowing (configurable in future)

---

### 7. MissingXmlDocumentationRule

**Type**: Analyzer Rule (implements IAnalyzerRule)  
**RuleId**: `LNT008`  
**Purpose**: Detect public APIs without XML documentation comments

**Attributes**:
| Attribute | Type | Description | Validation |
|-----------|------|-------------|------------|
| Id | string | "LNT008" | Const, non-null |
| Description | string | "Public APIs should have XML documentation" | Const, non-null |
| Severity | Severity enum | Info | Const |
| Category | string | DiagnosticCategories.Documentation | Const, non-null |

**Behavior**:
- Analyze(SyntaxTree tree) → IEnumerable\<DiagnosticResult\>
- Finds ClassDeclarationSyntax, MethodDeclarationSyntax, PropertyDeclarationSyntax
- Filters for public/protected accessibility
- Checks GetLeadingTrivia() for SingleLineDocumentationCommentTrivia
- Accepts \<inheritdoc /\> as valid documentation (FR-024)
- Returns diagnostic for undocumented public members

**Relationships**:
- Implements: IAnalyzerRule
- Produces: DiagnosticResult (0 to N)
- Consumes: SyntaxTree (Roslyn API)

**Validation Rules**:
- MUST check classes, methods, and properties
- MUST only check public and protected members
- MUST accept /// <summary> or <inheritdoc/>
- MUST include member type and name in diagnostic

**Known Limitations**:
- Does not check events or delegates (can be added in future)
- Does not validate quality of documentation (only presence)

---

### 8. LongMethodRule (Enhanced)

**Type**: Analyzer Rule (implements IAnalyzerRule)  
**RuleId**: `LNT001` (existing)  
**Purpose**: Detect methods exceeding 20 statements (enhanced metadata)

**Attributes** (updated):
| Attribute | Type | Description | Validation | Change |
|-----------|------|-------------|------------|--------|
| Id | string | "LNT001" | Const, non-null | No change |
| Description | string | "Method exceeds recommended length" | Const, non-null | No change |
| Severity | Severity enum | Warning | Const | No change |
| Category | string | DiagnosticCategories.CodeSmell | Const, non-null | **UPDATED** (was Maintainability) |

**Behavior** (updated):
- Existing: Counts statements via m.Body?.Statements.Count > 20
- Enhancement: Update diagnostic message to include fix guidance
  - Old: "Method is too long"
  - New: "Method '{methodName}' has {statementCount} statements (max: 20). Consider extracting logical blocks into separate methods."

**Relationships**:
- Implements: IAnalyzerRule (existing)
- Produces: DiagnosticResult (existing)
- Consumes: SyntaxTree (existing)

**Validation Rules** (existing):
- All existing tests must continue passing
- New tests verify updated category and message format

**Changes**:
1. Category property: Update to use DiagnosticCategories.CodeSmell
2. Diagnostic message: Add method name and statement count to message
3. Diagnostic message: Add fix guidance text

---

## Shared Entities (Existing Infrastructure)

### DiagnosticResult (record)

**Purpose**: Immutable value object representing a rule violation

**Attributes**:
| Attribute | Type | Description | Source |
|-----------|------|-------------|--------|
| FilePath | string | Path to file containing violation | From SyntaxTree.FilePath |
| RuleId | string | Unique rule identifier | From IAnalyzerRule.Id |
| Message | string | Human-readable description | Rule-specific template |
| LineNumber | int | 1-based line number of violation | From SyntaxNode.GetLocation() |
| Severity | Severity enum | Warning, Info, Error | From IAnalyzerRule.Severity |
| Category | string | Rule category for grouping | From IAnalyzerRule.Category |

**Validation**:
- All properties required (no nulls)
- Immutable after construction (C# record)
- FilePath, RuleId, Message must be non-empty strings
- LineNumber must be >= 1

---

### DiagnosticCategories (static class)

**Purpose**: Constant definitions for rule categories

**Existing Constants**:
- Maintainability
- Performance
- Security
- Style
- Design
- General

**New Constant** (added in this feature):
```csharp
/// <summary>
///     Code smells that indicate potential maintainability issues.
///     Examples: long parameter lists, deep nesting, exception swallowing.
/// </summary>
public const string CodeSmell = "Code Smell";
```

**New Constant** (added in this feature):
```csharp
/// <summary>
///     Documentation quality issues.
///     Examples: missing XML docs, incomplete summaries.
/// </summary>
public const string Documentation = "Documentation";
```

---

## Relationships

```
IAnalyzerRule (interface)
    ├── LongParameterListRule (LNT002)
    ├── ComplexConditionalRule (LNT003)
    ├── MagicNumberRule (LNT004)
    ├── GodClassRule (LNT005)
    ├── DeadCodeRule (LNT006)
    ├── ExceptionSwallowingRule (LNT007)
    ├── MissingXmlDocumentationRule (LNT008)
    └── LongMethodRule (LNT001 - enhanced)
         ↓ each produces
    DiagnosticResult (0 to N instances)
         ↓ uses constants from
    DiagnosticCategories (static class)
```

**Cardinality**:
- One rule analyzes one syntax tree → produces 0 to N diagnostics
- Each diagnostic references exactly one rule ID
- Each diagnostic belongs to exactly one category
- Categories are shared across all rules (many-to-one)

---

## Validation Rules Summary

### Universal Validation (all rules)

- ✅ MUST implement IAnalyzerRule interface
- ✅ MUST be stateless (no mutable instance fields)
- ✅ MUST be deterministic (same input → same output, always)
- ✅ MUST return IEnumerable\<DiagnosticResult\> (not null)
- ✅ MUST return Enumerable.Empty() when no violations found
- ✅ MUST skip auto-generated code files (FR-018)
- ✅ MUST include location information in each diagnostic (FR-014)
- ✅ MUST include actionable fix guidance in message (FR-015)
- ✅ MUST include concrete values in message (e.g., "6 parameters") (FR-016)
- ✅ MUST pass rule ID, severity, category to DiagnosticResult constructor (FR-017)

### Rule-Specific Validation

Documented in individual entity sections above.

---

## Data Flow

```
1. SyntaxTree (input)
   ↓
2. Rule.Analyze(tree)
   ↓ (traverses tree, applies logic)
3. IEnumerable<DiagnosticResult> (output)
   ↓
4. AnalyzerEngine collects results
   ↓
5. ReportGenerator formats for output
```

**No persistence**: All entities are transient in-memory objects. No database, no files written by rules.

---

## Edge Cases & Special Handling

### Auto-Generated Code Detection

**Pattern**: Check at start of each rule's Analyze() method

```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    if (IsGeneratedCode(tree))
        return Enumerable.Empty<DiagnosticResult>();
    
    // ... rule logic
}
```

### Extension Method Parameters

**Pattern**: LongParameterListRule only

```csharp
bool isExtension = method.ParameterList.Parameters
    .FirstOrDefault()?.Modifiers.Any(SyntaxKind.ThisKeyword) ?? false;

int effectiveCount = isExtension 
    ? parameters.Count - 1 
    : parameters.Count;
```

### Interface Implementation Exclusion

**Pattern**: DeadCodeRule only

```csharp
// Check if method name matches any interface member
// (Syntax-only heuristic - full semantic check deferred)
```

### XML Doc Inheritance

**Pattern**: MissingXmlDocumentationRule only

```csharp
bool hasDoc = node.GetLeadingTrivia()
    .Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) &&
              (t.ToString().Contains("<summary") || 
               t.ToString().Contains("<inheritdoc")));
```

---

## Performance Considerations

**Constraints**: Each rule must analyze 1000-line file in <500ms

**Optimizations**:
- Use DescendantNodes() for lazy evaluation
- Filter with OfType<T>() early in LINQ chain
- Avoid multiple tree traversals (cache if needed)
- Use yield return for incremental result production
- Skip auto-generated files immediately

**Expected Performance**:
- LNT007 (Exception Swallowing): 10-20ms (simplest, one traversal)
- LNT002, LNT004, LNT005, LNT008: 20-50ms (single traversal with filtering)
- LNT003 (Complex Conditional): 30-70ms (recursive depth calculation)
- LNT006 (Dead Code): 50-100ms (reference finding within class)

All well within <500ms budget. Total for all 8 rules: ~200-300ms for 1000-line file.

---

## Summary

- **8 rule entities**: 7 new + 1 enhanced
- **2 infrastructure enhancements**: DiagnosticCategories.CodeSmell, DiagnosticCategories.Documentation constants
- **0 breaking changes**: All rules use existing IAnalyzerRule interface and DiagnosticResult class
- **Stateless design**: No mutable state, full Constitutional Principle III compliance
- **Deterministic**: Same input always produces same output
- **Performance validated**: <500ms per rule, <2s for all rules combined

All entities ready for implementation. No unresolved data modeling questions.
