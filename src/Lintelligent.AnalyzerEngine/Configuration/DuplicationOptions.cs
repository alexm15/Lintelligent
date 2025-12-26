namespace Lintelligent.AnalyzerEngine.Configuration;

/// <summary>
/// Configuration options for code duplication detection.
/// </summary>
public sealed class DuplicationOptions
{
    /// <summary>
    /// Minimum number of lines for a code block to be considered a duplication.
    /// Default: 10 lines.
    /// </summary>
    public int MinLines { get; set; } = 10;

    /// <summary>
    /// Minimum number of tokens for a code block to be considered a duplication.
    /// Default: 50 tokens.
    /// </summary>
    public int MinTokens { get; set; } = 50;

    /// <summary>
    /// Enable structural similarity detection (detects code with identical structure but different identifiers).
    /// Default: false.
    /// </summary>
    public bool EnableStructuralSimilarity { get; set; }

    /// <summary>
    /// Minimum similarity percentage (0-100) for structural similarity matches.
    /// Only applies when EnableStructuralSimilarity is true.
    /// Default: 85.0 (85%).
    /// </summary>
    public double MinSimilarityPercent { get; set; } = 85.0;

    /// <summary>
    /// Creates a new instance with default values.
    /// </summary>
    public DuplicationOptions()
    {
    }

    /// <summary>
    /// Creates a new instance with specified values.
    /// </summary>
    public DuplicationOptions(int minLines, int minTokens)
    {
        MinLines = minLines;
        MinTokens = minTokens;
    }

    /// <summary>
    /// Creates a new instance with all values specified.
    /// </summary>
    public DuplicationOptions(int minLines, int minTokens, bool enableStructuralSimilarity, double minSimilarityPercent)
    {
        MinLines = minLines;
        MinTokens = minTokens;
        EnableStructuralSimilarity = enableStructuralSimilarity;
        MinSimilarityPercent = minSimilarityPercent;
    }
}
