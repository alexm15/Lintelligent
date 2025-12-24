using Microsoft.CodeAnalysis;
using Lintelligent.AnalyzerEngine.Rules;

namespace Lintelligent.Analyzers.Adapters;

/// <summary>
///     Factory for creating Roslyn DiagnosticDescriptor from IAnalyzerRule.
///     Provides metadata mapping: rule properties → diagnostic descriptor properties.
/// </summary>
/// <remarks>
///     Mapping Strategy:
///     - Id: rule.Id (e.g., "LNT001")
///     - Title: rule.Description
///     - MessageFormat: "{0}" (dynamic message from DiagnosticResult)
///     - Category: rule.Category (e.g., "Maintainability")
///     - DefaultSeverity: Mapped from rule.Severity via SeverityMapper
///     - HelpLinkUri: GitHub URL with rule-specific anchor
///     - CustomTags: ["CodeQuality"] + category-specific tag
///     
///     Help Link Format:
///     https://github.com/[org]/Lintelligent/blob/main/specs/005-core-rule-library/rules-documentation.md#lnt001-long-method
///     
///     Thread Safety: Static methods only, no instance state (thread-safe)
/// </remarks>
public interface IRuleDescriptorFactory
{
    /// <summary>
    ///     Create DiagnosticDescriptor from IAnalyzerRule.
    /// </summary>
    /// <param name="rule">Analyzer rule to create descriptor for</param>
    /// <returns>Immutable diagnostic descriptor with complete metadata</returns>
    /// <remarks>
    ///     Validation:
    ///     - rule.Id must be non-null, non-empty
    ///     - rule.Description must be non-null, non-empty
    ///     - rule.Category must be non-null, non-empty
    ///     - rule.Severity must be defined enum value
    ///     
    ///     Throws ArgumentException if validation fails
    /// </remarks>
    static abstract DiagnosticDescriptor Create(IAnalyzerRule rule);

    /// <summary>
    ///     Get help link URL for specific rule.
    /// </summary>
    /// <param name="ruleId">Rule identifier (e.g., "LNT001")</param>
    /// <returns>Absolute URL to rule documentation with anchor</returns>
    /// <remarks>
    ///     Format: https://github.com/[org]/Lintelligent/blob/main/specs/005-core-rule-library/rules-documentation.md#{ruleId-lowercase}
    ///     Example: LNT001 → #lnt001-long-method
    ///     
    ///     Rule ID Anchor Mapping:
    ///     - LNT001 → #lnt001-long-method
    ///     - LNT002 → #lnt002-long-parameter-list
    ///     - LNT003 → #lnt003-complex-conditional
    ///     - LNT004 → #lnt004-magic-number
    ///     - LNT005 → #lnt005-god-class
    ///     - LNT006 → #lnt006-dead-code
    ///     - LNT007 → #lnt007-exception-swallowing
    ///     - LNT008 → #lnt008-missing-xml-documentation
    /// </remarks>
    static abstract string GetHelpLinkUri(string ruleId);

    /// <summary>
    ///     Get custom tags for rule based on category.
    /// </summary>
    /// <param name="category">Rule category (e.g., "Maintainability")</param>
    /// <returns>Array of tags for diagnostic descriptor</returns>
    /// <remarks>
    ///     Base tag: "CodeQuality" (all rules)
    ///     Category-specific tags:
    ///     - Maintainability → "Maintainability"
    ///     - CodeSmell → "CodeSmell"
    ///     - Documentation → "Documentation"
    ///     - Default → none (just "CodeQuality")
    /// </remarks>
    static abstract string[] GetCustomTags(string category);
}
