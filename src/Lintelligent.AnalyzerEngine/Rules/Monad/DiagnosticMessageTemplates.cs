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
        return $"Consider using Option<{returnType}> instead of nullable return. Found {nullCheckCount} null operations. Replace 'return null' with 'Option<{returnType}>.None' and 'return value' with 'Option<{returnType}>.Some(value)' to eliminate null reference exceptions.";
    }

    /// <summary>
    ///     Template for LNT201: Try/Catch to Either&lt;L, R&gt; diagnostic.
    ///     Explains benefits of Either&lt;L, R&gt; for explicit error handling.
    /// </summary>
    public static string GetEitherTemplate(string methodName, string exceptionType)
    {
        return $"Consider using Either<Error, T> for explicit error handling. Try/catch with returns in both branches can be refactored to railway-oriented programming. Replace 'return success' with 'Right(success)' and 'catch' with 'Left(error)' to make failures explicit in the method signature.";
    }

    /// <summary>
    ///     Template for LNT202: Sequential Validation to Validation&lt;T&gt; diagnostic.
    ///     Explains benefits of Validation&lt;T&gt; for accumulating errors.
    /// </summary>
    public static string GetValidationTemplate(string methodName, int validationCount)
    {
        return $"Consider using Validation<Error, T> to accumulate errors. Found {validationCount} sequential validations with early returns. Use applicative validation to collect ALL errors instead of stopping at the first failure, providing better user feedback.";
    }

    /// <summary>
    ///     Template for LNT203: Exception-based Flow to Try&lt;T&gt; diagnostic.
    ///     Explains benefits of Try&lt;T&gt; for exception-prone operations.
    /// </summary>
    public static string GetTryTemplate(string methodName, string exceptionType)
    {
        return $"Consider using Try<T> for exception wrapping. Method throws {exceptionType} which can be captured as a value instead. Wrap operation in Try(() => {{ ... }}) to enable functional error handling with Match, Bind, and other monadic operations.";
    }
}
