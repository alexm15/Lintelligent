using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.AnalyzerEngine.Rules;

public class LongMethodRule : IAnalyzerRule
{
    public string Id => "LNT001";
    public string Description => "Method exceeds recommended length";

    public DiagnosticResult? Analyze(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var longMethod = root.DescendantNodes()
                             .OfType<MethodDeclarationSyntax>()
                             .FirstOrDefault(m => m.Body?.Statements.Count > 20);

        if (longMethod == null) return null;
        var line = longMethod.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        return new DiagnosticResult(tree.FilePath, Id, "Method is too long", line);

    }
}
