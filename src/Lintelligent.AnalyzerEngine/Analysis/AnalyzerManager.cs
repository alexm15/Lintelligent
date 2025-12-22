using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;

namespace Lintelligent.AnalyzerEngine.Analysis;

public sealed class AnalyzerManager
{
    private readonly List<IAnalyzerRule> _rules = [];

    public IReadOnlyCollection<IAnalyzerRule> Rules => _rules.AsReadOnly();

    public void RegisterRule(IAnalyzerRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        _rules.Add(rule);
    }

    public void RegisterRules(IEnumerable<IAnalyzerRule> rules)
    {
        foreach (var rule in rules)
        {
            RegisterRule(rule);
        }
    }

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree syntaxTree)
    {
        return _rules.Select(rule => rule.Analyze(syntaxTree)).OfType<DiagnosticResult>();
    }
}