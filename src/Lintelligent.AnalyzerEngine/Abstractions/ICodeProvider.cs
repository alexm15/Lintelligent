namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
///     Abstraction for discovering and providing source code for analysis.
///     Implementations may read from file system, memory, IDE buffers, network, or any other source.
/// </summary>
/// <remarks>
///     Contract Requirements:
///     - MUST yield only valid, non-null SyntaxTree objects
///     (Invalid = null reference, tree with parse errors requiring caller action, or tree without FilePath set)
///     - MUST set meaningful FilePath on each SyntaxTree for diagnostic reporting
///     - MUST handle errors internally (log and skip problematic sources rather than throwing exceptions)
///     - MAY filter or transform source code before parsing
///     - SHOULD use lazy evaluation (yield) to support large codebases without memory exhaustion
///     Design Principles:
///     - Implementations should be stateless and instantiable without dependency injection
///     - File path can be any descriptive string - doesn't require actual file to exist
///     - Error handling is implementation responsibility - analyzer engine assumes all yielded trees are valid
///     Example Implementations:
///     - FileSystemCodeProvider: Discovers .cs files from disk
///     - InMemoryCodeProvider: Yields trees from in-memory string dictionary (testing)
///     - IdeBufferCodeProvider: Reads from IDE's in-memory document buffers (IDE plugins)
///     - GitDiffCodeProvider: Discovers only changed files in git diff (CI/CD optimization)
/// </remarks>
public interface ICodeProvider
{
    /// <summary>
    ///     Discovers and provides syntax trees for analysis.
    /// </summary>
    /// <returns>
    ///     Lazy sequence of parsed syntax trees.
    ///     MUST yield only valid, non-null SyntaxTree objects.
    ///     Empty sequence if no source code found (not an error condition).
    /// </returns>
    /// <exception cref="Exception">
    ///     Implementations SHOULD NOT throw exceptions for individual source errors.
    ///     Instead, log errors and skip problematic sources.
    ///     MAY throw for catastrophic failures that prevent any discovery (e.g., invalid configuration).
    /// </exception>
    IEnumerable<SyntaxTree> GetSyntaxTrees();
}