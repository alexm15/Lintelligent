# Data Model: Language-Ext Monad Detection

**Feature**: 022-monad-analyzer  
**Phase**: 1 (Design)  
**Date**: 2024-12-26

## Diagnostic Specifications

### LNT200: Nullable to Option<T>

**ID**: `LNT200`  
**Title**: Consider using Option<T> for nullable return type  
**Severity**: Info  
**Category**: Functional

**Message Format**:
```
Method '{methodName}' returns nullable - consider Option<T> for safer null handling
```

**Description Template**:
```
### Current Code
public {returnType}? {methodName}({parameters})
{
    {methodBody}
}

### Suggested Refactoring
public Option<{returnType}> {methodName}({parameters})
{
    // Replace "return null;" with "return Option<{returnType}>.None;"
    // Replace "return value;" with "return Option<{returnType}>.Some(value);"
}

### Benefits
- Eliminates null reference exceptions at compile time
- Forces caller to explicitly handle missing value case
- Self-documenting: signature shows value may be absent
- Enables functional composition with Map, Bind, Match

### When to Apply
Use Option<T> when:
- Method logically may not return a value (e.g., search, lookup)
- Null is a valid semantic result (not an error condition)
- Multiple null checks exist in method body ({nullCheckCount} found)

### Additional Resources
- language-ext Option<T> docs: https://github.com/louthy/language-ext/wiki/Option
```

**Trigger Conditions** (from research.md):
1. Method return type has `NullableAnnotation.Annotated`, AND
2. Method contains 3+ null checks OR null returns, AND
3. EditorConfig `language_ext_monad_detection = true`, AND
4. `LanguageExt.Core` assembly is referenced

**Location**: Report on method declaration identifier

---

### LNT201: Try/Catch to Either<L, R>

**ID**: `LNT201`  
**Title**: Consider using Either<L, R> for error handling  
**Severity**: Info  
**Category**: Functional

**Message Format**:
```
Method '{methodName}' uses try/catch for control flow - consider Either<Error, T> for explicit error handling
```

**Description Template**:
```
### Current Code
public {returnType} {methodName}({parameters})
{
    try
    {
        {tryBody}
        return successValue;
    }
    catch ({exceptionType} ex)
    {
        return errorValue;
    }
}

### Suggested Refactoring
public Either<Error, {successType}> {methodName}({parameters})
{
    try
    {
        {tryBody}
        return Right(successValue);
    }
    catch ({exceptionType} ex)
    {
        return Left(Error.New(ex));
    }
}

### Benefits
- Makes error handling explicit in method signature
- Prevents unhandled exceptions from propagating
- Enables Railway-Oriented Programming patterns
- Caller cannot ignore error case (no silent failures)

### When to Apply
Use Either<L, R> when:
- Try/catch returns different values in success/error branches
- Exceptions are expected conditions (not exceptional)
- Multiple error types need distinct handling
- Error context must be preserved for caller

### Additional Resources
- language-ext Either<L, R> docs: https://github.com/louthy/language-ext/wiki/Either
- Railway-Oriented Programming: https://fsharpforfunandprofit.com/rop/
```

**Trigger Conditions**:
1. Try/catch statement exists in method, AND
2. Catch block contains return statement (not just rethrow/log), AND
3. Try block also contains return statement (returns in both paths), AND
4. EditorConfig `language_ext_monad_detection = true`, AND
5. `LanguageExt.Core` assembly is referenced

**Location**: Report on try statement keyword

---

### LNT202: Sequential Validation to Validation<T>

**ID**: `LNT202`  
**Title**: Consider using Validation<T> for accumulating validations  
**Severity**: Info  
**Category**: Functional

**Message Format**:
```
Method '{methodName}' has {validationCount} sequential validations - consider Validation<T> to accumulate errors
```

**Description Template**:
```
### Current Code
public Result<{successType}> {methodName}({parameters})
{
    if (!IsValid(check1)) return Error("Validation 1 failed");
    if (!IsValid(check2)) return Error("Validation 2 failed");
    if (!IsValid(check3)) return Error("Validation 3 failed");
    
    return Success(result);
}

### Suggested Refactoring
public Validation<Error, {successType}> {methodName}({parameters})
{
    var validation1 = Validate(check1, "Validation 1 failed");
    var validation2 = Validate(check2, "Validation 2 failed");
    var validation3 = Validate(check3, "Validation 3 failed");
    
    return (validation1, validation2, validation3)
        .Apply((v1, v2, v3) => CreateResult(v1, v2, v3));
}

### Benefits
- Accumulates ALL validation errors (not just first failure)
- Provides complete feedback to user in single request
- Applicative functor pattern enables parallel validation
- Better UX: user sees all issues at once

### When to Apply
Use Validation<T> when:
- Multiple independent validation checks exist ({validationCount} found)
- All failures should be reported simultaneously
- Validations are independent (order doesn't matter)
- User experience requires showing all errors at once

### Additional Resources
- language-ext Validation<T> docs: https://github.com/louthy/language-ext/wiki/Validation
```

**Trigger Conditions**:
1. Method contains 2+ sequential if statements with return, AND
2. Return statements in if blocks return error values (string, Error, ValidationError), AND
3. Sequential validations are independent (no data flow between them), AND
4. EditorConfig `language_ext_monad_detection = true`, AND
5. `LanguageExt.Core` assembly is referenced

**Location**: Report on first if statement in validation sequence

---

### LNT203: Exception-based Flow to Try<T>

**ID**: `LNT203`  
**Title**: Consider using Try<T> for exception-prone operations  
**Severity**: Info  
**Category**: Functional

**Message Format**:
```
Method '{methodName}' may throw {exceptionType} - consider Try<T> for safer error handling
```

**Description Template**:
```
### Current Code
public {returnType} {methodName}({parameters})
{
    // May throw {exceptionType}
    {operationBody}
}

### Suggested Refactoring
public Try<{returnType}> {methodName}({parameters})
{
    return () => 
    {
        {operationBody}
    };
}

// Usage
var result = {methodName}({args})
    .Match(
        Succ: value => ProcessSuccess(value),
        Fail: ex => HandleError(ex)
    );

### Benefits
- Converts exceptions to values for functional composition
- Lazy evaluation: exception thrown only when result is matched
- Eliminates try/catch boilerplate in caller code
- Composable with other monads (Option, Either, Validation)

### When to Apply
Use Try<T> when:
- Operation calls external APIs that may throw
- File I/O, network calls, parsing operations
- Third-party library calls with poor error handling
- Want lazy evaluation of potentially failing computation

### Additional Resources
- language-ext Try<T> docs: https://github.com/louthy/language-ext/wiki/Try
```

**Trigger Conditions**:
1. Method may throw exceptions (calls I/O operations, parsing, reflection), OR
2. Method has XML doc comment with `<exception>` tag, AND
3. Method does not already use try/catch internally, AND
4. EditorConfig `language_ext_monad_detection = true`, AND
5. `LanguageExt.Core` assembly is referenced

**Location**: Report on method declaration identifier

**Note**: LNT203 is lower priority (US4-P4). May be deferred to future iteration if complexity is high.

---

## Configuration Schema

### EditorConfig Settings

```ini
# .editorconfig

[*.cs]

# Enable/disable language-ext monad detection (default: false)
language_ext_monad_detection = true

# Minimum complexity threshold for suggestions (default: 3)
# - Option<T>: minimum null checks or null returns
# - Validation<T>: minimum sequential validations
language_ext_min_complexity = 3

# Specific monad types to detect (default: all)
# Comma-separated list: option, either, validation, try
language_ext_enabled_monads = option, either, validation

# Severity override (default: info)
dotnet_diagnostic.LNT200.severity = suggestion
dotnet_diagnostic.LNT201.severity = suggestion
dotnet_diagnostic.LNT202.severity = suggestion
dotnet_diagnostic.LNT203.severity = none  # Disable Try<T> detection
```

### Configuration Validation

**Rule**: If `language_ext_monad_detection = false`, all LNT200-LNT203 diagnostics are suppressed.

**Rule**: If `language_ext_min_complexity` is set higher, fewer diagnostics are reported (e.g., setting to 5 means only methods with 5+ null checks trigger LNT200).

**Rule**: If specific monad type is disabled via `language_ext_enabled_monads`, corresponding diagnostic is suppressed.

---

## Diagnostic Properties

Each diagnostic includes properties for code fix support (future iteration):

```csharp
var properties = ImmutableDictionary.CreateBuilder<string, string>();
properties.Add("MonadType", "Option");  // Option | Either | Validation | Try
properties.Add("CurrentPattern", "nullable");  // Pattern detected
properties.Add("SuggestedType", "Option<T>");  // Suggested replacement
properties.Add("ComplexityScore", nullCheckCount.ToString());  // Metric
```

**Note**: Code fixes not implemented in Phase 1. Properties reserved for future use.

---

## Entity Relationships

```
DiagnosticDescriptor (LNT200-LNT203)
    ↓ has
DiagnosticResult
    ↓ contains
Location (method/statement position)
    ↓ references
SyntaxNode (MethodDeclarationSyntax, TryStatementSyntax, etc.)
    ↓ analyzed by
SemanticModel
    ↓ provides
ITypeSymbol (return type, exception types)
```

---

## Validation Rules

**VR-001**: Diagnostic ID must be unique across all Lintelligent rules  
**VR-002**: Severity must be Info (suggestions, not errors/warnings)  
**VR-003**: Description must include before/after code examples  
**VR-004**: Trigger conditions must check EditorConfig opt-in  
**VR-005**: Trigger conditions must verify LanguageExt.Core reference  
**VR-006**: Complexity threshold must be configurable via EditorConfig  
**VR-007**: Diagnostic properties must include MonadType for future code fixes  

---

**Status**: Data model complete. Defines all diagnostics, configuration schema, and validation rules. Ready for contract generation.
