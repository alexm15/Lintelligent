using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.AnalyzerEngine.Rules;

public class LongMethodRule : IAnalyzerRule
{
    public string Id => "LNT001";
    public string Description => "Method exceeds recommended length";
    public Severity Severity => Severity.Warning;
    public string Category => DiagnosticCategories.CodeSmell;

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var longMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Body?.Statements.Count > 20);

        foreach (var method in longMethods)
        {
            var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var methodName = method.Identifier.Text;
            var statementCount = method.Body!.Statements.Count;
            var message = $"Method '{methodName}' has {statementCount} statements (max: 20). Consider extracting logical blocks into separate methods.";
            
            yield return new DiagnosticResult(
                tree.FilePath,
                Id,
                message,
                line,
                Severity,
                Category
            );
        }
    }
}