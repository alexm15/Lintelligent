using Microsoft.CodeAnalysis;
using Lintelligent.AnalyzerEngine.Abstractions;

namespace Lintelligent.Analyzers.Metadata;

/// <summary>
///     Mapper for converting Lintelligent Severity enum to Roslyn DiagnosticSeverity.
///     Provides bidirectional mapping for EditorConfig integration.
/// </summary>
/// <remarks>
///     Mapping Table:
///     
///     Lintelligent.Severity → Roslyn.DiagnosticSeverity
///     ------------------------------------------------
///     Severity.Error        → DiagnosticSeverity.Error
///     Severity.Warning      → DiagnosticSeverity.Warning
///     Severity.Info         → DiagnosticSeverity.Info
///     
///     EditorConfig String → Roslyn.DiagnosticSeverity
///     ------------------------------------------------
///     "none"                → (suppress diagnostic)
///     "suggestion"          → DiagnosticSeverity.Info
///     "warning"             → DiagnosticSeverity.Warning
///     "error"               → DiagnosticSeverity.Error
///     
///     Build Behavior:
///     - Error → Build fails (exit code 1)
///     - Warning → Build succeeds with warnings
///     - Info/Suggestion → Build succeeds, IDE shows hint
///     - None → No diagnostic reported
///     
///     Thread Safety: Static methods only, no state (thread-safe)
/// </remarks>
public interface ISeverityMapper
{
    /// <summary>
    ///     Convert Lintelligent Severity to Roslyn DiagnosticSeverity.
    /// </summary>
    /// <param name="severity">Lintelligent severity from IAnalyzerRule</param>
    /// <returns>Equivalent Roslyn diagnostic severity</returns>
    /// <remarks>
    ///     Direct mapping:
    ///     - Severity.Error → DiagnosticSeverity.Error
    ///     - Severity.Warning → DiagnosticSeverity.Warning
    ///     - Severity.Info → DiagnosticSeverity.Info
    ///     
    ///     Throws ArgumentException if severity is undefined enum value
    /// </remarks>
    static abstract DiagnosticSeverity ToRoslynSeverity(Severity severity);

    /// <summary>
    ///     Convert EditorConfig severity string to Roslyn DiagnosticSeverity.
    /// </summary>
    /// <param name="editorConfigSeverity">Severity string from .editorconfig (e.g., "warning")</param>
    /// <returns>Equivalent Roslyn diagnostic severity</returns>
    /// <remarks>
    ///     Mapping:
    ///     - "suggestion" → DiagnosticSeverity.Info (IDE hint, no build impact)
    ///     - "warning" → DiagnosticSeverity.Warning (build warning)
    ///     - "error" → DiagnosticSeverity.Error (build failure)
    ///     - "none" → throw InvalidOperationException (caller should check and suppress)
    ///     - null/empty/unknown → throw ArgumentException
    ///     
    ///     Case-Insensitive: Accepts "Warning", "WARNING", "warning" (normalized to lowercase)
    ///     
    ///     Note: "none" should be handled by caller BEFORE conversion (suppresses diagnostic)
    /// </remarks>
    static abstract DiagnosticSeverity FromEditorConfigSeverity(string editorConfigSeverity);

    /// <summary>
    ///     Check if EditorConfig severity suppresses diagnostic.
    /// </summary>
    /// <param name="editorConfigSeverity">Severity string from .editorconfig</param>
    /// <returns>True if diagnostic should be suppressed (severity is "none")</returns>
    /// <remarks>
    ///     Usage:
    ///     if (SeverityMapper.IsSuppressed(editorConfigSeverity))
    ///     {
    ///         continue; // Skip rule execution
    ///     }
    ///     
    ///     Case-Insensitive: "none", "None", "NONE" all return true
    /// </remarks>
    static abstract bool IsSuppressed(string editorConfigSeverity);
}
