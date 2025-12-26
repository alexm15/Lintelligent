namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
///     Calculates structural similarity between two syntax trees.
///     Uses normalized AST comparison to detect code with identical structure
///     but different identifiers and literals.
/// </summary>
public static class SimilarityDetector
{
    /// <summary>
    ///     Calculates the similarity percentage between two syntax nodes.
    ///     Returns a value from 0.0 to 100.0 indicating structural similarity.
    /// </summary>
    /// <param name="node1">The first syntax node (should be normalized).</param>
    /// <param name="node2">The second syntax node (should be normalized).</param>
    /// <returns>Similarity percentage (0.0 = completely different, 100.0 = identical structure).</returns>
    public static double CalculateSimilarity(SyntaxNode node1, SyntaxNode node2)
    {
        // Extract structural information from both nodes
        List<SyntaxKind> structure1 = ExtractStructure(node1);
        List<SyntaxKind> structure2 = ExtractStructure(node2);

        // Calculate similarity using Levenshtein distance on syntax kind sequences
        var maxLength = Math.Max(structure1.Count, structure2.Count);
        if (maxLength == 0)
            return 100.0;

        var distance = CalculateLevenshteinDistance(structure1, structure2);
        var similarity = (1.0 - ((double)distance / maxLength)) * 100.0;

        return Math.Max(0.0, Math.Min(100.0, similarity));
    }

    /// <summary>
    ///     Extracts the structural sequence of syntax kinds from a syntax tree.
    ///     This represents the "shape" of the code independent of specific values.
    /// </summary>
    private static List<SyntaxKind> ExtractStructure(SyntaxNode node)
    {
        var structure = new List<SyntaxKind>();
        ExtractStructureRecursive(node, structure);
        return structure;
    }

    private static void ExtractStructureRecursive(SyntaxNode node, List<SyntaxKind> structure)
    {
        structure.Add(node.Kind());

        foreach (SyntaxNode child in node.ChildNodes()) ExtractStructureRecursive(child, structure);
    }

    /// <summary>
    ///     Calculates the Levenshtein distance between two sequences of syntax kinds.
    ///     This measures the minimum number of edit operations needed to transform one sequence into another.
    /// </summary>
    private static int CalculateLevenshteinDistance(List<SyntaxKind> s1, List<SyntaxKind> s2)
    {
        var n = s1.Count;
        var m = s2.Count;

        if (n == 0)
            return m;
        if (m == 0)
            return n;

        var dp = new int[n + 1, m + 1];

        // Initialize first row and column
        for (var i = 0; i <= n; i++)
            dp[i, 0] = i;
        for (var j = 0; j <= m; j++)
            dp[0, j] = j;

        // Calculate distance using dynamic programming
        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                dp[i, j] = Math.Min(
                    Math.Min(
                        dp[i - 1, j] + 1, // deletion
                        dp[i, j - 1] + 1), // insertion
                    dp[i - 1, j - 1] + cost); // substitution
            }
        }

        return dp[n, m];
    }
}
