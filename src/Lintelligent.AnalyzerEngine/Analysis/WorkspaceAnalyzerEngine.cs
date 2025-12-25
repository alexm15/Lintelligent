using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.ProjectModel;
using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;

namespace Lintelligent.AnalyzerEngine.Analysis;

/// <summary>
/// Orchestrates workspace-level analyzers across multiple syntax trees.
/// </summary>
/// <remarks>
/// Constitutional Compliance (Principle I):
/// - Core analysis engine, framework-agnostic
/// - No dependency on CLI, logging, or I/O
/// - Testable without DI infrastructure
/// 
/// Integration Pattern (RT-005):
/// - Workspace analyzers run AFTER single-file rules
/// - Sequential execution (not parallel) to maintain determinism
/// - Accepts pre-built WorkspaceContext from caller (no I/O)
/// </remarks>
public sealed class WorkspaceAnalyzerEngine
{
    private readonly List<IWorkspaceAnalyzer> _analyzers = [];
    
    /// <summary>
    /// Gets the registered workspace analyzers.
    /// </summary>
    public IReadOnlyCollection<IWorkspaceAnalyzer> Analyzers => _analyzers.AsReadOnly();
    
    /// <summary>
    /// Registers a workspace analyzer.
    /// </summary>
    /// <param name="analyzer">The analyzer to register</param>
    /// <exception cref="ArgumentNullException">If analyzer is null</exception>
    /// <exception cref="ArgumentException">If analyzer has invalid metadata</exception>
    public void RegisterAnalyzer(IWorkspaceAnalyzer analyzer)
    {
        ArgumentNullExceptionPolyfills.ThrowIfNull(analyzer, nameof(analyzer));
        
        // Validate analyzer metadata at registration time (fail-fast)
        ArgumentExceptionPolyfills.ThrowIfNullOrWhiteSpace(analyzer.Id, nameof(analyzer.Id));
        ArgumentExceptionPolyfills.ThrowIfNullOrWhiteSpace(analyzer.Category, nameof(analyzer.Category));
        ArgumentExceptionPolyfills.ThrowIfNullOrWhiteSpace(analyzer.Description, nameof(analyzer.Description));
        
        if (!EnumPolyfills.IsDefined(analyzer.Severity))
        {
            throw new ArgumentException(
                $"Analyzer '{analyzer.Id}' has undefined severity value: {analyzer.Severity}",
                nameof(analyzer));
        }
        
        _analyzers.Add(analyzer);
    }
    
    /// <summary>
    /// Registers multiple workspace analyzers.
    /// </summary>
    /// <param name="analyzers">Analyzers to register</param>
    public void RegisterAnalyzers(IEnumerable<IWorkspaceAnalyzer> analyzers)
    {
        ArgumentNullException.ThrowIfNull(analyzers);
        
        foreach (var analyzer in analyzers)
        {
            RegisterAnalyzer(analyzer);
        }
    }
    
    /// <summary>
    /// Analyzes all syntax trees using registered workspace analyzers.
    /// </summary>
    /// <param name="trees">All syntax trees in the workspace</param>
    /// <param name="context">Workspace metadata (solution, projects)</param>
    /// <returns>Enumerable of all diagnostic results from all analyzers</returns>
    /// <exception cref="ArgumentNullException">If trees or context is null</exception>
    /// <remarks>
    /// Execution:
    /// - Analyzers run sequentially (not parallel) for determinism
    /// - Each analyzer receives all trees and full context
    /// - Results are flattened into single enumerable
    /// - Exceptions from analyzers are allowed to propagate (caller handles)
    /// 
    /// Performance:
    /// - Lazy evaluation via SelectMany (yield return)
    /// - Trees are not duplicated (passed by reference)
    /// - Memory: O(n) where n = number of trees
    /// - Time: O(a * f(n)) where a = analyzer count, f(n) = analyzer complexity
    /// </remarks>
    public IEnumerable<DiagnosticResult> Analyze(
        IReadOnlyList<SyntaxTree> trees,
        WorkspaceContext context)
    {
        ArgumentNullException.ThrowIfNull(trees);
        ArgumentNullException.ThrowIfNull(context);
        
        // Sequential execution: run each analyzer against all trees
        // Lazy evaluation: results streamed as they are produced
        return _analyzers.SelectMany(analyzer => analyzer.Analyze(trees, context));
    }
}
