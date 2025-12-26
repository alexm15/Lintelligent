using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Lintelligent.Analyzers.Adapters;

/// <summary>
///     Converts Lintelligent DiagnosticResult to Roslyn Diagnostic.
/// </summary>
public static class DiagnosticConverter
{
    /// <summary>
    ///     Converts a DiagnosticResult to a Roslyn Diagnostic.
    /// </summary>
    /// <param name="result">The diagnostic result from IAnalyzerRule.</param>
    /// <param name="tree">The syntax tree being analyzed.</param>
    /// <param name="descriptor">The diagnostic descriptor for the rule.</param>
    /// <returns>A Roslyn Diagnostic that can be reported via context.ReportDiagnostic().</returns>
    public static Diagnostic Convert(DiagnosticResult result, SyntaxTree tree, DiagnosticDescriptor descriptor)
    {
#if NETSTANDARD2_0
        ArgumentNullExceptionPolyfills.ThrowIfNull(result, nameof(result));
        ArgumentNullExceptionPolyfills.ThrowIfNull(tree, nameof(tree));
        ArgumentNullExceptionPolyfills.ThrowIfNull(descriptor, nameof(descriptor));
#else
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(descriptor);
#endif

        Location location = CreateLocation(result.LineNumber, tree);
        
        // Convert properties to nullable string values for Roslyn API
        var roslynProperties = result.Properties.IsEmpty 
            ? null 
            : result.Properties.ToImmutableDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value);
        
        // Pass custom properties to Roslyn diagnostic for code fix provider support
        return Diagnostic.Create(
            descriptor, 
            location, 
            properties: roslynProperties, 
            messageArgs: result.Message);
    }

    /// <summary>
    ///     Creates a Roslyn Location from a 1-indexed line number.
    /// </summary>
    /// <remarks>
    ///     DiagnosticResult uses 1-indexed line numbers (user-friendly).
    ///     Roslyn Location uses 0-indexed line numbers (API convention).
    ///     This method handles the conversion and bounds checking.
    /// </remarks>
    public static Location CreateLocation(int lineNumber, SyntaxTree tree)
    {
        SourceText text = tree.GetText();
        if (text.Lines.Count == 0) return Location.None; // Empty file edge case

        // Convert 1-indexed to 0-indexed, clamp to valid range
#if NETSTANDARD2_0
        var roslynLine = MathPolyfills.Clamp(lineNumber - 1, 0, text.Lines.Count - 1);
#else
        var roslynLine = Math.Clamp(lineNumber - 1, 0, text.Lines.Count - 1);
#endif
        TextLine textLine = text.Lines[roslynLine];

        return Location.Create(tree, textLine.Span);
    }

    /// <summary>
    ///     Gets the line span for a location (for testing).
    /// </summary>
    public static FileLinePositionSpan GetLineSpan(Location location)
    {
        return location.GetLineSpan();
    }
}
