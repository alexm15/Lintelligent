using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.AnalyzerEngine.Rules;

public class LongMethodRule : IAnalyzerRule
{
    public string Id => "LNT001";
    public string Description => "Method exceeds recommended length";
    public Severity Severity => Severity.Warning;
    public string Category => DiagnosticCategories.Maintainability;

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var longMethods = root.DescendantNodes()
                             .OfType<MethodDeclarationSyntax>()
                             .Where(m => m.Body?.Statements.Count > 20);

        foreach (var method in longMethods)
        {
            var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new DiagnosticResult(
                tree.FilePath,
                Id,
                "Method is too long",
                line,
                Severity,
                Category
            );
        }
    }
}
