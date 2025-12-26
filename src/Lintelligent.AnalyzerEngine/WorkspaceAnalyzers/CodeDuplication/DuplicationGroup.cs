namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
///     Represents a set of related duplication instances (same code in multiple locations).
/// </summary>
/// <remarks>
///     Immutable class for thread-safety. Contains all occurrences of the same duplicated code block.
///     Used to calculate severity scores and identify affected projects.
/// </remarks>
public class DuplicationGroup
{
    /// <summary>
    ///     Creates a new duplication group.
    /// </summary>
    /// <param name="hash">Shared hash value</param>
    /// <param name="instances">All instances (must have at least 2)</param>
    /// <param name="lineCount">Number of lines in duplicated block</param>
    /// <param name="tokenCount">Number of tokens in duplicated block</param>
    /// <exception cref="ArgumentException">If instances count is less than 2</exception>
    public DuplicationGroup(ulong hash, IReadOnlyList<DuplicationInstance> instances, int lineCount, int tokenCount)
    {
        if (instances == null || instances.Count < 2)
            throw new ArgumentException("Duplication group must have at least 2 instances", nameof(instances));

        if (lineCount <= 0)
            throw new ArgumentException("Line count must be positive", nameof(lineCount));

        if (tokenCount <= 0)
            throw new ArgumentException("Token count must be positive", nameof(tokenCount));

        Hash = hash;
        Instances = instances
            .OrderBy(i => i.ProjectName, StringComparer.Ordinal)
            .ThenBy(i => i.FilePath, StringComparer.Ordinal)
            .ThenBy(i => i.Location.Start.Line)
            .ToList();
        LineCount = lineCount;
        TokenCount = tokenCount;
    }

    /// <summary>
    ///     Shared hash value across all instances in this group.
    /// </summary>
    public ulong Hash { get; }

    /// <summary>
    ///     All occurrences of this duplication (minimum 2 required).
    ///     Sorted by: ProjectName, then FilePath, then Location start line.
    /// </summary>
    public IReadOnlyList<DuplicationInstance> Instances { get; }

    /// <summary>
    ///     Number of lines in the duplicated block.
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    ///     Number of tokens in the duplicated block.
    /// </summary>
    public int TokenCount { get; }

    /// <summary>
    ///     Calculates severity score for prioritizing refactoring efforts.
    ///     Higher score = more severe (more instances × more lines = more duplication to fix).
    /// </summary>
    /// <returns>Severity score (instances count × line count)</returns>
    public int GetSeverityScore()
    {
        return Instances.Count * LineCount;
    }

    /// <summary>
    ///     Returns unique list of project names affected by this duplication.
    /// </summary>
    /// <returns>Enumerable of unique project names</returns>
    public IEnumerable<string> GetAffectedProjects()
    {
        return Instances
            .Select(i => i.ProjectName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal);
    }
}
