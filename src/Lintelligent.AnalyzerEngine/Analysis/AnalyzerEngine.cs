using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;

namespace Lintelligent.AnalyzerEngine.Analysis;

/// <summary>
/// Core analysis orchestrator that processes syntax trees through registered analyzer rules.
/// </summary>
/// <remarks>
/// This engine is framework-agnostic and performs no file system IO operations.
/// Syntax trees are provided by implementations of ICodeProvider (in the CLI layer).
/// 
/// Design Principles:
/// - Stateless: No mutable state, same trees always yield identical results
/// - Streaming: Uses yield to process large codebases without memory exhaustion
/// - Deterministic: Analysis results depend only on input syntax trees, not environment
/// </remarks>
public class AnalyzerEngine(AnalyzerManager manager)
{
    /// <summary>
    /// Analyzes a collection of syntax trees and yields diagnostic results.
    /// </summary>
    /// <param name="syntaxTrees">
    /// Enumerable collection of parsed syntax trees to analyze.
    /// Trees should have FilePath set for accurate diagnostic reporting.
    /// </param>
    /// <returns>
    /// Lazy sequence of diagnostic results for each rule violation found.
    /// Empty if no violations detected or input is empty.
    /// </returns>
    /// <remarks>
    /// This method processes trees in a streaming fashion using yield return.
    /// Results are produced as trees are analyzed, enabling memory-efficient processing
    /// of large codebases without loading all diagnostics into memory.
    /// 
    /// Implementation is deterministic: same syntax trees will always produce
    /// identical diagnostic results regardless of execution environment.
    /// </remarks>
    public IEnumerable<DiagnosticResult> Analyze(IEnumerable<SyntaxTree> syntaxTrees)
    {
        foreach (var tree in syntaxTrees)
        {
            foreach (var diagnostic in manager.Analyze(tree))
            {
                yield return diagnostic;
            }
        }
    }
}
