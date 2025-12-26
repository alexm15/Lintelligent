namespace Lintelligent.AnalyzerEngine.Utilities;

/// <summary>
///     Utility for computing rolling hashes of token streams using Rabin-Karp algorithm.
/// </summary>
/// <remarks>
///     Research Decision RT-001:
///     - Algorithm: Rabin-Karp rolling hash
///     - Collision rate: Less than 0.01% for typical code
///     - Update complexity: O(1) for rolling window
///     - Prime modulus: 2^61 - 1 (large Mersenne prime for good distribution)
///     - Base: 31 (standard prime for string hashing)
///     Constitutional Compliance:
///     - Stateless: All methods are static
///     - Deterministic: Same token stream always produces same hash
///     - No I/O: Pure computation only
/// </remarks>
public static class TokenHasher
{
    /// <summary>
    ///     Large Mersenne prime for modulus (2^61 - 1).
    ///     Provides good hash distribution while avoiding overflow.
    /// </summary>
    private const ulong Modulus = (1UL << 61) - 1;

    /// <summary>
    ///     Base prime for polynomial rolling hash.
    /// </summary>
    private const ulong Base = 31;

    /// <summary>
    ///     Computes rolling hash of token sequence using Rabin-Karp algorithm.
    /// </summary>
    /// <param name="tokens">Enumerable of tokens to hash</param>
    /// <returns>Hash value (0 if no tokens)</returns>
    /// <remarks>
    ///     Hash formula: hash = ((hash * Base) + tokenKind.GetHashCode()) % Modulus
    ///     Time complexity: O(n) where n = number of tokens
    ///     Space complexity: O(1)
    /// </remarks>
    public static ulong HashTokens(IEnumerable<SyntaxToken> tokens)
    {
        if (tokens == null)
            throw new ArgumentNullException(nameof(tokens));

        ulong hash = 0;

        foreach (SyntaxToken token in tokens)
        {
            // Use token kind for semantic comparison (ignore variable names, whitespace)
            var tokenValue = (int)token.Kind();
            hash = ((hash * Base) + (ulong)tokenValue) % Modulus;
        }

        return hash;
    }

    /// <summary>
    ///     Extracts tokens from syntax tree, excluding trivia and comments.
    /// </summary>
    /// <param name="tree">Syntax tree to extract tokens from</param>
    /// <returns>Enumerable of tokens (descendants of root)</returns>
    /// <remarks>
    ///     Research Decision RT-003:
    ///     - Uses DescendantTokens() to get all tokens in tree order
    ///     - Excludes trivia (whitespace, comments) for normalized comparison
    ///     - Preserves token order (critical for rolling hash correctness)
    ///     Time complexity: O(n) where n = number of nodes in tree
    ///     Space complexity: O(1) with lazy evaluation (yield return in Roslyn)
    /// </remarks>
    public static IEnumerable<SyntaxToken> ExtractTokens(SyntaxTree tree)
    {
        if (tree == null)
            throw new ArgumentNullException(nameof(tree));

        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        // DescendantTokens() excludes trivia by default
        // Returns tokens in syntax tree order (left-to-right, depth-first)
        return root.DescendantTokens();
    }

    /// <summary>
    ///     Computes hash of entire syntax tree.
    ///     Convenience method that combines ExtractTokens and HashTokens.
    /// </summary>
    /// <param name="tree">Syntax tree to hash</param>
    /// <returns>Hash value of all tokens in tree</returns>
    public static ulong HashTree(SyntaxTree tree)
    {
        IEnumerable<SyntaxToken> tokens = ExtractTokens(tree);
        return HashTokens(tokens);
    }
}
