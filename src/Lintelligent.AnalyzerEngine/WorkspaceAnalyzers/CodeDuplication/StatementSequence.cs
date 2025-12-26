using Microsoft.CodeAnalysis.Text;

namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
///     Represents a sequence of consecutive statements that can be analyzed for duplication.
/// </summary>
public sealed class StatementSequence
{
    public StatementSequence(
        IReadOnlyList<StatementSyntax> statements,
        string context,
        string filePath)
    {
        if (statements.Count == 0)
            throw new ArgumentException("Statement sequence cannot be empty", nameof(statements));

        Statements = statements;
        Context = context;
        FilePath = filePath;

        // Calculate location from first to last statement
        FileLinePositionSpan firstLocation = statements[0].GetLocation().GetLineSpan();
        FileLinePositionSpan lastLocation = statements[statements.Count - 1].GetLocation().GetLineSpan();

        Location = new LinePositionSpan(
            firstLocation.StartLinePosition,
            lastLocation.EndLinePosition);
    }

    public IReadOnlyList<StatementSyntax> Statements { get; }
    public string Context { get; }
    public string FilePath { get; }
    public LinePositionSpan Location { get; }

    /// <summary>
    ///     Extracts all tokens from this statement sequence for hashing.
    /// </summary>
    public IEnumerable<SyntaxToken> ExtractTokens()
    {
        return Statements.SelectMany(statement => statement.DescendantTokens());
    }
}
