using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
/// Represents a sequence of consecutive statements that can be analyzed for duplication.
/// </summary>
public sealed class StatementSequence
{
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public string Context { get; }
    public string FilePath { get; }
    public LinePositionSpan Location { get; }
    
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
        var firstLocation = statements[0].GetLocation().GetLineSpan();
        var lastLocation = statements[statements.Count - 1].GetLocation().GetLineSpan();
        
        Location = new LinePositionSpan(
            firstLocation.StartLinePosition,
            lastLocation.EndLinePosition);
    }
    
    /// <summary>
    /// Extracts all tokens from this statement sequence for hashing.
    /// </summary>
    public IEnumerable<SyntaxToken> ExtractTokens()
    {
        foreach (var statement in Statements)
        {
            foreach (var token in statement.DescendantTokens())
            {
                yield return token;
            }
        }
    }
}
