using Lintelligent.AnalyzerEngine.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lintelligent.AnalyzerEngine.Tests.TestUtilities;

/// <summary>
///     In-memory implementation of ICodeProvider for testing and demonstration.
/// </summary>
/// <remarks>
///     This provider demonstrates that AnalyzerEngine can analyze code from
///     any source, not just the file system. Useful for:
///     - Unit testing without file system dependencies
///     - IDE integration with unsaved editor buffers
///     - Analyzing generated or dynamically constructed code
///     Example usage:
///     <code>
/// var sources = new Dictionary&lt;string, string&gt;
/// {
///     ["Test.cs"] = "class Test { void Method() { } }"
/// };
/// var provider = new InMemoryCodeProvider(sources);
/// var engine = new AnalyzerEngine(manager);
/// var results = engine.Analyze(provider.GetSyntaxTrees());
/// </code>
/// </remarks>
public class InMemoryCodeProvider : ICodeProvider
{
    private readonly Dictionary<string, string> _sources;

    /// <summary>
    ///     Initializes a new instance of InMemoryCodeProvider.
    /// </summary>
    /// <param name="sources">
    ///     Dictionary mapping file paths to source code content.
    ///     Keys should be valid file paths (used for diagnostics reporting).
    ///     Values are the C# source code to parse.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if sources is null.
    /// </exception>
    public InMemoryCodeProvider(Dictionary<string, string> sources)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
    }

    /// <summary>
    ///     Yields syntax trees for all in-memory source code.
    /// </summary>
    /// <returns>
    ///     Lazy sequence of parsed syntax trees, one per dictionary entry.
    ///     File paths from dictionary keys are set on the syntax trees for
    ///     diagnostic reporting.
    /// </returns>
    public IEnumerable<SyntaxTree> GetSyntaxTrees()
    {
        foreach (var kvp in _sources)
        {
            var filePath = kvp.Key;
            var sourceCode = kvp.Value;

            var tree = CSharpSyntaxTree.ParseText(sourceCode, path: filePath);
            yield return tree;
        }
    }
}