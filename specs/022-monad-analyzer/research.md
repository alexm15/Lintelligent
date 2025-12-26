# Research: Language-Ext Monad Detection

**Feature**: 022-monad-analyzer  
**Phase**: 0 (Research & Discovery)  
**Date**: 2024-12-26

## Overview

This document captures research findings for implementing monad detection rules in the Lintelligent analyzer. All NEEDS CLARIFICATION items from the technical context have been resolved.

---

## 1. language-ext Type System Analysis

**Objective**: Document language-ext monad type signatures to recognize them during semantic analysis.

### Core Monad Types

```csharp
// Package: LanguageExt.Core
namespace LanguageExt
{
    // Option<T> - Represents optional values (alternative to null)
    public readonly struct Option<T>
    {
        public static Option<T> Some(T value);
        public static Option<T> None;
    }

    // Either<L, R> - Represents success (Right) or failure (Left)
    public readonly struct Either<L, R>
    {
        public static Either<L, R> Right(R value);
        public static Either<L, R> Left(L value);
    }

    // Try<T> - Represents computations that may throw exceptions
    public readonly struct Try<T>
    {
        public static Try<T> Success(T value);
        public static Try<T> Fail(Exception ex);
    }
}

namespace LanguageExt.Common
{
    // Validation<T> - Accumulates multiple validation errors
    public readonly struct Validation<F, T>
    {
        public static Validation<F, T> Success(T value);
        public static Validation<F, T> Fail(Seq<F> failures);
    }
    
    // Simplified version (single failure type)
    public readonly struct Validation<T> : Validation<Error, T> { }
}
```

### Type Detection Strategy

**Decision**: Use fully qualified type names for semantic model comparison:
- `LanguageExt.Option<T>`
- `LanguageExt.Either<L, R>`
- `LanguageExt.Try<T>`
- `LanguageExt.Common.Validation<T>` or `LanguageExt.Common.Validation<F, T>`

**Rationale**: Avoids false positives from custom types with same names.

**API Pattern**:
```csharp
var typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;
var isOption = typeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
    .StartsWith("global::LanguageExt.Option<") ?? false;
```

---

## 2. Roslyn Semantic Model API Research

**Objective**: Identify Roslyn APIs for pattern detection.

### 2.1 Detecting Nullable Return Types

**Use Case**: FR-002 (NullableToOptionRule) - Detect methods returning nullable types.

```csharp
// API Pattern
var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
var returnType = methodSymbol.ReturnType;

// Check if nullable reference type (C# 8+)
bool isNullable = returnType.NullableAnnotation == NullableAnnotation.Annotated;

// Check for null returns in method body
var returnStatements = methodDeclaration.DescendantNodes()
    .OfType<ReturnStatementSyntax>();
    
foreach (var returnStmt in returnStatements)
{
    var constantValue = semanticModel.GetConstantValue(returnStmt.Expression);
    if (constantValue.HasValue && constantValue.Value == null)
    {
        // Found explicit "return null;"
    }
}
```

**Decision**: Trigger LNT200 when:
1. Return type has `NullableAnnotation.Annotated`, AND
2. Method contains 3+ null checks or null returns (complexity threshold)

### 2.2 Finding Try/Catch Blocks

**Use Case**: FR-003 (TryCatchToEitherRule) - Detect error handling patterns.

```csharp
// API Pattern
var tryCatchStatements = methodDeclaration.DescendantNodes()
    .OfType<TryStatementSyntax>();

foreach (var tryStmt in tryCatchStatements)
{
    // Check catch block returns error value
    var catchClauses = tryStmt.Catches;
    foreach (var catchClause in catchClauses)
    {
        var returnInCatch = catchClause.Block.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .FirstOrDefault();
            
        if (returnInCatch != null)
        {
            // Try/catch used for control flow (Either<L, R> candidate)
        }
    }
}
```

**Decision**: Trigger LNT201 when:
1. Try/catch block returns values in both branches (not just throws), AND
2. Exception is caught and converted to error return (not logged/rethrown)

### 2.3 Identifying Sequential Validation Patterns

**Use Case**: FR-004 (SequentialValidationRule) - Detect validation chains.

```csharp
// Pattern: Multiple if statements returning error strings/objects
// Example code to detect:
// if (!IsValid(input1)) return "Error 1";
// if (!IsValid(input2)) return "Error 2";
// return Success(result);

var ifStatements = methodDeclaration.DescendantNodes()
    .OfType<IfStatementSyntax>()
    .Where(ifs => ifs.Statement is ReturnStatementSyntax)
    .ToList();

if (ifStatements.Count >= 2)
{
    // Check if returns are error values (strings, error objects)
    var returnsErrorValues = ifStatements.All(ifs =>
    {
        var returnStmt = (ReturnStatementSyntax)ifs.Statement;
        var returnType = semanticModel.GetTypeInfo(returnStmt.Expression).Type;
        return returnType?.SpecialType == SpecialType.System_String ||
               returnType?.Name.Contains("Error") == true;
    });
    
    if (returnsErrorValues)
    {
        // Sequential validation pattern (Validation<T> candidate)
    }
}
```

**Decision**: Trigger LNT202 when:
1. 2+ sequential if statements with early error returns, AND
2. Returns are validation errors (strings, ValidationError, Result<T>)

---

## 3. NuGet Package Reference Detection

**Objective**: FR-008 requires checking if language-ext is referenced before reporting diagnostics.

### API Pattern

```csharp
// From DiagnosticAnalyzer context
public override void Initialize(AnalysisContext context)
{
    context.RegisterCompilationAction(compilationContext =>
    {
        var compilation = compilationContext.Compilation;
        
        // Check if language-ext assembly is referenced
        bool hasLanguageExt = compilation.ReferencedAssemblyNames.Any(name =>
            name.Name.Equals("LanguageExt.Core", StringComparison.OrdinalIgnoreCase));
        
        if (!hasLanguageExt)
        {
            // Skip monad detection - package not available
            return;
        }
        
        // Proceed with analysis...
    });
}
```

**Decision**: Use `Compilation.ReferencedAssemblyNames` to check for `LanguageExt.Core` assembly.

**Rationale**: More reliable than searching for NuGet package references (which are MSBuild-level, not available in Roslyn compilation).

---

## 4. EditorConfig Integration Pattern

**Objective**: FR-001 requires opt-in via `language_ext_monad_detection = true`.

### API Pattern

```csharp
// From DiagnosticAnalyzer (Roslyn 3.0+)
public override void Initialize(AnalysisContext context)
{
    context.RegisterSyntaxTreeAction(treeContext =>
    {
        var tree = treeContext.Tree;
        var options = treeContext.Options;
        
        // Get analyzer config for this syntax tree
        var analyzerConfigOptions = options.AnalyzerConfigOptionsProvider
            .GetOptions(tree);
        
        // Read custom EditorConfig setting
        if (!analyzerConfigOptions.TryGetValue(
            "language_ext_monad_detection", 
            out var value) || 
            !bool.TryParse(value, out var enabled) || 
            !enabled)
        {
            // Setting not enabled - skip analysis
            return;
        }
        
        // Proceed with monad detection...
    });
}
```

**Integration Point**: Modify `LintelligentDiagnosticAnalyzer.cs` to check EditorConfig before invoking monad rules.

**Decision**: Add EditorConfig check at analyzer level (not rule level) to skip rule execution entirely when disabled.

**Configuration Schema**:
```ini
# .editorconfig
[*.cs]
language_ext_monad_detection = true          # Enable monad detection
language_ext_min_complexity = 3              # Minimum null checks/validations to trigger
```

---

## 5. Diagnostic Message Best Practices

**Objective**: FR-006, FR-007 require educational messages with code examples.

### Message Template Structure

```csharp
// Diagnostic descriptor
var descriptor = new DiagnosticDescriptor(
    id: "LNT200",
    title: "Consider using Option<T> for nullable return type",
    messageFormat: "Method '{0}' returns nullable - consider Option<T> for safer null handling",
    category: "Functional",
    defaultSeverity: DiagnosticSeverity.Info,
    isEnabledByDefault: true,
    description: @"
### Current Code
```csharp
public string? FindUser(int id)
{{
    if (id < 0) return null;
    return _users.FirstOrDefault(u => u.Id == id);
}}
```

### Suggested Refactoring
```csharp
public Option<string> FindUser(int id)
{{
    if (id < 0) return Option<string>.None;
    return _users.FirstOrDefault(u => u.Id == id).ToOption();
}}
```

### Benefits
- Eliminates null reference exceptions
- Forces caller to handle missing value case
- Self-documenting: signature shows value may be absent
"
);
```

**Limitation**: Roslyn diagnostic descriptions support plain text only. Code blocks must use escaped braces `{{` and newlines `\n`.

**Decision**: 
1. Use `messageFormat` for concise single-line summary
2. Use `description` property for full educational content with code examples
3. Format code blocks with proper indentation for IDE display

**Alternative Considered**: Use `DiagnosticResult.Properties` to attach code snippets. Rejected because not displayed in IDE quick info.

---

## Research Findings Summary

### Confirmed Decisions

1. **Monad Types**: Detect Option<T>, Either<L,R>, Validation<T>, Try<T> using fully qualified type names
2. **Complexity Thresholds**:
   - LNT200 (Option): 3+ null checks or null returns
   - LNT201 (Either): Try/catch with return in both branches
   - LNT202 (Validation): 2+ sequential error returns
3. **Package Check**: Use `Compilation.ReferencedAssemblyNames` to verify LanguageExt.Core
4. **EditorConfig**: Read `language_ext_monad_detection` via `AnalyzerConfigOptionsProvider`
5. **Message Format**: Use `description` property with formatted code blocks

### Roslyn APIs Required

- `SemanticModel.GetTypeInfo()` - Type inference
- `ITypeSymbol.NullableAnnotation` - Nullable detection
- `SyntaxNode.DescendantNodes<T>()` - Syntax tree traversal
- `Compilation.ReferencedAssemblyNames` - Package reference check
- `AnalyzerConfigOptionsProvider.GetOptions()` - EditorConfig integration

### Open Questions (Deferred to Phase 1)

- **Q**: Should we detect async methods returning `Task<T?>` → `Task<Option<T>>`?
  - **Answer**: Yes, include in FR-002 implementation. Same complexity threshold.

- **Q**: Should we suggest `Either<Exception, T>` or `Either<Error, T>`?
  - **Answer**: Use `Either<Error, T>` in examples (language-ext best practice). Error type is customizable.

- **Q**: Minimum .NET version for NullableAnnotation API?
  - **Answer**: Roslyn 3.0+ (.NET Core 3.0, included in our Roslyn SDK 4.12.0 dependency)

---

**Status**: Phase 0 complete. All NEEDS CLARIFICATION resolved. Ready for Phase 1 design.
