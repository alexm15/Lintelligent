# Core Rule Library - Rules Documentation

**Feature**: 005-core-rule-library  
**Status**: Complete  
**Created**: December 2025  
**Total Rules**: 8

## Overview

This document provides a comprehensive reference for all analyzer rules implemented in the Core Rule Library. Each rule detects specific code quality issues and provides actionable guidance for developers.

---

## LNT001 - Long Method Rule

### What It Detects

Methods with more than 20 statements.

### Why It Matters

Long methods are harder to understand, test, and maintain. They often violate the Single Responsibility Principle by doing too many things. Breaking them into smaller, focused methods improves code readability and reusability.

### How It Works

- Counts all statement syntax nodes within the method body
- Triggers when count exceeds 20 statements
- Reports the method name and exact statement count
- Provides location of the method declaration

### Example Violation

```csharp
public void ProcessOrder(Order order)
{
    // 25 statements doing validation, calculation, persistence, notification
    ValidateOrder(order);
    CalculateTotal(order);
    ApplyDiscounts(order);
    SaveToDatabase(order);
    SendConfirmationEmail(order);
    UpdateInventory(order);
    LogTransaction(order);
    // ... 18 more statements
}
```

### Suggested Fix

Extract logical blocks into separate methods:

```csharp
public void ProcessOrder(Order order)
{
    ValidateOrder(order);
    CalculateOrderTotal(order);
    SaveOrder(order);
    NotifyCustomer(order);
}
```

### Configuration

- **ID**: LNT001
- **Severity**: Warning
- **Category**: Code Smell
- **Threshold**: 20 statements (not configurable in MVP)

---

## LNT002 - Long Parameter List Rule

### What It Detects

Methods (including constructors) with more than 5 parameters.

### Why It Matters

Long parameter lists are difficult to understand and use correctly. They often indicate that the method is doing too much or that related parameters should be grouped into a cohesive object.

### How It Works

- Counts parameters in method and constructor declarations
- Excludes the `this` parameter for extension methods
- Triggers when count exceeds 5 parameters
- Reports the method name and exact parameter count

### Example Violation

```csharp
public void CreateUser(
    string firstName,
    string lastName,
    string email,
    string phone,
    DateTime birthDate,
    string address) // 6 parameters - VIOLATION
{
    // ...
}
```

### Suggested Fix

Use a parameter object or builder pattern:

```csharp
public void CreateUser(UserRegistrationData userData)
{
    // ...
}

public class UserRegistrationData
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime BirthDate { get; set; }
    public string Address { get; set; }
}
```

### Configuration

- **ID**: LNT002
- **Severity**: Warning
- **Category**: Code Smell
- **Threshold**: 5 parameters (not configurable in MVP)

---

## LNT003 - Complex Conditional Rule

### What It Detects

Conditional statements (if/switch) nested more than 3 levels deep.

### Why It Matters

Deeply nested conditionals are hard to understand and test. They often indicate complex control flow that should be simplified using guard clauses, early returns, or extracted methods.

### How It Works

- Recursively calculates nesting depth for each if/switch statement
- Counts both `if` and `switch` statements as nesting levels
- Does NOT count else-if chains as nesting (they're at the same level)
- Triggers when depth exceeds 3 levels
- Reports the exact nesting depth

### Example Violation

```csharp
if (user != null)                   // Depth 1
{
    if (user.IsActive)              // Depth 2
    {
        if (user.HasPermission)     // Depth 3
        {
            if (order.IsValid)      // Depth 4 - VIOLATION
            {
                ProcessOrder(order);
            }
        }
    }
}
```

### Suggested Fix

Use guard clauses and early returns:

```csharp
if (user == null) return;
if (!user.IsActive) return;
if (!user.HasPermission) return;
if (!order.IsValid) return;

ProcessOrder(order);
```

### Configuration

- **ID**: LNT003
- **Severity**: Warning
- **Category**: Code Smell
- **Threshold**: 3 levels (not configurable in MVP)

---

## LNT004 - Magic Number Rule

### What It Detects

Hardcoded numeric literals (except 0, 1, -1) that should be named constants.

### Why It Matters

Magic numbers make code harder to understand and maintain. Using named constants improves readability and makes it easier to change values consistently.

### How It Works

- Finds all numeric literal expressions (int, long, float, double)
- Excludes common values: 0, 1, -1
- Excludes literals in const declarations
- Triggers for all other numeric literals
- Reports the literal value for context

### Example Violation

```csharp
public void ProcessData()
{
    Thread.Sleep(5000);           // Magic number - VIOLATION
    var buffer = new byte[1024];  // Magic number - VIOLATION
    var threshold = 3.14159;      // Magic number - VIOLATION
}
```

### Suggested Fix

Replace with named constants:

```csharp
private const int TimeoutMilliseconds = 5000;
private const int BufferSize = 1024;
private const double CircleAreaThreshold = Math.PI;

public void ProcessData()
{
    Thread.Sleep(TimeoutMilliseconds);
    var buffer = new byte[BufferSize];
    var threshold = CircleAreaThreshold;
}
```

### Configuration

- **ID**: LNT004
- **Severity**: Info
- **Category**: Code Smell
- **Excluded Values**: 0, 1, -1

---

## LNT005 - God Class Rule

### What It Detects

Classes that are too large: more than 500 lines of code OR more than 15 methods.

### Why It Matters

God classes violate the Single Responsibility Principle by doing too many things. They are hard to understand, test, and maintain. Splitting them into smaller, focused classes improves code organization and reusability.

### How It Works

- Calculates line count from class start to end (including braces, comments, whitespace)
- Counts explicit method declarations (excludes constructors, properties, auto-property accessors)
- Triggers if EITHER threshold is exceeded
- Reports both metrics when both are violated
- Analyzes partial classes independently (limitation)

### Example Violation

```csharp
public class OrderService // 520 lines, 18 methods - VIOLATION
{
    // Method 1-18: validation, calculation, persistence, 
    // notification, inventory, shipping, payments, analytics...
}
```

### Suggested Fix

Split into focused classes by responsibility:

```csharp
public class OrderValidator { }
public class OrderCalculator { }
public class OrderRepository { }
public class OrderNotifier { }
public class InventoryService { }
public class ShippingService { }
public class PaymentProcessor { }
```

### Configuration

- **ID**: LNT005
- **Severity**: Warning
- **Category**: Design
- **Line Threshold**: 500 lines
- **Method Threshold**: 15 methods

---

## LNT006 - Dead Code Rule

### What It Detects

Unused private methods and fields that are never referenced within the declaring class.

### Why It Matters

Dead code clutters the codebase, confuses developers, and increases maintenance burden. Removing it improves code clarity and reduces the risk of bugs.

### How It Works

- Finds all private methods and fields
- Searches for references within the same class using syntax-based analysis
- Excludes private members when class implements interfaces (heuristic to avoid false positives)
- Flags fields used only in their initializer as unused
- Triggers when no references are found

### Known Limitations

- Cannot detect reflection-based usage
- Cannot detect cross-file usage of public members
- Uses heuristic for interface detection (not full semantic analysis)

### Example Violation

```csharp
public class Calculator
{
    private int _unusedField;               // Never referenced - VIOLATION
    
    private void UnusedHelper()             // Never called - VIOLATION
    {
        // Implementation
    }
    
    public int Add(int a, int b)
    {
        return a + b; // Doesn't use _unusedField or call UnusedHelper
    }
}
```

### Suggested Fix

Remove unused members:

```csharp
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

### Configuration

- **ID**: LNT006
- **Severity**: Info
- **Category**: Maintainability
- **Scope**: Private members only

---

## LNT007 - Exception Swallowing Rule

### What It Detects

Empty catch blocks that suppress exceptions without handling them.

### Why It Matters

Swallowing exceptions silently makes debugging nearly impossible. Errors occur but leave no trace, leading to silent failures and data corruption. Always handle exceptions explicitly - log them, re-throw them, or remove the try-catch if error handling isn't needed.

### How It Works

- Finds all catch clauses in the syntax tree
- Checks if the catch block has zero statements
- Triggers for empty blocks (including blocks with only comments)
- Does NOT flag catch blocks with throw, logging, or other statements

### Example Violation

```csharp
try
{
    RiskyOperation();
}
catch (Exception)
{
    // Empty - suppresses all errors silently - VIOLATION
}

try
{
    AnotherOperation();
}
catch
{
    // TODO: handle this - VIOLATION (comment-only, no code)
}
```

### Suggested Fix

Log the exception or remove the try-catch:

```csharp
// Option 1: Log and re-throw
try
{
    RiskyOperation();
}
catch (Exception ex)
{
    _logger.Error(ex, "Operation failed");
    throw;
}

// Option 2: Remove try-catch if not needed
RiskyOperation(); // Let caller handle errors
```

### Configuration

- **ID**: LNT007
- **Severity**: Warning
- **Category**: Code Smell
- **Detection**: Zero statements in catch block

---

## LNT008 - Missing XML Documentation Rule

### What It Detects

Public and protected classes, methods, and properties without XML documentation comments.

### Why It Matters

Public APIs should be documented so consumers understand how to use them. XML documentation enables IntelliSense tooltips in IDEs and can be used to generate API reference documentation.

### How It Works

- Checks all classes, methods, and properties
- Only flags public and protected members (not private, internal)
- Accepts `/// <summary>` as valid documentation
- Accepts `/// <inheritdoc />` as valid documentation for inherited members
- Rejects regular `//` comments (must be XML doc format)

### Example Violation

```csharp
public class OrderService               // No XML doc - VIOLATION
{
    public void ProcessOrder(Order order)   // No XML doc - VIOLATION
    {
        // Implementation
    }
    
    public decimal Total { get; set; }      // No XML doc - VIOLATION
}
```

### Suggested Fix

Add XML documentation comments:

```csharp
/// <summary>
/// Service for processing customer orders.
/// </summary>
public class OrderService
{
    /// <summary>
    /// Processes the specified order and updates inventory.
    /// </summary>
    /// <param name="order">The order to process.</param>
    public void ProcessOrder(Order order)
    {
        // Implementation
    }
    
    /// <summary>
    /// Gets or sets the total amount for the current transaction.
    /// </summary>
    public decimal Total { get; set; }
}
```

### Configuration

- **ID**: LNT008
- **Severity**: Info
- **Category**: Documentation
- **Scope**: Public and protected members only

---

## Cross-Cutting Concerns

### Generated Code Detection

All rules automatically skip generated code files to avoid false positives. A file is considered generated if:

- Filename ends with `.Designer.cs`, `.g.cs`, or contains `.Generated.`
- File has `<auto-generated>` or `<auto-generated />` in the first 10 trivia tokens

### Performance

All rules are designed to complete analysis in under 500ms per rule for a 1000-line file on modern hardware. Performance is achieved through:

- Syntax-only analysis (no semantic model required for most rules)
- Single-pass tree traversal per rule
- Early termination for generated code
- Efficient LINQ queries over syntax nodes

### Test Coverage

All rules have comprehensive test coverage:

- Minimum 7 tests per rule
- Coverage includes: positive cases, negative cases, boundary conditions, edge cases, metadata verification, generated code exclusion
- Current coverage: ≥95% line coverage across all rules

---

## Summary Table

| Rule ID | Name                      | Priority | Severity | Category       | Threshold       |
|---------|---------------------------|----------|----------|----------------|-----------------|
| LNT001  | Long Method               | P2       | Warning  | Code Smell     | 20 statements   |
| LNT002  | Long Parameter List       | P1       | Warning  | Code Smell     | 5 parameters    |
| LNT003  | Complex Conditional       | P1       | Warning  | Code Smell     | 3 nesting levels|
| LNT004  | Magic Number              | P2       | Info     | Code Smell     | Exclude 0,1,-1  |
| LNT005  | God Class                 | P2       | Warning  | Design         | 500 LOC/15 methods |
| LNT006  | Dead Code                 | P3       | Info     | Maintainability| Private members |
| LNT007  | Exception Swallowing      | P1       | Warning  | Code Smell     | Zero statements |
| LNT008  | Missing XML Documentation | P3       | Info     | Documentation  | Public members  |

---

## Future Enhancements

Potential improvements for future iterations:

1. **Configurable Thresholds**: Allow users to customize thresholds (e.g., max statements, max parameters)
2. **Auto-Fix Capabilities**: Provide automated code fixes for some violations (e.g., extract method, create parameter object)
3. **Semantic Analysis**: Use semantic model for more accurate dead code detection (cross-file analysis, reflection detection)
4. **Partial Class Aggregation**: Analyze partial classes as a single unit for God Class detection
5. **Rule Suppression**: Support `[SuppressRule]` attributes and `#pragma` directives
6. **Custom Messages**: Allow customization of diagnostic messages
7. **Localization**: Support multiple languages for diagnostic messages
8. **Integration**: VS Code/Visual Studio integration for real-time analysis

---

## References

- [Specification](spec.md) - User stories and functional requirements
- [Technical Plan](plan.md) - Architecture and design decisions
- [API Contracts](contracts/rule-contracts.md) - Detailed rule contracts
- [Implementation Guide](quickstart.md) - Step-by-step implementation guide
- [Research](research.md) - Technical research and decision rationale
