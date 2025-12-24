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

        // Validate rule metadata at registration time (fail-fast)
        ArgumentException.ThrowIfNullOrWhiteSpace(rule.Id, nameof(rule.Id));
        ArgumentException.ThrowIfNullOrWhiteSpace(rule.Category, nameof(rule.Category));

        if (!Enum.IsDefined(rule.Severity))
            throw new ArgumentException(
                $"Rule '{rule.Id}' has undefined severity value: {rule.Severity}",
                nameof(rule.Severity));

        _rules.Add(rule);
    }

    public void RegisterRules(IEnumerable<IAnalyzerRule> rules)
    {
        foreach (var rule in rules) RegisterRule(rule);
    }

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree syntaxTree)
    {
        return _rules.SelectMany(rule => rule.Analyze(syntaxTree));
    }
}