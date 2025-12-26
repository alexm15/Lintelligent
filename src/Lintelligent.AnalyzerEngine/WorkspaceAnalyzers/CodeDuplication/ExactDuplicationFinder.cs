using Lintelligent.AnalyzerEngine.Utilities;
using Microsoft.CodeAnalysis;

namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
/// Detects exact code duplications across syntax trees using token-based hashing.
/// Implements two-pass algorithm: hash all code blocks, then group matches.
/// </summary>
/// <remarks>
/// Algorithm (RT-004 from research):
/// Pass 1: Hash all files, build hash → instances mapping  
/// Pass 2: For hashes with 2+ instances, create DuplicationGroups
/// Memory: ~10MB per 100k LOC (hash table only, trees stay in memory externally)
/// </remarks>
public static class ExactDuplicationFinder
{
    /// <summary>
    /// Finds all exact duplications across the provided syntax trees.
    /// </summary>
    /// <param name="trees">Syntax trees to analyze for duplications.</param>
    /// <returns>Duplication groups ordered by severity (most severe first).</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="trees"/> is null.</exception>
    public static IEnumerable<DuplicationGroup> FindDuplicates(IEnumerable<SyntaxTree> trees)
    {
        ArgumentNullException.ThrowIfNull(trees);

        var instancesByHash = BuildHashMapping(trees);
        return CreateDuplicationGroups(instancesByHash);
    }

    private static Dictionary<ulong, List<DuplicationInstance>> BuildHashMapping(IEnumerable<SyntaxTree> trees)
    {

        // Pass 1: Hash all trees and build hash → instances mapping
        var instancesByHash = new Dictionary<ulong, List<DuplicationInstance>>();

        foreach (var tree in trees)
        {
            var hash = TokenHasher.HashTree(tree);
            var filePath = tree.FilePath;

            // Get root and location
            var root = tree.GetRoot();
            var location = root.GetLocation().GetLineSpan();

            // Extract tokens for token count
            var tokens = TokenHasher.ExtractTokens(tree).ToList();
            var tokenCount = tokens.Count;

            // Get source text
            var sourceText = root.ToFullString();

            // Create duplication instance
            var instance = new DuplicationInstance(
                filePath,
                string.Empty, // Will be filled by DuplicationDetector with workspace context
                location.Span,
                tokenCount,
                hash,
                sourceText);

            // Add to hash mapping
            if (!instancesByHash.TryGetValue(hash, out var instances))
            {
                instances = new List<DuplicationInstance>();
                instancesByHash[hash] = instances;
            }

            instances.Add(instance);
        }

        return instancesByHash;
    }

    private static IEnumerable<DuplicationGroup> CreateDuplicationGroups(
        Dictionary<ulong, List<DuplicationInstance>> instancesByHash)
    {
        // Pass 2: Create duplication groups for hashes with 2+ instances
        var groups = new List<DuplicationGroup>();

        foreach (var (hash, instances) in instancesByHash)
        {
            if (instances.Count >= 2)
            {
                // Calculate line count from first instance (all should be identical)
                var lineCount = instances[0].Location.End.Line - instances[0].Location.Start.Line + 1;
                var tokenCount = instances[0].TokenCount;

                var group = new DuplicationGroup(hash, instances, lineCount, tokenCount);
                groups.Add(group);
            }
        }

        // Order by severity (most severe first)
        return groups.OrderByDescending(g => g.GetSeverityScore());
    }
}
