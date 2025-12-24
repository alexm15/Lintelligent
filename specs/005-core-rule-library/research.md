# Research: Core Rule Library

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)  
**Created**: December 24, 2025  
**Purpose**: Resolve technical unknowns from Technical Context and provide implementation guidance for all 8 rules

## Overview

This research document consolidates findings for implementing 7 new code quality rules and enhancing the existing LongMethodRule. All findings are based on Roslyn API documentation, existing codebase analysis, and C# code smell detection best practices.

## Technical Decisions

### 1. Diagnostic Categories

**Decision**: Add new category constant "Code Smell" to DiagnosticCategories class

**Rationale**:
- Existing categories: Maintainability, Performance, Security, Style, Design, General
- Spec requires "Code Smell" category (distinct from Maintainability and Design)
- "Code Smell" is a universally recognized term from Martin Fowler's refactoring work
- Maps to rules: LongParameterListRule, ComplexConditionalRule, ExceptionSwallowingRule, LongMethodRule (enhanced)

**Implementation**:
```csharp
// Add to DiagnosticCategories.cs
/// <summary>
///     Code smells that indicate potential maintainability issues.
///     Examples: long parameter lists, deep nesting, exception swallowing.
/// </summary>
public const string CodeSmell = "Code Smell";
```

**Alternatives considered**:
- Reuse "Maintainability" - Rejected: too generic, code smells are a distinct subcategory
- Use "Refactoring" - Rejected: describes the solution, not the problem
- Keep as custom strings per rule - Rejected: violates consistency (DiagnosticCategories provides standardization)

---

### 2. Roslyn APIs for Rule Implementation

**Research Summary**: Analysis of Roslyn syntax tree APIs required for each rule

#### Long Parameter List Rule (US1)
**API**: `MethodDeclarationSyntax.ParameterList.Parameters.Count`  
**Alternative**: `BaseMethodDeclarationSyntax` (covers methods, constructors, operators)  
**Pattern**:
```csharp
var methods = root.DescendantNodes()
    .OfType<BaseMethodDeclarationSyntax>()
    .Where(m => m.ParameterList.Parameters.Count > 5);
```
**Edge Case - Extension Methods**: Check for `this` modifier on first parameter:
```csharp
bool isExtensionMethod = method.ParameterList.Parameters.FirstOrDefault()
    ?.Modifiers.Any(SyntaxKind.ThisKeyword) ?? false;
int effectiveParamCount = isExtensionMethod 
    ? method.ParameterList.Parameters.Count - 1 
    : method.ParameterList.Parameters.Count;
```

#### Complex Conditional Rule (US2)
**API**: Recursive tree traversal counting nested `IfStatementSyntax` and `SwitchStatementSyntax`  
**Pattern**:
```csharp
int CalculateNestingDepth(SyntaxNode node, int currentDepth = 0)
{
    if (node is IfStatementSyntax || node is SwitchStatementSyntax)
        currentDepth++;
    
    int maxDepth = currentDepth;
    foreach (var child in node.ChildNodes())
    {
        int childDepth = CalculateNestingDepth(child, currentDepth);
        maxDepth = Math.Max(maxDepth, childDepth);
    }
    return maxDepth;
}
```
**Note**: if-else chains at same level don't increase depth, only nesting within blocks

#### Magic Number Rule (US3)
**API**: `LiteralExpressionSyntax.Kind()` checking for `NumericLiteralExpression`  
**Exclusions**:
- Values 0, 1, -1: `literal.Token.ValueText`
- Named constants: Check parent is `VariableDeclaratorSyntax` with const modifier
- Attribute arguments: Acceptable framework patterns (requires heuristics or configuration)

**Pattern**:
```csharp
var literals = root.DescendantNodes()
    .OfType<LiteralExpressionSyntax>()
    .Where(l => l.Kind() == SyntaxKind.NumericLiteralExpression)
    .Where(l => !IsAcceptableLiteral(l));

bool IsAcceptableLiteral(LiteralExpressionSyntax literal)
{
    var value = literal.Token.ValueText;
    if (value == "0" || value == "1" || value == "-1") return true;
    
    // Check if part of const declaration
    var parent = literal.Parent;
    while (parent != null)
    {
        if (parent is VariableDeclaratorSyntax declarator)
        {
            var declaration = declarator.Parent?.Parent as FieldDeclarationSyntax;
            return declaration?.Modifiers.Any(SyntaxKind.ConstKeyword) ?? false;
        }
        parent = parent.Parent;
    }
    return false;
}
```

#### God Class Rule (US4)
**API**: 
- LOC: `SyntaxTree.GetText().Lines.Count` for class span
- Method count: `ClassDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>().Count()`

**Pattern**:
```csharp
var classes = root.DescendantNodes()
    .OfType<ClassDeclarationSyntax>();

foreach (var cls in classes)
{
    var span = cls.GetLocation().GetLineSpan();
    int lineCount = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
    
    // Count only actual method declarations, exclude properties
    int methodCount = cls.Members
        .OfType<MethodDeclarationSyntax>()
        .Count();
    
    if (lineCount > 500 || methodCount > 15)
        yield return diagnostic;
}
```
**Edge Case - Partial Classes**: Current limitation - analyze each partial independently (cross-file aggregation out of scope per FR-004 constraints)

#### Dead Code Rule (US5)
**API**: `SemanticModel.GetSymbolInfo()` and `SymbolFinder.FindReferencesAsync()`  
**Requirement**: Semantic analysis (not just syntax)  

**Pattern**:
```csharp
var semanticModel = compilation.GetSemanticModel(tree);
var privateMembers = root.DescendantNodes()
    .Where(n => n is MethodDeclarationSyntax || n is FieldDeclarationSyntax)
    .Where(n => HasPrivateModifier(n));

foreach (var member in privateMembers)
{
    var symbol = semanticModel.GetDeclaredSymbol(member);
    var references = /* find references within declaring type */;
    
    if (references.Count() == 0 && !IsInterfaceImplementation(symbol))
        yield return diagnostic;
}
```
**Note**: Requires Compilation object, not just SyntaxTree. May need AnalyzerEngine enhancement to pass SemanticModel.

#### Exception Swallowing Rule (US6)
**API**: `CatchClauseSyntax.Block.Statements.Count`  

**Pattern**:
```csharp
var catchClauses = root.DescendantNodes()
    .OfType<CatchClauseSyntax>()
    .Where(c => c.Block.Statements.Count == 0);
```
**Simplest rule** - purely syntactic, no semantic analysis required

#### Missing XML Documentation Rule (US7)
**API**:
- Check modifiers: `MemberDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword)`
- Check doc comment: `SyntaxNode.GetLeadingTrivia().Any(t => t.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia)`

**Pattern**:
```csharp
var publicMembers = root.DescendantNodes()
    .Where(n => n is ClassDeclarationSyntax || 
                n is MethodDeclarationSyntax || 
                n is PropertyDeclarationSyntax)
    .Where(n => HasPublicModifier(n))
    .Where(n => !HasXmlDocumentation(n));

bool HasXmlDocumentation(SyntaxNode node)
{
    return node.GetLeadingTrivia()
        .Any(t => t.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                  t.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia);
}
```
**Edge Case - inheritdoc**: Check trivia content for `<inheritdoc/>` tag:
```csharp
bool HasInheritDoc(SyntaxNode node)
{
    var docTrivia = node.GetLeadingTrivia()
        .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
    return docTrivia.ToString().Contains("<inheritdoc");
}
```

---

### 3. Rule ID Assignment

**Decision**: Sequential assignment starting from LNT002 (LNT001 is LongMethodRule)

| Rule | ID | Rationale |
|------|-----|-----------|
| LongMethodRule (existing) | LNT001 | Already assigned |
| LongParameterListRule | LNT002 | First new rule (US1, Priority P1) |
| ComplexConditionalRule | LNT003 | US2, Priority P1 |
| MagicNumberRule | LNT004 | US3, Priority P2 |
| GodClassRule | LNT005 | US4, Priority P2 |
| DeadCodeRule | LNT006 | US5, Priority P3 |
| ExceptionSwallowingRule | LNT007 | US6, Priority P1 |
| MissingXmlDocumentationRule | LNT008 | US7, Priority P3 |

**Alternatives considered**:
- Group by category (e.g., LNT-CS-001 for Code Smell) - Rejected: overly complex, harder to reference
- Use semantic prefixes (e.g., PARAM, COND, NUM) - Rejected: inconsistent with existing LNT001 pattern

---

### 4. Auto-Generated Code Detection

**Decision**: Use two detection strategies in combination

**Strategy 1 - File Name Pattern**: `*.Designer.cs`, `*.g.cs`, `*.Generated.cs`  
**Strategy 2 - Header Comment**: Check first 10 lines for `<auto-generated>` or `// <auto-generated />`

**Implementation**:
```csharp
bool IsGeneratedCode(SyntaxTree tree)
{
    // Check file name
    string fileName = Path.GetFileName(tree.FilePath);
    if (fileName.EndsWith(".Designer.cs") || 
        fileName.EndsWith(".g.cs") || 
        fileName.Contains(".Generated."))
        return true;
    
    // Check header comment
    var root = tree.GetRoot();
    var firstTrivia = root.GetLeadingTrivia().Take(10);
    return firstTrivia.Any(t => 
        t.ToString().Contains("<auto-generated>") ||
        t.ToString().Contains("<auto-generated />"));
}
```

**Application**: Each rule should check this at the start of Analyze() and return empty if true

---

### 5. Semantic Model Requirements

**Decision**: Dead Code Rule (LNT006) requires semantic analysis - AnalyzerEngine enhancement needed

**Current Limitation**: IAnalyzerRule.Analyze receives only SyntaxTree, not SemanticModel  
**Required Enhancement**: Two options:

**Option A - Extend IAnalyzerRule interface (BREAKING CHANGE)**:
```csharp
// Add overload to IAnalyzerRule
IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree, SemanticModel? semanticModel = null);
```

**Option B - New interface for semantic rules (PREFERRED)**:
```csharp
public interface ISemanticAnalyzerRule : IAnalyzerRule
{
    IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree, SemanticModel semanticModel);
}
```

**Recommendation**: Option B (new interface) - Avoids breaking existing rules, clear separation between syntax-only and semantic rules

**AnalyzerEngine Change Required**: Detect rule type and provide SemanticModel when available:
```csharp
if (rule is ISemanticAnalyzerRule semanticRule && semanticModel != null)
    results = semanticRule.Analyze(tree, semanticModel);
else
    results = rule.Analyze(tree);
```

**Impact**: Dead Code Rule deferred to separate story or this feature accepts limitation (detect only via syntax heuristics, not full semantic analysis)

**Final Decision for Feature 005**: Implement Dead Code Rule using syntax-only heuristics (detect unused members by checking for zero invocation syntax within same class). Full semantic analysis deferred to future enhancement. Document limitation in rule description.

---

### 6. Test Data Generation Strategy

**Decision**: Use CSharpSyntaxTree.ParseText() with raw C# strings for test fixtures

**Pattern** (from existing LongMethodRuleTests.cs):
```csharp
private static SyntaxTree CreateSyntaxTree(string code)
{
    return CSharpSyntaxTree.ParseText(code, path: "TestFile.cs");
}

[Fact]
public void Analyze_MethodWithSixParameters_ReturnsDiagnostic()
{
    // Arrange
    var code = @"
        public class TestClass
        {
            public void MethodWithSixParams(int a, int b, int c, int d, int e, int f)
            {
            }
        }";
    var tree = CreateSyntaxTree(code);
    var rule = new LongParameterListRule();
    
    // Act
    var results = rule.Analyze(tree).ToList();
    
    // Assert
    results.Should().ContainSingle();
    results[0].RuleId.Should().Be("LNT002");
    results[0].Severity.Should().Be(Severity.Warning);
}
```

**Advantages**:
- No external test files required
- Test is self-contained and readable
- Easy to modify test cases
- Fast execution (in-memory parsing)

---

### 7. Performance Considerations

**Requirement**: <500ms per rule for 1000-line file

**Findings**:
- Roslyn syntax tree traversal is highly optimized (C++ implementation)
- DescendantNodes() is lazy (IEnumerable) - no performance issue
- Where() clauses are LINQ deferred execution - minimal overhead
- Expected performance: 10-50ms per rule for 1000-line file on modern hardware

**Optimization Tips**:
- Use `DescendantNodes()` not `DescendantNodesAndSelf()` unless root needed
- Filter early: `.OfType<T>()` before `.Where()` clauses
- Avoid redundant tree traversals: cache results if multiple passes needed
- Use `yield return` for lazy evaluation (already pattern in LongMethodRule)

**Validation**: Add performance test to verify <500ms threshold (similar to existing Constitution_Testability_InMemoryTestingWorks test in Feature 004)

---

## Research Findings by Rule

### LNT002 - Long Parameter List Rule

**Best Practices Reference**: Clean Code (Martin), Refactoring (Fowler) - recommend max 3-4 parameters  
**Industry Standard**: 5 parameters (spec requirement) - balanced between strictness and practicality  
**Common Violations**: Constructors with many dependencies, API methods with excessive options  
**Fix Patterns**:
1. Parameter Object: `CreateUser(string name, string email)` → `CreateUser(UserInfo info)`
2. Builder Pattern: Fluent API for complex construction
3. Method Decomposition: Split into smaller, focused methods

**Diagnostic Message Template**: "Method '{0}' has {1} parameters (max: 5). Consider using a parameter object or builder pattern."

---

### LNT003 - Complex Conditional Rule

**Best Practices Reference**: Cyclomatic Complexity research (McCabe) - max depth 3-4  
**Rationale**: Each nesting level doubles cognitive load; >3 levels exponentially harder to test  
**Common Violations**: Nested validation logic, complex business rules, state machine implementations  
**Fix Patterns**:
1. Guard Clauses: Early returns to flatten nesting
2. Extract Method: Move nested logic to named helper methods
3. Strategy Pattern: Replace complex conditionals with polymorphism
4. Specification Pattern: Composable business rules

**Diagnostic Message Template**: "Conditional nesting depth is {0} (max: 3). Consider using guard clauses or extracting to separate methods."

---

### LNT004 - Magic Number Rule

**Best Practices Reference**: Code Complete (McConnell), Clean Code (Martin)  
**Acceptable Literals**: 0, 1, -1 (mathematical identities), array indices, loop counters  
**Gray Areas** (out of scope for v1): 
- Framework constants (e.g., HTTP status 200, MaxLength 255)
- Mathematical constants (pi, e) - should use Math.PI, Math.E
- Business constants (e.g., tax rates) - definitely magic numbers

**Fix Patterns**:
1. Named Constants: `const int MaxRetries = 3;`
2. Configuration: Move to appsettings.json for values that may change
3. Enum: Replace numeric status codes with enum

**Diagnostic Message Template**: "Magic number '{0}' should be replaced with a named constant. Consider extracting to a const field or readonly property."

---

### LNT005 - God Class Rule

**Best Practices Reference**: Single Responsibility Principle (SOLID), Clean Code  
**Thresholds**: 500 LOC OR 15 methods (spec requirement)  
**Industry Context**: 
- 500 LOC: Conservative estimate (some sources say 200-300)
- 15 methods: Allows moderate class size while flagging God Classes

**Common Violations**: Service classes, Manager classes, Utility classes  
**Fix Patterns**:
1. Extract Class: Group related methods into cohesive classes
2. Facade Pattern: Split implementation from public interface
3. Delegate Pattern: Compose from smaller, focused classes

**Diagnostic Message Template**: "Class '{0}' has {1} lines (max: 500) / {2} methods (max: 15). Consider splitting into smaller, focused classes by responsibility."

---

### LNT006 - Dead Code Rule

**Best Practices Reference**: Clean Code, Pragmatic Programmer ("Don't live with broken windows")  
**Detection Strategy** (syntax-only for Feature 005):
- Find private methods/fields
- Search for references within same class using syntax tree
- Flag if zero references found
- Exclude: Fields used only in initializer are dead code

**Limitations** (documented):
- Cannot detect unused methods called via reflection
- Cannot detect unused interface implementations (excluded per FR-022)
- Cross-file analysis not performed (public methods may be unused but undetected)

**Fix Pattern**: Remove unused code (safe for private members, risky for public APIs)

**Diagnostic Message Template**: "Private {0} '{1}' is never used. Consider removing dead code."

---

### LNT007 - Exception Swallowing Rule

**Best Practices Reference**: Framework Design Guidelines (Microsoft), Effective C# (Wagner)  
**Rationale**: Empty catch blocks hide errors, cause silent failures, complicate debugging  
**Acceptable Patterns** (not flagged):
```csharp
catch (Exception ex) { Logger.Error(ex); } // Logging
catch { throw; }                           // Re-throw
catch (Exception ex) when (Filter(ex)) { } // Exception filter (C# 6+)
```

**Violations**:
```csharp
catch { }                    // Empty catch
catch (Exception) { }        // Empty with exception
catch { /* TODO: handle */ } // Comment but no code
```

**Edge Case - Cancellation**: `catch (OperationCanceledException) { }` technically empty but intentional. Feature 005 flags it (can be whitelisted in future configuration feature).

**Fix Patterns**:
1. Log the exception
2. Re-throw: `throw;` or `throw new CustomException("message", ex);`
3. Handle gracefully: Return default value, show error UI, etc.

**Diagnostic Message Template**: "Empty catch block suppresses exceptions. Consider logging the exception or removing the try-catch if error handling is not needed."

---

### LNT008 - Missing XML Documentation Rule

**Best Practices Reference**: Framework Design Guidelines (Microsoft) - all public APIs must be documented  
**Rationale**: IntelliSense relies on XML docs, public APIs are contracts requiring documentation  
**Scope**: Classes, methods, properties, events (public or protected accessibility)  
**Exclusions**: Private members, internal members (configurable in future), test code

**Acceptable Documentation**:
```csharp
/// <summary>Description</summary>
public class Foo { }

/// <inheritdoc />
public override string ToString() { }

/// <summary>Does something</summary>
/// <param name="x">The x value</param>
public void DoSomething(int x) { }
```

**Fix Pattern**: Add XML doc comment with /// summary tag

**Diagnostic Message Template**: "Public {0} '{1}' is missing XML documentation. Add a /// <summary> comment to describe the API."

---

### LNT001 - Long Method Rule Enhancement

**Current State**: Basic implementation exists, needs category and message updates  
**Required Changes**:
1. Category: Update from `DiagnosticCategories.Maintainability` to `DiagnosticCategories.CodeSmell`
2. Message: Add fix guidance: "Method '{0}' has {1} statements (max: 20). Consider extracting logical blocks into separate methods."
3. Tests: Update assertions to expect new category

**No algorithmic changes** - enhancement is purely metadata and messaging

---

## Risk Analysis

### High Risk
None - all patterns are well-understood Roslyn APIs with existing usage in LongMethodRule

### Medium Risk
1. **Dead Code Rule semantic analysis** - Mitigated by deferring full semantic analysis, using syntax heuristics
2. **Magic Number false positives on framework constants** - Mitigated by documenting as known limitation, deferring configuration to future

### Low Risk
1. **Performance on very large files** - Mitigated by Roslyn's efficient tree traversal, validated by performance tests
2. **Edge cases on partial classes** - Mitigated by documenting behavior (analyze each partial independently)

---

## Open Questions (Resolved)

~~1. Should Dead Code Rule require semantic analysis?~~
- **RESOLVED**: Use syntax-only heuristics for Feature 005. Document limitation. Full semantic analysis deferred.

~~2. Should Magic Number Rule flag attribute arguments?~~
- **RESOLVED**: Flag by default. Framework-standard values (e.g., MaxLength 255) flagged as well. Configuration deferred to future.

~~3. Should we add "Code Smell" category to DiagnosticCategories?~~
- **RESOLVED**: Yes, add new constant. Distinct from Maintainability and Design.

~~4. Should Documentation Rule check internal members?~~
- **RESOLVED**: No, only public and protected. Internal member documentation deferred to future configuration.

---

## Implementation Priority

Based on spec priorities and technical complexity:

**Phase 1 - P1 Rules (Implement First)**:
1. LNT007 - Exception Swallowing (simplest, pure syntax)
2. LNT002 - Long Parameter List (simple, pure syntax)
3. LNT003 - Complex Conditional (moderate, recursive traversal)

**Phase 2 - P2 Rules**:
4. LNT004 - Magic Number (moderate, exclusion logic)
5. LNT005 - God Class (simple, counting)
6. LNT001 - Long Method Enhancement (trivial, metadata changes)

**Phase 3 - P3 Rules**:
7. LNT008 - Missing XML Documentation (moderate, trivia inspection)
8. LNT006 - Dead Code (complex, syntax-based reference finding)

---

## Conclusion

All technical unknowns from Technical Context have been resolved. No NEEDS CLARIFICATION markers remain. Implementation can proceed to Phase 1 (Design & Contracts) with clear technical guidance for all 8 rules.

**Key Takeaways**:
- All rules implementable with existing Roslyn APIs and infrastructure
- No breaking changes to IAnalyzerRule or AnalyzerEngine required
- Add one new DiagnosticCategories constant: "Code Smell"
- Dead Code Rule uses syntax heuristics (not full semantic analysis)
- Performance requirement (<500ms) easily achievable with Roslyn
- Test strategy: in-memory syntax trees with raw C# strings
