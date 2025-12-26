using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.Analyzers.Metadata;
using Microsoft.CodeAnalysis;

namespace Lintelligent.Analyzers.Adapters;

/// <summary>
///     Factory for creating Roslyn DiagnosticDescriptor from IAnalyzerRule.
/// </summary>
public static class RuleDescriptorFactory
{
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
