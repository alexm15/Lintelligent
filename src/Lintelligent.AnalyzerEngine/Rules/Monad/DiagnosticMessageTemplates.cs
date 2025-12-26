namespace Lintelligent.AnalyzerEngine.Rules.Monad;

/// <summary>
///     Educational message templates for monad detection diagnostics.
///     Each template includes explanation, before/after examples, and usage guidance.
/// </summary>
public static class DiagnosticMessageTemplates
{
    /// <summary>
    ///     Template for LNT200: Nullable to Option&lt;T&gt; diagnostic.
    ///     Explains benefits of Option&lt;T&gt; over nullable types for safer null handling.
    /// </summary>
    public static string GetOptionTemplate(string methodName, string returnType, int nullCheckCount)
    {
        return $@"Method '{methodName}' returns nullable - consider Option<T> for safer null handling

### Current Code
public {returnType}? {methodName}(...)
{{
    // Returns null when value not found
    return null;
}}

### Suggested Refactoring
public Option<{returnType}> {methodName}(...)
{{
    // Replace ""return null;"" with ""return Option<{returnType}>.None;""
    // Replace ""return value;"" with ""return Option<{returnType}>.Some(value);""
}}

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
- language-ext Option<T> docs: https://github.com/louthy/language-ext/wiki/Option";
    }

    /// <summary>
    ///     Template for LNT201: Try/Catch to Either&lt;L, R&gt; diagnostic.
    ///     Explains benefits of Either&lt;L, R&gt; for explicit error handling.
    /// </summary>
    public static string GetEitherTemplate(string methodName, string exceptionType)
    {
        return $@"Method '{methodName}' uses try/catch for control flow - consider Either<Error, T> for explicit error handling

### Current Code
public Result {methodName}(...)
{{
    try
    {{
        // Perform operation
        return successValue;
    }}
    catch ({exceptionType} ex)
    {{
        return errorValue;
    }}
}}

### Suggested Refactoring
public Either<Error, Result> {methodName}(...)
{{
    try
    {{
        // Perform operation
        return Right(successValue);
    }}
    catch ({exceptionType} ex)
    {{
        return Left(Error.New(ex));
    }}
}}

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
- Railway-Oriented Programming: https://fsharpforfunandprofit.com/rop/";
    }

    /// <summary>
    ///     Template for LNT202: Sequential Validation to Validation&lt;T&gt; diagnostic.
    ///     Explains benefits of Validation&lt;T&gt; for accumulating errors.
    /// </summary>
    public static string GetValidationTemplate(string methodName, int validationCount)
    {
        return $@"Method '{methodName}' has {validationCount} sequential validations - consider Validation<T> to accumulate errors

### Current Code
public Result Validate{methodName}(...)
{{
    if (!IsValid(check1)) return Error(""Validation 1 failed"");
    if (!IsValid(check2)) return Error(""Validation 2 failed"");
    if (!IsValid(check3)) return Error(""Validation 3 failed"");
    
    return Success(result);
}}

### Suggested Refactoring
public Validation<Error, Result> Validate{methodName}(...)
{{
    var validation1 = Validate(check1, ""Validation 1 failed"");
    var validation2 = Validate(check2, ""Validation 2 failed"");
    var validation3 = Validate(check3, ""Validation 3 failed"");
    
    return (validation1, validation2, validation3)
        .Apply((v1, v2, v3) => CreateResult(v1, v2, v3));
}}

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
- language-ext Validation<T> docs: https://github.com/louthy/language-ext/wiki/Validation";
    }

    /// <summary>
    ///     Template for LNT203: Exception-based Flow to Try&lt;T&gt; diagnostic.
    ///     Explains benefits of Try&lt;T&gt; for exception-prone operations.
    /// </summary>
    public static string GetTryTemplate(string methodName, string exceptionType)
    {
        return $@"Method '{methodName}' throws {exceptionType} - consider Try<T> for exception wrapping

### Current Code
public Result {methodName}(...)
{{
    // Operation that may throw
    if (errorCondition)
    {{
        throw new {exceptionType}(""Operation failed"");
    }}
    return successValue;
}}

### Suggested Refactoring
public Try<Result> {methodName}(...)
{{
    return () => {{
        // Operation that may throw
        if (errorCondition)
        {{
            throw new {exceptionType}(""Operation failed"");
        }}
        return successValue;
    }};
}}

### Benefits
- Captures exceptions as values (no silent throws)
- Enables functional error handling with Match
- Lazy evaluation prevents immediate exception
- Composable with other monadic operations

### When to Apply
Use Try<T> when:
- Operation throws exceptions as normal flow control
- Exception is expected outcome (not exceptional)
- Caller should handle exception functionally
- Deferred execution is beneficial

### Additional Resources
- language-ext Try<T> docs: https://github.com/louthy/language-ext/wiki/Try";
    }

    /// <summary>
    ///     Validates that all templates include required educational components.
    /// </summary>
    /// <param name="template">The template to validate.</param>
    /// <returns>True if template is valid, false otherwise.</returns>
    public static bool ValidateTemplate(string template)
    {
        return template.Contains("### Current Code") &&
               template.Contains("### Suggested Refactoring") &&
               template.Contains("### Benefits") &&
               template.Contains("### When to Apply") &&
               template.Contains("### Additional Resources");
    }
}
