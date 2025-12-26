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
}
