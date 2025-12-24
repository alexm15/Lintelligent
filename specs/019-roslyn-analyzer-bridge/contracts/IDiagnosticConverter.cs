using Microsoft.CodeAnalysis;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Abstractions;

namespace Lintelligent.Analyzers.Adapters;

/// <summary>
///     Converter for transforming DiagnosticResult to Roslyn Diagnostic.
///     Handles line number indexing conversion (1-indexed → 0-indexed) and location mapping.
/// </summary>
/// <remarks>
///     Conversion Strategy:
///     1. Convert line number: DiagnosticResult.LineNumber (1-indexed) → Roslyn LinePosition (0-indexed)
///     2. Get TextLine from SyntaxTree at converted line number
///     3. Create TextSpan for entire line (column 0 to line length)
///     4. Create Location from SyntaxTree and TextSpan
///     5. Map Severity enum → DiagnosticSeverity via SeverityMapper
///     6. Find matching DiagnosticDescriptor by rule ID
///     7. Create Diagnostic with descriptor, location, and message
///     
///     Error Handling:
///     - Line number out of range → clamp to file bounds
///     - Empty file → skip diagnostic (no valid location)
///     - Unknown rule ID → log warning, skip diagnostic
///     
///     Thread Safety: Static methods only, no instance state (thread-safe)
/// </remarks>
public interface IDiagnosticConverter
{
    /// <summary>
    ///     Convert DiagnosticResult to Roslyn Diagnostic.
    /// </summary>
    /// <param name="result">Analysis result from IAnalyzerRule</param>
    /// <param name="tree">Syntax tree being analyzed (for location creation)</param>
    /// <param name="descriptor">Diagnostic descriptor for this rule</param>
    /// <returns>Roslyn diagnostic ready for reporting</returns>
    /// <remarks>
    ///     Preconditions:
    ///     - result must be non-null
    ///     - tree must be non-null
    ///     - descriptor must be non-null
    ///     - descriptor.Id must match result.RuleId
    ///     
    ///     Line Number Conversion:
    ///     - DiagnosticResult: 1-indexed (user-facing, line 1 = first line)
    ///     - Roslyn LinePosition: 0-indexed (internal, line 0 = first line)
    ///     - Conversion: roslynLine = result.LineNumber - 1
    ///     
    ///     Location Strategy:
    ///     - Use entire line as location (TextLine.Span)
    ///     - Future enhancement: Column-specific locations if DiagnosticResult extended
    /// </remarks>
    static abstract Diagnostic Convert(DiagnosticResult result, SyntaxTree tree, DiagnosticDescriptor descriptor);

    /// <summary>
    ///     Create Roslyn Location from line number and syntax tree.
    /// </summary>
    /// <param name="lineNumber">1-indexed line number from DiagnosticResult</param>
    /// <param name="tree">Syntax tree containing the line</param>
    /// <returns>Roslyn Location pointing to specified line</returns>
    /// <remarks>
    ///     Steps:
    ///     1. Convert to 0-indexed: roslynLine = lineNumber - 1
    ///     2. Clamp to valid range: [0, tree.GetText().Lines.Count - 1]
    ///     3. Get TextLine: tree.GetText().Lines[roslynLine]
    ///     4. Create Location: Location.Create(tree, textLine.Span)
    ///     
    ///     Edge Cases:
    ///     - lineNumber < 1 → clamp to line 0
    ///     - lineNumber > file length → clamp to last line
    ///     - Empty file (0 lines) → return Location.None
    /// </remarks>
    static abstract Location CreateLocation(int lineNumber, SyntaxTree tree);

    /// <summary>
    ///     Get line span for diagnostic location (for future column support).
    /// </summary>
    /// <param name="lineNumber">1-indexed line number</param>
    /// <param name="tree">Syntax tree</param>
    /// <returns>LinePositionSpan covering entire line</returns>
    /// <remarks>
    ///     Current Implementation: Spans entire line (column 0 to end)
    ///     Future Enhancement: Accept column parameter for precise location
    ///     
    ///     Example:
    ///     - Line 10, no column → LinePositionSpan(9, 0, 9, lineLength)
    ///     - Line 10, column 15 → LinePositionSpan(9, 15, 9, 15 + tokenLength)
    /// </remarks>
    static abstract LinePositionSpan GetLineSpan(int lineNumber, SyntaxTree tree);
}
