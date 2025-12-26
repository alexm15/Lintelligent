using System.Collections.Immutable;
using System.Globalization;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.AnalyzerEngine.Rules.Monad;

/// <summary>
///     LNT201: Detects try/catch blocks used for control flow,
///     suggesting Either&lt;L, R&gt; for railway-oriented programming.
/// </summary>
/// <remarks>
///     Triggers when:
///     - Try block contains return statement
///     - Catch block contains return statement (not just rethrow/log)
///     - Method does not already return Either&lt;L, R&gt;
///     
///     Educational Goal: Show developers how Either&lt;L, R&gt; makes failures explicit
///     in method signatures and enables railway-oriented programming patterns.
/// </remarks>
public class TryCatchToEitherRule : IAnalyzerRule
{
    public string Id => "LNT201";
    public string Description => "Consider using Either<L, R> for error handling instead of try/catch";
    public Severity Severity => Severity.Info;
    public string Category => DiagnosticCategories.Functional;

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
    {
        SyntaxNode root = tree.GetRoot();

        // Find all try/catch statements
        var tryStatements = root.DescendantNodes()
            .OfType<TryStatementSyntax>();

        foreach (var tryStatement in tryStatements)
        {
            var diagnostic = AnalyzeTryStatement(tryStatement, tree.FilePath);
            if (diagnostic != null)
                yield return diagnostic;
        }
    }

    /// <summary>
    ///     Analyzes a single try/catch statement for Either&lt;L, R&gt; opportunity.
    /// </summary>
    private DiagnosticResult? AnalyzeTryStatement(TryStatementSyntax tryStatement, string filePath)
    {
        // Skip if no catch clauses
        if (tryStatement.Catches.Count == 0)
            return null;

        // Check if try block has return statement
        var tryHasReturn = tryStatement.Block
            .DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .Any();

        if (!tryHasReturn)
            return null;

        // Check if any catch block has return statement (control flow usage)
        var catchWithReturn = tryStatement.Catches
            .Where(c => c.Block != null)
            .Any(c => c.Block!.DescendantNodes()
                .OfType<ReturnStatementSyntax>()
                .Any());

        if (!catchWithReturn)
            return null;

        // Find the containing method to check return type
        var containingMethod = tryStatement.Ancestors()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (containingMethod == null)
            return null;

        // Skip if method already returns Either<L, R>
        if (AlreadyReturnsEither(containingMethod))
            return null;

        var methodName = containingMethod.Identifier.Text;
        var line = tryStatement.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        
        // Get exception type from first catch clause
        var exceptionType = tryStatement.Catches[0].Declaration?.Type?.ToString() ?? "Exception";

        var message = DiagnosticMessageTemplates.GetEitherTemplate(methodName, exceptionType);

        var properties = ImmutableDictionary<string, string>.Empty
            .Add("MonadType", "Either")
            .Add("PatternName", "try-catch-control-flow");

        return new DiagnosticResult(
            filePath,
            Id,
            message,
            line,
            Severity,
            Category,
            properties
        );
    }

    /// <summary>
    ///     Checks if the method already returns Either&lt;L, R&gt; type.
    /// </summary>
    private static bool AlreadyReturnsEither(MethodDeclarationSyntax method)
    {
        var returnTypeText = method.ReturnType.ToString();
        return returnTypeText.Contains("Either<", StringComparison.Ordinal) || 
               returnTypeText.Contains("Either ", StringComparison.Ordinal);
    }
}
