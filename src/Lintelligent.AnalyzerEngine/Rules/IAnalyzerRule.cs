using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.AnalyzerEngine.Rules;

public interface IAnalyzerRule
{
    string Id { get; }
    string Description { get; }
    DiagnosticResult? Analyze(SyntaxTree tree);
}
