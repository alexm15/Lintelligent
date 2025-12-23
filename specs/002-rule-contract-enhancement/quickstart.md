# Quickstart: Enhanced Rule Contract Migration

**Feature**: 002-rule-contract-enhancement  
**Target Audience**: Developers migrating from v1.x to v2.0  
**Time to Complete**: ~15-30 minutes

---

## What Changed

### Breaking Changes

1. **IAnalyzerRule.Analyze() Return Type**
   - **Old**: `DiagnosticResult? Analyze(SyntaxTree tree)`
   - **New**: `IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)`

2. **IAnalyzerRule New Properties**
   - **Added**: `Severity Severity { get; }`
   - **Added**: `string Category { get; }`

3. **DiagnosticResult Constructor**
   - **Old**: `DiagnosticResult(string filePath, string ruleId, string message, int lineNumber)`
   - **New**: `DiagnosticResult(filePath, ruleId, message, lineNumber, Severity severity, string category)`

### Why This Change?

- **Multiple Findings**: Rules can now report all violations in a file, not just the first one
- **Severity Filtering**: Users can filter results by Error/Warning/Info to focus on critical issues
- **Categorization**: Results grouped by category (Maintainability, Performance, Security, etc.) for better reporting
- **Constitutional Alignment**: Principle III requires severity metadata and comprehensive findings

---

## Migration Steps

### Step 1: Update Rule Properties

Add `Severity` and `Category` properties to your rule class.

**Before**:
```csharp
public class LongMethodRule : IAnalyzerRule
{
    public string Id => "LongMethod";
    public string Description => "Methods should not exceed 50 lines";
    
    public DiagnosticResult? Analyze(SyntaxTree tree) { /* ... */ }
}
```

**After**:
```csharp
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;

public class LongMethodRule : IAnalyzerRule
{
    public string Id => "LongMethod";
    public string Description => "Methods should not exceed 50 lines";
    public Severity Severity => Severity.Warning;  // NEW
    public string Category => DiagnosticCategories.Maintainability;  // NEW
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree) { /* ... */ }
}
```

**Severity Selection Guide**:
- `Severity.Error`: Critical bugs, security issues, correctness problems
- `Severity.Warning`: Code smells, maintainability issues, should fix but non-blocking
- `Severity.Info`: Style suggestions, optional improvements

**Category Selection**:
- Use constants from `DiagnosticCategories` when applicable:
  - `Maintainability`, `Performance`, `Security`, `Style`, `Design`, `General`
- Or define custom category: `public string Category => "CustomDomain";`

---

### Step 2: Update Analyze() Return Type

Change return type from `DiagnosticResult?` to `IEnumerable<DiagnosticResult>`.

**Before** (single finding):
```csharp
public DiagnosticResult? Analyze(SyntaxTree tree)
{
    var root = tree.GetRoot();
    var method = FindFirstLongMethod(root);
    
    if (method == null)
        return null;  // No findings
    
    return new DiagnosticResult(
        tree.FilePath,
        Id,
        $"Method '{method.Identifier}' exceeds 50 lines",
        method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
    );
}
```

**After** (multiple findings):
```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    var root = tree.GetRoot();
    var methods = FindAllLongMethods(root);  // Changed: find ALL long methods
    
    if (!methods.Any())
        return Enumerable.Empty<DiagnosticResult>();  // No findings
    
    // Use LINQ Select or foreach with yield return
    return methods.Select(method => new DiagnosticResult(
        tree.FilePath,
        Id,
        $"Method '{method.Identifier}' exceeds 50 lines ({method.LineCount} lines)",
        method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
        Severity,   // NEW: pass rule's severity
        Category    // NEW: pass rule's category
    ));
}
```

**Alternative with yield return** (lazy evaluation):
```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    var root = tree.GetRoot();
    
    foreach (var method in FindAllLongMethods(root))
    {
        yield return new DiagnosticResult(
            tree.FilePath,
            Id,
            $"Method '{method.Identifier}' exceeds 50 lines ({method.LineCount} lines)",
            method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            Severity,
            Category
        );
    }
    
    // No explicit return needed - if no methods found, yields nothing (empty enumerable)
}
```

**Key Changes**:
- Return `IEnumerable<DiagnosticResult>` (not nullable)
- Return `Enumerable.Empty<DiagnosticResult>()` instead of `null` for no findings
- Pass `Severity` and `Category` to DiagnosticResult constructor
- Find **all** violations, not just the first one

---

### Step 3: Update DiagnosticResult Creation

Add `severity` and `category` parameters to all DiagnosticResult constructor calls.

**Before**:
```csharp
return new DiagnosticResult(
    filePath: tree.FilePath,
    ruleId: Id,
    message: "Method too long",
    lineNumber: 42
);
```

**After**:
```csharp
return new DiagnosticResult(
    filePath: tree.FilePath,
    ruleId: Id,
    message: "Method too long",
    lineNumber: 42,
    severity: Severity,   // Pass rule's Severity property
    category: Category    // Pass rule's Category property
);
```

**Common Pattern**: Always pass `this.Severity` and `this.Category` from the rule.

---

## Complete Example: LongMethodRule Migration

### Before (v1.x)

```csharp
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Results;

public class LongMethodRule : IAnalyzerRule
{
    private const int MaxLines = 50;

    public string Id => "LongMethod";
    public string Description => "Methods should not exceed 50 lines";

    public DiagnosticResult? Analyze(SyntaxTree tree)
    {
        var root = tree.GetCompilationUnitRoot();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var lineSpan = method.GetLocation().GetLineSpan();
            var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

            if (lineCount > MaxLines)
            {
                return new DiagnosticResult(
                    tree.FilePath,
                    Id,
                    $"Method '{method.Identifier}' is {lineCount} lines (max {MaxLines})",
                    lineSpan.StartLinePosition.Line + 1
                );
            }
        }

        return null;  // No violations found
    }
}
```

### After (v2.0)

```csharp
using Lintelligent.AnalyzerEngine.Abstractions;  // NEW: for Severity enum
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Results;

public class LongMethodRule : IAnalyzerRule
{
    private const int MaxLines = 50;

    public string Id => "LongMethod";
    public string Description => "Methods should not exceed 50 lines to maintain readability";
    
    // NEW: Severity and Category properties
    public Severity Severity => Severity.Warning;
    public string Category => DiagnosticCategories.Maintainability;

    // CHANGED: Return type IEnumerable<DiagnosticResult>
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
    {
        var root = tree.GetCompilationUnitRoot();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var lineSpan = method.GetLocation().GetLineSpan();
            var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

            if (lineCount > MaxLines)
            {
                // CHANGED: yield return instead of return
                // CHANGED: Added Severity and Category parameters
                yield return new DiagnosticResult(
                    tree.FilePath,
                    Id,
                    $"Method '{method.Identifier}' is {lineCount} lines (max {MaxLines})",
                    lineSpan.StartLinePosition.Line + 1,
                    Severity,   // NEW
                    Category    // NEW
                );
            }
        }

        // No explicit return needed for empty enumerable
    }
}
```

---

## Testing Your Migration

### 1. Unit Test for Multiple Findings

```csharp
[Fact]
public void Analyze_FileWithMultipleLongMethods_ReturnsAllFindings()
{
    // Arrange
    var code = @"
        class MyClass
        {
            void Method1() { /* 60 lines */ }
            void Method2() { /* 30 lines */ }  // OK
            void Method3() { /* 70 lines */ }
        }";
    var tree = CSharpSyntaxTree.ParseText(code);
    var rule = new LongMethodRule();

    // Act
    var results = rule.Analyze(tree).ToList();

    // Assert
    results.Should().HaveCount(2);  // Method1 and Method3
    results.Should().AllSatisfy(r =>
    {
        r.Severity.Should().Be(Severity.Warning);
        r.Category.Should().Be("Maintainability");
    });
}
```

### 2. Unit Test for Empty Results

```csharp
[Fact]
public void Analyze_NoViolations_ReturnsEmptyEnumerable()
{
    // Arrange
    var code = "class MyClass { void ShortMethod() { } }";
    var tree = CSharpSyntaxTree.ParseText(code);
    var rule = new LongMethodRule();

    // Act
    var results = rule.Analyze(tree);

    // Assert
    results.Should().BeEmpty();
    results.Should().NotBeNull();  // Important: never null
}
```

### 3. Verify Metadata Propagation

```csharp
[Fact]
public void Analyze_ReturnsResultsWithCorrectMetadata()
{
    // Arrange
    var code = "class C { void LongMethod() { /* 60 lines */ } }";
    var tree = CSharpSyntaxTree.ParseText(code);
    var rule = new LongMethodRule();

    // Act
    var result = rule.Analyze(tree).Single();

    // Assert
    result.Severity.Should().Be(rule.Severity);
    result.Category.Should().Be(rule.Category);
    result.RuleId.Should().Be(rule.Id);
}
```

---

## CLI Usage (Post-Migration)

### Filter Results by Severity

```bash
# Show only errors (critical issues)
lintelligent scan --severity Error

# Show errors and warnings (exclude info)
lintelligent scan --severity Error,Warning

# Show all severities (default)
lintelligent scan
```

### Group Results by Category

```bash
# Group output by category
lintelligent scan --group-by category

# Example output:
# === Maintainability ===
# - LongMethod: Method 'Process' is 65 lines (max 50)
# - ComplexCondition: Cyclomatic complexity 12 (max 10)
#
# === Performance ===
# - StringConcat: Use StringBuilder for repeated concatenation
```

---

## Common Pitfalls

### ❌ Returning null

```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    if (noViolations)
        return null;  // WRONG: will cause NullReferenceException
}
```

**✅ Solution**: Return `Enumerable.Empty<DiagnosticResult>()`

```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    if (noViolations)
        return Enumerable.Empty<DiagnosticResult>();  // CORRECT
}
```

---

### ❌ Forgetting Metadata Parameters

```csharp
return new DiagnosticResult(
    tree.FilePath,
    Id,
    "Message",
    lineNumber
    // Missing: Severity and Category
);
```

**✅ Solution**: Always pass Severity and Category

```csharp
return new DiagnosticResult(
    tree.FilePath,
    Id,
    "Message",
    lineNumber,
    Severity,   // Add this
    Category    // Add this
);
```

---

### ❌ Hardcoding Severity in DiagnosticResult

```csharp
return new DiagnosticResult(
    tree.FilePath, Id, "Message", lineNumber,
    Severity.Error,  // WRONG: hardcoded, not from rule property
    "Maintainability"
);
```

**✅ Solution**: Use rule's Severity and Category properties

```csharp
return new DiagnosticResult(
    tree.FilePath, Id, "Message", lineNumber,
    this.Severity,   // CORRECT: from rule property
    this.Category
);
```

---

## Performance Tips

### Use Lazy Evaluation

Prefer `yield return` over `.ToList()` for large result sets:

**Good** (lazy):
```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    foreach (var issue in FindIssues(tree))
    {
        yield return CreateDiagnostic(issue);
    }
}
```

**Avoid** (eager allocation):
```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    var results = new List<DiagnosticResult>();
    foreach (var issue in FindIssues(tree))
    {
        results.Add(CreateDiagnostic(issue));
    }
    return results;  // Allocates list upfront
}
```

### Limit Findings Per File

If your rule could emit 100+ findings per file, consider batching or early exit:

```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    var issueCount = 0;
    const int maxFindings = 50;

    foreach (var issue in FindIssues(tree))
    {
        if (issueCount++ >= maxFindings)
        {
            yield return new DiagnosticResult(
                tree.FilePath, Id,
                $"Too many violations (>{maxFindings}), stopping analysis",
                1, Severity, Category
            );
            yield break;
        }

        yield return CreateDiagnostic(issue);
    }
}
```

---

## FAQ

**Q: Can I return a List instead of IEnumerable?**  
A: Yes, `List<DiagnosticResult>` is compatible with `IEnumerable<DiagnosticResult>`. However, `yield return` is preferred for lazy evaluation.

**Q: What if my rule only ever returns one finding?**  
A: Still return `IEnumerable<DiagnosticResult>`. Use `yield return` or `return new[] { diagnostic };`.

**Q: Can I use Severity.Error for all findings?**  
A: Technically yes, but defeats the purpose. Choose severity based on impact:
- Bugs/security → Error
- Code smells → Warning
- Style → Info

**Q: Are custom categories allowed?**  
A: Yes! Use `DiagnosticCategories` constants for common cases, or define custom strings like `"DomainLogic"`.

**Q: Will this break AnalyzerEngine consumers?**  
A: No. The AnalyzerEngine public API (Analyze method) remains unchanged. Only custom rule implementations need updates.

**Q: How do I handle exceptions in rules?**  
A: AnalyzerEngine catches exceptions and continues analysis. For expected errors (e.g., malformed syntax), catch and return empty enumerable. Let unexpected exceptions propagate.

---

## Next Steps

1. **Update all custom rules** following the patterns above
2. **Run tests** to verify multiple findings and metadata propagation
3. **Update CLI commands** to use severity filtering (if needed)
4. **Document severity levels** for your custom rules in README
5. **Performance test** rules emitting many findings

**Estimated Migration Time**: 15-30 minutes per rule (simple rules faster)

---

## Resources

- **Specification**: [spec.md](spec.md) - Full requirements and user stories
- **Research**: [research.md](research.md) - Design decisions and alternatives
- **Data Model**: [data-model.md](data-model.md) - Entity definitions and validation
- **Contracts**: [contracts/](contracts/) - Interface definitions and examples
- **Constitution**: `.specify/memory/constitution.md` - Architectural principles

**Need Help?** Review the complete LongMethodRule migration example above or consult the data model documentation.
