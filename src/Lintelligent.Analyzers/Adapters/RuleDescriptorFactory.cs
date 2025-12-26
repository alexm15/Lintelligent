using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.Analyzers.Metadata;
using Microsoft.CodeAnalysis;

namespace Lintelligent.Analyzers.Adapters;

/// <summary>
///     Factory for creating Roslyn DiagnosticDescriptor from IAnalyzerRule.
/// </summary>
public static class RuleDescriptorFactory
{
#pragma warning disable S1075 // URIs should not be hardcoded - These are documentation links
    private const string BaseHelpUrl =
        "https://github.com/YourOrg/Lintelligent/blob/main/specs/005-core-rule-library/rules-documentation.md";

    private static readonly Dictionary<string, string> RuleAnchors = new(StringComparer.Ordinal)
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
    
    private static readonly Dictionary<string, string> MonadRuleUrls = new(StringComparer.Ordinal)
    {
        ["LNT200"] = "https://github.com/louthy/language-ext/wiki/How-to-handle-errors-in-a-functional-way#optional-results",
        ["LNT201"] = "https://github.com/louthy/language-ext/wiki/How-to-handle-errors-in-a-functional-way#eitherl-r",
        ["LNT202"] = "https://github.com/louthy/language-ext/wiki/How-to-handle-errors-in-a-functional-way#generalising-to-other-alternative-value-types",
        ["LNT203"] = "https://github.com/louthy/language-ext/wiki/How-to-handle-errors-in-a-functional-way#generalising-to-other-alternative-value-types"
    };
#pragma warning restore S1075

    /// <summary>
    ///     Creates a DiagnosticDescriptor from an IAnalyzerRule.
    /// </summary>
    public static DiagnosticDescriptor Create(IAnalyzerRule rule)
    {
#if NETSTANDARD2_0
        ArgumentNullExceptionPolyfills.ThrowIfNull(rule, nameof(rule));
#else
        ArgumentNullException.ThrowIfNull(rule);
#endif

        return new DiagnosticDescriptor(
            rule.Id,
            rule.Description,
            "{0}", // Filled with DiagnosticResult.Message
            rule.Category,
            SeverityMapper.ToRoslynSeverity(rule.Severity),
            true,
            rule.Description,
            GetHelpLinkUri(rule.Id),
            GetCustomTags(rule.Category));
    }

    /// <summary>
    ///     Gets the help link URI for a rule ID.
    /// </summary>
    public static string GetHelpLinkUri(string ruleId)
    {
        // Check if it's a monad detection rule - link directly to LanguageExt docs
        if (MonadRuleUrls.TryGetValue(ruleId, out var monadUrl))
            return monadUrl;
        
        // Check if it's a standard rule with an anchor
        return RuleAnchors.TryGetValue(ruleId, out var anchor)
            ? $"{BaseHelpUrl}#{anchor}"
            : BaseHelpUrl;
    }

    /// <summary>
    ///     Gets custom tags based on rule category.
    /// </summary>
    public static string[] GetCustomTags(string category)
    {
        var tags = new List<string> {"CodeQuality"};

        if (string.Equals(category, "Maintainability", StringComparison.Ordinal)) tags.Add("Maintainability");
        else if (string.Equals(category, "CodeSmell", StringComparison.Ordinal)) tags.Add("CodeSmell");
        else if (string.Equals(category, "Documentation", StringComparison.Ordinal)) tags.Add("Documentation");

        return tags.ToArray();
    }
}
