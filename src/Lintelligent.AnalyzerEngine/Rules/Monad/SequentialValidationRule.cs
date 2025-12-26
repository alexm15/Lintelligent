using System.Collections.Immutable;
using System.Globalization;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.AnalyzerEngine.Rules.Monad;

/// <summary>
///     LNT202: Detects sequential validation checks that fail fast,
///     suggesting Validation&lt;T&gt; to accumulate all errors.
/// </summary>
/// <remarks>
///     Triggers when:
///     - Method contains 2+ sequential if statements with immediate returns
///     - Returns appear to be error/validation results (not success values)
///     - Method does not already return Validation&lt;T&gt;
///     
///     Educational Goal: Show developers how Validation&lt;T&gt; improves UX
///     by collecting all validation errors at once instead of failing fast.
/// </remarks>
public class SequentialValidationRule : IAnalyzerRule
{
    public string Id => "LNT202";
    public string Description => "Consider using Validation<T> to accumulate validation errors";
    public Severity Severity => Severity.Info;
    public string Category => DiagnosticCategories.Functional;

    /// <summary>
    ///     Minimum number of sequential validations to trigger diagnostic.
    ///     Default: 2 (configurable via EditorConfig in production).
    /// </summary>
    private const int DefaultMinValidations = 2;

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
    {
        SyntaxNode root = tree.GetRoot();

        // Find all methods
        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            if (method.Body == null)
                continue;

            // Skip if method already returns Validation<T>
            if (AlreadyReturnsValidation(method))
                continue;

            // Find if statements with immediate return statements
            var validationChecks = method.Body
                .DescendantNodes()
                .OfType<IfStatementSyntax>()
                .Where(HasImmediateReturn)
                .ToList();

            if (validationChecks.Count < DefaultMinValidations)
                continue;

            var methodName = method.Identifier.Text;
            var firstCheck = validationChecks[0];
            var line = firstCheck.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            var message = DiagnosticMessageTemplates.GetValidationTemplate(
                methodName, 
                validationChecks.Count);

            var properties = ImmutableDictionary<string, string>.Empty
                .Add("MonadType", "Validation")
                .Add("ComplexityScore", validationChecks.Count.ToString(CultureInfo.InvariantCulture));

            yield return new DiagnosticResult(
                tree.FilePath,
                Id,
                message,
                line,
                Severity,
                Category,
                properties
            );
        }
    }

    /// <summary>
    ///     Checks if an if statement has an immediate return in its body.
    /// </summary>
    private static bool HasImmediateReturn(IfStatementSyntax ifStatement)
    {
        // Check if the if statement's direct body is a return statement
        if (ifStatement.Statement is ReturnStatementSyntax)
            return true;

        // Check if it's a block with a single return statement
        if (ifStatement.Statement is BlockSyntax block)
        {
            return block.Statements.Count == 1 && 
                   block.Statements[0] is ReturnStatementSyntax;
        }

        return false;
    }

    /// <summary>
    ///     Checks if the method already returns Validation&lt;T&gt; type.
    /// </summary>
    private static bool AlreadyReturnsValidation(MethodDeclarationSyntax method)
    {
        var returnTypeText = method.ReturnType.ToString();
        return returnTypeText.Contains("Validation<", StringComparison.Ordinal) || 
               returnTypeText.Contains("Validation ", StringComparison.Ordinal);
    }
}
