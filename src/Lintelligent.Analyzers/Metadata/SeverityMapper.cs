using Microsoft.CodeAnalysis;
using Lintelligent.AnalyzerEngine.Abstractions;

namespace Lintelligent.Analyzers.Metadata;

/// <summary>
/// Converts Lintelligent Severity to Roslyn DiagnosticSeverity and handles EditorConfig severity parsing.
/// </summary>
public static class SeverityMapper
{
    /// <summary>
    /// Converts Lintelligent Severity enum to Roslyn DiagnosticSeverity.
    /// </summary>
    public static DiagnosticSeverity ToRoslynSeverity(Severity severity)
    {
        return severity switch
        {
            Severity.Error => DiagnosticSeverity.Error,
            Severity.Warning => DiagnosticSeverity.Warning,
            Severity.Info => DiagnosticSeverity.Info,
            _ => throw new ArgumentException($"Undefined severity: {severity}", nameof(severity))
        };
    }

    /// <summary>
    /// Converts EditorConfig severity string to Roslyn DiagnosticSeverity.
    /// </summary>
    /// <remarks>
    /// EditorConfig severity values: none, suggestion, warning, error
    /// Roslyn DiagnosticSeverity: Hidden, Info, Warning, Error
    /// </remarks>
    public static DiagnosticSeverity FromEditorConfigSeverity(string editorConfigSeverity)
    {
        return editorConfigSeverity.ToLowerInvariant() switch
        {
            "suggestion" => DiagnosticSeverity.Info,
            "warning" => DiagnosticSeverity.Warning,
            "error" => DiagnosticSeverity.Error,
            "none" => throw new InvalidOperationException("Severity 'none' should be handled by caller via IsSuppressed()"),
            _ => throw new ArgumentException($"Invalid EditorConfig severity: {editorConfigSeverity}", nameof(editorConfigSeverity))
        };
    }

    /// <summary>
    /// Checks if EditorConfig severity is 'none' (suppressed).
    /// </summary>
    public static bool IsSuppressed(string? editorConfigSeverity)
    {
        return string.Equals(editorConfigSeverity, "none", StringComparison.OrdinalIgnoreCase);
    }
}
