# Rule API Contracts

**Feature**: [spec.md](../spec.md) | **Plan**: [../plan.md](../plan.md)  
**Purpose**: Define the API contracts for all 8 analyzer rules

## Overview

All rules implement the `IAnalyzerRule` interface. This document specifies the exact API contract for each rule including inputs, outputs, and behavior guarantees.

---

## IAnalyzerRule Interface (Existing)

```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

public interface IAnalyzerRule
{
    /// <summary>Unique identifier for the rule.</summary>
    string Id { get; }

    /// <summary>Human-readable description of what the rule checks.</summary>
    string Description { get; }

    /// <summary>Severity level of findings produced by this rule.</summary>
    Severity Severity { get; }

    /// <summary>Category for grouping related rules.</summary>
    string Category { get; }

    /// <summary>
    /// Analyzes a syntax tree and returns zero or more diagnostic findings.
    /// Must be deterministic and not throw exceptions under normal operation.
    /// </summary>
    /// <param name="tree">The syntax tree to analyze. Never null.</param>
    /// <returns>
    /// Enumerable of diagnostic results. Return Enumerable.Empty<DiagnosticResult>()
    /// if no findings. Never return null.
    /// </returns>
    IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
```

---

## LNT002 - LongParameterListRule

### Contract

```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

public class LongParameterListRule : IAnalyzerRule
{
    public string Id => "LNT002";
    
    public string Description => 
        "Methods should not have more than 5 parameters";
    
    public Severity Severity => Severity.Warning;
    
    public string Category => DiagnosticCategories.CodeSmell;
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
```

### Input

- **tree**: SyntaxTree containing C# code to analyze
- **Preconditions**: tree must not be null (enforced by AnalyzerEngine)

### Output

- **Returns**: IEnumerable\<DiagnosticResult\> (lazy evaluation via yield return)
- **Empty result**: Enumerable.Empty() when no violations found
- **Never null**: Contract guarantees non-null return value

### Diagnostic Format

**When**: Method or constructor has >5 parameters (excluding `this` for extension methods)

**Message Template**: 
```
"Method '{methodName}' has {paramCount} parameters (max: 5). Consider using a parameter object or builder pattern."
```

**Example**:
```
"Method 'CreateUser' has 6 parameters (max: 5). Consider using a parameter object or builder pattern."
```

**Location**: Method declaration line

### Behavior Guarantees

- ✅ Deterministic: Same tree → same results, always
- ✅ Stateless: No side effects, thread-safe
- ✅ Skips auto-generated files (returns empty)
- ✅ Counts constructors, regular methods, operators
- ✅ Excludes `this` parameter for extension methods
- ✅ Includes generic type parameters in method name for clarity

---

## LNT003 - ComplexConditionalRule

### Contract

```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

public class ComplexConditionalRule : IAnalyzerRule
{
    public string Id => "LNT003";
    
    public string Description => 
        "Conditionals should not be nested more than 3 levels deep";
    
    public Severity Severity => Severity.Warning;
    
    public string Category => DiagnosticCategories.CodeSmell;
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
```

### Input

- **tree**: SyntaxTree containing C# code to analyze

### Output

- **Returns**: IEnumerable\<DiagnosticResult\>
- **One diagnostic per method** containing deeply nested conditionals
- **Location**: Deepest nested if/switch statement

### Diagnostic Format

**When**: if or switch statement nested >3 levels deep

**Message Template**:
```
"Conditional nesting depth is {depth} (max: 3). Consider using guard clauses or extracting to separate methods."
```

**Example**:
```
"Conditional nesting depth is 4 (max: 3). Consider using guard clauses or extracting to separate methods."
```

**Location**: The innermost conditional statement exceeding threshold

### Behavior Guarantees

- ✅ Counts both if and switch statements as nesting
- ✅ if-else chains at same level do NOT increase depth
- ✅ Nesting inside lambda expressions counts toward depth
- ✅ Reports location of deepest violation (not all nested statements)
- ✅ Deterministic depth calculation via recursive traversal

### Nesting Examples

**Depth 3 (OK)**:
```csharp
if (a)           // Depth 1
{
    if (b)       // Depth 2
    {
        if (c)   // Depth 3 - OK
        {
        }
    }
}
```

**Depth 4 (Violation)**:
```csharp
if (a)           // Depth 1
{
    if (b)       // Depth 2
    {
        switch(x) // Depth 3
        {
            case 1:
                if (c) // Depth 4 - VIOLATION
                {
                }
        }
    }
}
```

**Chain (NOT nesting, OK)**:
```csharp
if (a) { }
else if (b) { }  // Same level, not nested
else if (c) { }  // Same level, not nested
```

---

## LNT004 - MagicNumberRule

### Contract

```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

public class MagicNumberRule : IAnalyzerRule
{
    public string Id => "LNT004";
    
    public string Description => 
        "Avoid magic numbers; use named constants";
    
    public Severity Severity => Severity.Info;
    
    public string Category => DiagnosticCategories.CodeSmell;
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
```

### Input

- **tree**: SyntaxTree containing C# code to analyze

### Output

- **Returns**: IEnumerable\<DiagnosticResult\>
- **One diagnostic per magic number** literal found

### Diagnostic Format

**When**: Numeric literal (int, long, float, double) not in excluded set and not a named constant

**Message Template**:
```
"Magic number '{value}' should be replaced with a named constant. Consider extracting to a const field or readonly property."
```

**Example**:
```
"Magic number '5000' should be replaced with a named constant. Consider extracting to a const field or readonly property."
```

**Location**: Literal expression location

### Behavior Guarantees

- ✅ Excludes values: 0, 1, -1 (common constants)
- ✅ Excludes literals in const declarations
- ✅ Detects integer literals (int, long, uint, ulong)
- ✅ Detects floating-point literals (float, double, decimal)
- ✅ Includes literal value in message for context

### Exclusion Logic

**Excluded** (no diagnostic):
```csharp
const int MaxRetries = 3;   // Named constant
int x = 0;                  // 0, 1, -1 excluded
int y = 1;
int z = -1;
```

**Flagged** (diagnostic):
```csharp
int timeout = 5000;         // Magic number
double pi = 3.14159;        // Magic number (should use Math.PI)
if (status == 3) { }        // Magic number
```

---

## LNT005 - GodClassRule

### Contract

```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

public class GodClassRule : IAnalyzerRule
{
    public string Id => "LNT005";
    
    public string Description => 
        "Classes should not exceed 500 lines or 15 methods";
    
    public Severity Severity => Severity.Warning;
    
    public string Category => DiagnosticCategories.Design;
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
```

### Input

- **tree**: SyntaxTree containing C# code to analyze

### Output

- **Returns**: IEnumerable\<DiagnosticResult\>
- **One diagnostic per God Class** found

### Diagnostic Format

**When**: Class exceeds 500 lines OR 15 methods

**Message Template** (LOC violation):
```
"Class '{className}' has {lineCount} lines (max: 500). Consider splitting into smaller, focused classes by responsibility."
```

**Message Template** (Method count violation):
```
"Class '{className}' has {methodCount} methods (max: 15). Consider splitting into smaller, focused classes by responsibility."
```

**Message Template** (Both violations):
```
"Class '{className}' has {lineCount} lines (max: 500) and {methodCount} methods (max: 15). Consider splitting into smaller, focused classes by responsibility."
```

**Location**: Class declaration line

### Behavior Guarantees

- ✅ Counts lines from class start to end (includes braces, comments, whitespace)
- ✅ Counts only explicit method declarations (excludes auto-property accessors)
- ✅ Triggers if EITHER threshold exceeded
- ✅ Reports both metrics when both violated
- ✅ Partial classes: Analyzes each partial independently (limitation documented)

### Counting Rules

**Line Count**: Entire class span including:
- Opening/closing braces
- Comments
- Blank lines
- All members

**Method Count**: Only counts:
- MethodDeclarationSyntax nodes
- Excludes: properties, fields, constructors (separate from methods)
- Excludes: Auto-property get/set (not explicit method declarations)

---

## LNT006 - DeadCodeRule

### Contract

```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

public class DeadCodeRule : IAnalyzerRule
{
    public string Id => "LNT006";
    
    public string Description => 
        "Remove unused private members";
    
    public Severity Severity => Severity.Info;
    
    public string Category => DiagnosticCategories.Maintainability;
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
```

### Input

- **tree**: SyntaxTree containing C# code to analyze

### Output

- **Returns**: IEnumerable\<DiagnosticResult\>
- **One diagnostic per unused private member** found

### Diagnostic Format

**When**: Private method or field has zero references within declaring class

**Message Template** (method):
```
"Private method '{methodName}' is never used. Consider removing dead code."
```

**Message Template** (field):
```
"Private field '{fieldName}' is never used. Consider removing dead code."
```

**Location**: Member declaration line

### Behavior Guarantees

- ✅ Only checks private members (not public, protected, internal)
- ✅ Searches for references within same class
- ✅ Excludes private members implementing explicit interfaces (heuristic)
- ✅ Flags fields used only in their initializer as unused
- ✅ Syntax-based analysis (limitation: may miss reflection usage)

### Detection Strategy (Syntax-Only)

**Unused** (flagged):
```csharp
private void HelperMethod() { }  // Never called
private int _unusedField;        // Never referenced
private int _count = 0;          // Only in initializer, never read
```

**Used** (not flagged):
```csharp
private void Helper() { }
public void DoWork() { Helper(); }  // Called

private int _field;
public int Prop => _field;          // Referenced
```

**Excluded** (not flagged, interface implementation):
```csharp
private void InterfaceMethod() { }  // Matches interface member name (heuristic)
```

### Known Limitations

- Cannot detect reflection-based usage
- Cannot detect cross-file usage of public members
- Heuristic for interface detection (not full semantic analysis)

---

## LNT007 - ExceptionSwallowingRule

### Contract

```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

public class ExceptionSwallowingRule : IAnalyzerRule
{
    public string Id => "LNT007";
    
    public string Description => 
        "Avoid empty catch blocks that suppress exceptions";
    
    public Severity Severity => Severity.Warning;
    
    public string Category => DiagnosticCategories.CodeSmell;
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
```

### Input

- **tree**: SyntaxTree containing C# code to analyze

### Output

- **Returns**: IEnumerable\<DiagnosticResult\>
- **One diagnostic per empty catch block** found

### Diagnostic Format

**When**: Catch block contains zero statements

**Message Template**:
```
"Empty catch block suppresses exceptions. Consider logging the exception or removing the try-catch if error handling is not needed."
```

**Location**: catch keyword line

### Behavior Guarantees

- ✅ Simplest rule - purely syntactic check
- ✅ Flags catch blocks with zero statements
- ✅ Flags catch blocks with comments but no code
- ✅ Does NOT flag catch blocks with throw, logging, or other statements

### Examples

**Flagged** (empty):
```csharp
try { } 
catch { }  // Empty - FLAGGED

try { } 
catch (Exception) { }  // Empty - FLAGGED

try { } 
catch 
{ 
    // TODO: handle  // Comment only - FLAGGED
}
```

**Not Flagged** (has statements):
```csharp
try { } 
catch { throw; }  // Re-throw - OK

try { } 
catch (Exception ex) { Logger.Error(ex); }  // Logging - OK

try { } 
catch (Exception ex) when (ex is TimeoutException) { }  // Filter - OK
```

---

## LNT008 - MissingXmlDocumentationRule

### Contract

```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

public class MissingXmlDocumentationRule : IAnalyzerRule
{
    public string Id => "LNT008";
    
    public string Description => 
        "Public APIs should have XML documentation";
    
    public Severity Severity => Severity.Info;
    
    public string Category => DiagnosticCategories.Documentation;
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
```

### Input

- **tree**: SyntaxTree containing C# code to analyze

### Output

- **Returns**: IEnumerable\<DiagnosticResult\>
- **One diagnostic per undocumented public API** member

### Diagnostic Format

**When**: Public/protected class, method, or property lacks XML doc comment

**Message Template** (class):
```
"Public class '{className}' is missing XML documentation. Add a /// <summary> comment to describe the API."
```

**Message Template** (method):
```
"Public method '{methodName}' is missing XML documentation. Add a /// <summary> comment to describe the API."
```

**Message Template** (property):
```
"Public property '{propertyName}' is missing XML documentation. Add a /// <summary> comment to describe the API."
```

**Location**: Member declaration line

### Behavior Guarantees

- ✅ Checks classes, methods, and properties
- ✅ Only checks public and protected members
- ✅ Accepts `/// <summary>` as valid documentation
- ✅ Accepts `/// <inheritdoc />` as valid documentation
- ✅ Rejects regular `//` comments (must be XML doc format)

### Documentation Formats

**Valid** (not flagged):
```csharp
/// <summary>Description</summary>
public class Foo { }

/// <inheritdoc />
public override string ToString() { }

/// <summary>Does something</summary>
/// <param name="x">Parameter</param>
public void DoSomething(int x) { }
```

**Invalid** (flagged):
```csharp
// Regular comment, not XML doc
public class Bar { }  // FLAGGED

public void Method() { }  // No doc - FLAGGED
```

**Not Checked** (ignored):
```csharp
private void Helper() { }      // Private - ignored
internal class Service { }     // Internal - ignored
protected internal void X() { } // Not public - ignored
```

---

## LNT001 - LongMethodRule (Enhanced)

### Contract (Updated)

```csharp
namespace Lintelligent.AnalyzerEngine.Rules;

public class LongMethodRule : IAnalyzerRule
{
    public string Id => "LNT001";
    
    public string Description => 
        "Method exceeds recommended length";
    
    public Severity Severity => Severity.Warning;
    
    public string Category => DiagnosticCategories.CodeSmell;  // UPDATED
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree);
}
```

### Changes from Existing Implementation

1. **Category**: 
   - Old: `DiagnosticCategories.Maintainability`
   - New: `DiagnosticCategories.CodeSmell`

2. **Message**:
   - Old: `"Method is too long"`
   - New: `"Method '{methodName}' has {statementCount} statements (max: 20). Consider extracting logical blocks into separate methods."`

3. **Behavior**: No changes to detection logic (still counts statements > 20)

### Diagnostic Format (Updated)

**When**: Method body has >20 statements

**Message Template** (new):
```
"Method '{methodName}' has {statementCount} statements (max: 20). Consider extracting logical blocks into separate methods."
```

**Example**:
```
"Method 'ProcessOrder' has 25 statements (max: 20). Consider extracting logical blocks into separate methods."
```

**Location**: Method declaration line (unchanged)

### Migration Impact

- ✅ Existing tests continue passing (detection logic unchanged)
- ✅ New tests verify updated category
- ✅ New tests verify updated message format
- ✅ No breaking changes to IAnalyzerRule contract

---

## Common Behavior (All Rules)

### Auto-Generated Code Handling

All rules MUST skip auto-generated files:

```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    if (IsGeneratedCode(tree))
        return Enumerable.Empty<DiagnosticResult>();
    
    // ... rule logic
}

private static bool IsGeneratedCode(SyntaxTree tree)
{
    string fileName = Path.GetFileName(tree.FilePath);
    if (fileName.EndsWith(".Designer.cs") || 
        fileName.EndsWith(".g.cs") || 
        fileName.Contains(".Generated."))
        return true;
    
    var root = tree.GetRoot();
    var leadingTrivia = root.GetLeadingTrivia().Take(10);
    return leadingTrivia.Any(t => 
        t.ToString().Contains("<auto-generated>") ||
        t.ToString().Contains("<auto-generated />"));
}
```

### Error Handling

- MUST NOT throw exceptions for malformed syntax (Roslyn handles this)
- MAY return empty enumerable for files that cannot be analyzed
- MUST NOT log errors (violation of Constitutional Principle III)

### Performance

- MUST use lazy evaluation (yield return)
- MUST complete analysis of 1000-line file in <500ms
- SHOULD minimize tree traversals (cache results if multiple passes needed)

### Determinism

- Same syntax tree → same diagnostics, always
- No randomness, no time-based logic
- No external state dependencies
- Thread-safe (stateless design ensures this)

---

## DiagnosticResult Contract (Existing)

```csharp
public record DiagnosticResult(
    string FilePath,
    string RuleId,
    string Message,
    int LineNumber,
    Severity Severity,
    string Category);
```

**Usage by Rules**:
```csharp
yield return new DiagnosticResult(
    tree.FilePath,
    Id,                    // Rule ID (e.g., "LNT002")
    message,               // Formatted message with concrete values
    lineNumber,            // From SyntaxNode.GetLocation()
    Severity,              // From rule.Severity
    Category               // From rule.Category
);
```

---

## Summary

- **8 rule contracts defined**: 7 new + 1 enhanced
- **All implement IAnalyzerRule**: Consistent interface
- **Clear input/output contracts**: Deterministic, stateless, lazy
- **Concrete message templates**: Actionable with specific values
- **Common behavior patterns**: Auto-gen detection, error handling, performance
- **Ready for implementation**: All contracts specify enough detail for TDD

All rules ready for test-first implementation based on these contracts.
