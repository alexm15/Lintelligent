using System.Collections.Immutable;
using System.Globalization;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.AnalyzerEngine.Rules.Monad;

/// <summary>
///     LNT200: Detects nullable return types with multiple null checks,
///     suggesting Option&lt;T&gt; for safer null handling.
/// </summary>
/// <remarks>
///     Triggers when:
///     - Method returns nullable reference type (indicated by ? annotation)
///     - Method contains 3+ null checks or null returns
///     - Method does not already return Option&lt;T&gt;
///     
///     Educational Goal: Show developers how Option&lt;T&gt; eliminates null reference exceptions
///     and forces explicit handling of missing values.
/// </remarks>
public class NullableToOptionRule : IAnalyzerRule
{
    public string Id => "LNT200";
    public string Description => "Consider using Option<T> for nullable return type";
    public Severity Severity => Severity.Info;
    public string Category => DiagnosticCategories.Functional;

    /// <summary>
    ///     Minimum number of null-related operations to trigger the diagnostic.
    ///     Default: 3 (configurable via EditorConfig in production).
    /// </summary>
    private const int DefaultMinComplexity = 3;

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
    {
        SyntaxNode root = tree.GetRoot();

        // Find all methods with nullable return types
        var nullableMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(method => IsNullableReturnType(method));

        foreach (var method in nullableMethods)
        {
            // Skip if method already returns Option<T>
            if (AlreadyReturnsOption(method))
                continue;

            // Count null-related operations
            var nullComplexity = CountNullComplexity(method);

            // Only report if complexity exceeds threshold
            if (nullComplexity >= DefaultMinComplexity)
            {
                var methodName = method.Identifier.Text;
                var returnType = GetBaseReturnType(method);
                var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                var message = DiagnosticMessageTemplates.GetOptionTemplate(
                    methodName, 
                    returnType, 
                    nullComplexity);

                var properties = ImmutableDictionary<string, string>.Empty
                    .Add("MonadType", "Option")
                    .Add("ComplexityScore", nullComplexity.ToString(CultureInfo.InvariantCulture));

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
    }

    /// <summary>
    ///     Detects if a method has a nullable return type (indicated by ? annotation).
    /// </summary>
    private static bool IsNullableReturnType(MethodDeclarationSyntax method)
    {
        return method.ReturnType is NullableTypeSyntax;
    }

    /// <summary>
    ///     Checks if the method already returns Option&lt;T&gt; type.
    /// </summary>
    private static bool AlreadyReturnsOption(MethodDeclarationSyntax method)
    {
        var returnTypeText = method.ReturnType.ToString();
        return returnTypeText.Contains("Option<") || 
               returnTypeText.Contains("Option ");
    }

    /// <summary>
    ///     Gets the base return type without the nullable annotation.
    /// </summary>
    private static string GetBaseReturnType(MethodDeclarationSyntax method)
    {
        if (method.ReturnType is NullableTypeSyntax nullableType)
        {
            return nullableType.ElementType.ToString();
        }
        return method.ReturnType.ToString();
    }

    /// <summary>
    ///     Counts null-related complexity: null checks, null comparisons, null returns.
    /// </summary>
    private static int CountNullComplexity(MethodDeclarationSyntax method)
    {
        var complexity = 0;

        if (method.Body == null)
            return 0;

        // Count null literal usages (null checks, null returns, null assignments)
        var nullLiterals = method.Body.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(literal => literal.IsKind(SyntaxKind.NullLiteralExpression));

        complexity += nullLiterals.Count();

        // Count null-conditional operators (?. and ?[])
        var nullConditionals = method.Body.DescendantNodes()
            .OfType<ConditionalAccessExpressionSyntax>();

        complexity += nullConditionals.Count();

        // Count null-coalescing operators (??)
        var nullCoalescing = method.Body.DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Where(binary => binary.IsKind(SyntaxKind.CoalesceExpression));

        complexity += nullCoalescing.Count();

        return complexity;
    }
}
