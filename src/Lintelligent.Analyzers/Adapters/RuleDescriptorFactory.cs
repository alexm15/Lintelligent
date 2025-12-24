using Microsoft.CodeAnalysis;
using Lintelligent.AnalyzerEngine.Rules;

namespace Lintelligent.Analyzers.Adapters;

/// <summary>
/// Factory for creating Roslyn DiagnosticDescriptor from IAnalyzerRule.
/// </summary>
public static class RuleDescriptorFactory
{
    private const string BaseHelpUrl = "https://github.com/YourOrg/Lintelligent/blob/main/specs/005-core-rule-library/rules-documentation.md";

    private static readonly Dictionary<string, string> RuleAnchors = new()
    {
        ["LNT001"] = "lnt001-long-method",
        ["LNT002"] = "lnt002-long-parameter-list",
        ["LNT003"] = "lnt003-complex-conditional",
        ["LNT004"] = "lnt004-magic-number",
        ["LNT005"] = "lnt005-god-class",
        ["LNT006"] = "lnt006-dead-code",
        ["LNT007"] = "lnt007-exception-swallowing",
        ["LNT008"] = "lnt008-missing-xml-documentation"
    };

    /// <summary>
    /// Creates a DiagnosticDescriptor from an IAnalyzerRule.
    /// </summary>
    public static DiagnosticDescriptor Create(IAnalyzerRule rule)
    {
#if NETSTANDARD2_0
        ArgumentNullExceptionPolyfills.ThrowIfNull(rule, nameof(rule));
#else
        ArgumentNullException.ThrowIfNull(rule);
#endif

        return new DiagnosticDescriptor(
            id: rule.Id,
            title: rule.Description,
            messageFormat: "{0}",  // Filled with DiagnosticResult.Message
            category: rule.Category,
            defaultSeverity: Metadata.SeverityMapper.ToRoslynSeverity(rule.Severity),
            isEnabledByDefault: true,
            description: rule.Description,
            helpLinkUri: GetHelpLinkUri(rule.Id),
            customTags: GetCustomTags(rule.Category));
    }

    /// <summary>
    /// Gets the help link URI for a rule ID.
    /// </summary>
    public static string GetHelpLinkUri(string ruleId)
    {
        return RuleAnchors.TryGetValue(ruleId, out var anchor)
            ? $"{BaseHelpUrl}#{anchor}"
            : BaseHelpUrl;
    }

    /// <summary>
    /// Gets custom tags based on rule category.
    /// </summary>
    public static string[] GetCustomTags(string category)
    {
        var tags = new List<string> { "CodeQuality" };

        if (category == "Maintainability") tags.Add("Maintainability");
        else if (category == "CodeSmell") tags.Add("CodeSmell");
        else if (category == "Documentation") tags.Add("Documentation");

        return tags.ToArray();
    }
}
