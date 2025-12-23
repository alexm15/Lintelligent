using Lintelligent.AnalyzerEngine.Abstractions;
using Microsoft.CodeAnalysis;

namespace Lintelligent.AnalyzerEngine.Tests.TestUtilities;

/// <summary>
/// Decorator that filters syntax trees from another provider based on a predicate.
/// </summary>
/// <remarks>
/// This provider demonstrates the composability of ICodeProvider.
/// Use cases include:
/// - Analyzing only modified files in IDE integration
/// - Filtering by file path patterns (e.g., only test files)
/// - Excluding generated code or third-party libraries
/// - Conditional analysis based on file metadata
/// 
/// Example usage:
/// <code>
/// var baseProvider = new FileSystemCodeProvider("./src");
/// var filter = new FilteringCodeProvider(
///     baseProvider,
///     tree => tree.FilePath.Contains("Controllers")
/// );
/// var results = engine.Analyze(filter.GetSyntaxTrees());
/// </code>
/// </remarks>
public class FilteringCodeProvider : ICodeProvider
{
    private readonly ICodeProvider _innerProvider;
    private readonly Func<SyntaxTree, bool> _predicate;

    /// <summary>
    /// Initializes a new instance of FilteringCodeProvider.
    /// </summary>
    /// <param name="innerProvider">
    /// The underlying provider to filter. Cannot be null.
    /// </param>
    /// <param name="predicate">
    /// Function that returns true for syntax trees to include.
    /// Trees where predicate returns false are excluded from results.
    /// Cannot be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if innerProvider or predicate is null.
    /// </exception>
    public FilteringCodeProvider(ICodeProvider innerProvider, Func<SyntaxTree, bool> predicate)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    /// <summary>
    /// Yields syntax trees from the inner provider that match the filter predicate.
    /// </summary>
    /// <returns>
    /// Lazy sequence of syntax trees where predicate returns true.
    /// Evaluation is lazy - inner provider is only queried when this
    /// sequence is enumerated.
    /// </returns>
    public IEnumerable<SyntaxTree> GetSyntaxTrees()
    {
        foreach (var tree in _innerProvider.GetSyntaxTrees())
        {
            if (_predicate(tree))
            {
                yield return tree;
            }
        }
    }
}
