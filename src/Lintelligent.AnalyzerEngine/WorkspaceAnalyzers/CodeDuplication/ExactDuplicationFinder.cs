using Lintelligent.AnalyzerEngine.Configuration;
using Lintelligent.AnalyzerEngine.Utilities;
using Microsoft.CodeAnalysis;

namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
/// Detects exact code duplications across syntax trees using token-based hashing.
/// Implements two-pass algorithm: hash all code blocks, then group matches.
/// Detects both whole-file duplications and statement sequences within methods.
/// </summary>
/// <remarks>
/// Algorithm (RT-004 from research):
/// Pass 1: Hash all files AND extract statement sequences, build hash → instances mapping  
/// Pass 2: For hashes with 2+ instances, create DuplicationGroups
/// Memory: ~10MB per 100k LOC (hash table only, trees stay in memory externally)
/// Granularity: Detects duplicated code at multiple levels:
/// - Whole files (entire syntax trees)
/// - Statement sequences within methods (minimum 3 consecutive statements)
/// - Constructors, property accessors, and other code blocks
/// </remarks>
public static class ExactDuplicationFinder
{
    /// <summary>
    /// Finds all exact duplications across the provided syntax trees.
    /// Detects both whole-file duplications and duplicated statement sequences within methods.
    /// </summary>
    /// <param name="trees">Syntax trees to analyze for duplications.</param>
    /// <param name="options">Options for filtering duplications (null uses defaults).</param>
    /// <returns>Duplication groups ordered by severity (most severe first).</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="trees"/> is null.</exception>
    public static IEnumerable<DuplicationGroup> FindDuplicates(
        IEnumerable<SyntaxTree> trees,
        DuplicationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(trees);

        options ??= new DuplicationOptions();
        var instancesByHash = BuildHashMapping(trees);
        return CreateDuplicationGroups(instancesByHash, options);
    }

    private static Dictionary<ulong, List<DuplicationInstance>> BuildHashMapping(
        IEnumerable<SyntaxTree> trees)
    {
        // Pass 1: Hash all trees and statement sequences, build hash → instances mapping
        var instancesByHash = new Dictionary<ulong, List<DuplicationInstance>>();

        foreach (var tree in trees)
        {
            // Hash whole files (original behavior)
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

            // Create duplication instance for whole file
            var instance = new DuplicationInstance(
                filePath,
                string.Empty, // Will be filled by DuplicationDetector with workspace context
                location.Span,
                tokenCount,
                hash,
                sourceText);

            AddToHashMapping(instancesByHash, hash, instance);
            
            // Also hash statement sequences within methods (NEW: sub-block detection)
            foreach (var sequence in StatementSequenceExtractor.ExtractSequences(tree, minStatements: 3))
            {
                var sequenceTokens = sequence.ExtractTokens().ToList();
                var sequenceHash = TokenHasher.HashTokens(sequenceTokens);
                var sequenceTokenCount = sequenceTokens.Count;
                
                // Reconstruct source text from statements
                var sequenceSource = string.Join(Environment.NewLine, 
                    sequence.Statements.Select(s => s.ToFullString()));
                
                var sequenceInstance = new DuplicationInstance(
                    sequence.FilePath,
                    sequence.Context, // e.g., "Method: CreateUser"
                    sequence.Location,
                    sequenceTokenCount,
                    sequenceHash,
                    sequenceSource);
                    
                AddToHashMapping(instancesByHash, sequenceHash, sequenceInstance);
            }
        }

        return instancesByHash;
    }
    
    private static void AddToHashMapping(
        Dictionary<ulong, List<DuplicationInstance>> mapping,
        ulong hash,
        DuplicationInstance instance)
    {
        if (!mapping.TryGetValue(hash, out var instances))
        {
            instances = new List<DuplicationInstance>();
            mapping[hash] = instances;
        }
        instances.Add(instance);
    }

    private static IEnumerable<DuplicationGroup> CreateDuplicationGroups(
        Dictionary<ulong, List<DuplicationInstance>> instancesByHash,
        DuplicationOptions options)
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

                // Apply threshold filtering: report if meets MinLines OR MinTokens
                if (lineCount >= options.MinLines || tokenCount >= options.MinTokens)
                {
                    var group = new DuplicationGroup(hash, instances, lineCount, tokenCount);
                    groups.Add(group);
                }
            }
        }

        // Order by severity (most severe first)
        return groups.OrderByDescending(g => g.GetSeverityScore());
    }
}
