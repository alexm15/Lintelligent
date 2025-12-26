namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
///     Extracts statement sequences from syntax trees for fine-grained duplication detection.
///     Enables detection of duplicated code blocks within different methods.
/// </summary>
public static class StatementSequenceExtractor
{
    /// <summary>
    ///     Extracts all statement sequences from a syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to analyze.</param>
    /// <param name="minStatements">Minimum number of consecutive statements to extract (default: 3).</param>
    /// <returns>All statement sequences found in the tree.</returns>
    public static IEnumerable<StatementSequence> ExtractSequences(
        SyntaxTree tree,
        int minStatements = 3)
    {
        SyntaxNode root = tree.GetRoot();
        var filePath = tree.FilePath;

        return ExtractFromMethods(root, filePath, minStatements)
            .Concat(ExtractFromConstructors(root, filePath, minStatements))
            .Concat(ExtractFromAccessors(root, filePath, minStatements))
            .Concat(ExtractFromCatchBlocks(root, filePath, minStatements))
            .Concat(ExtractFromTryBlocks(root, filePath, minStatements));
    }

    private static IEnumerable<StatementSequence> ExtractFromMethods(SyntaxNode root, string filePath,
        int minStatements)
    {
        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (method.Body != null)
            {
                var context = $"Method: {method.Identifier.Text}";
                foreach (StatementSequence seq in ExtractSequencesFromBlock(method.Body, context, filePath,
                             minStatements))
                    yield return seq;
            }
        }
    }

    private static IEnumerable<StatementSequence> ExtractFromConstructors(SyntaxNode root, string filePath,
        int minStatements)
    {
        foreach (ConstructorDeclarationSyntax constructor in root.DescendantNodes()
                     .OfType<ConstructorDeclarationSyntax>())
        {
            if (constructor.Body != null)
            {
                var context = $"Constructor: {constructor.Identifier.Text}";
                foreach (StatementSequence seq in ExtractSequencesFromBlock(constructor.Body, context, filePath,
                             minStatements))
                    yield return seq;
            }
        }
    }

    private static IEnumerable<StatementSequence> ExtractFromAccessors(SyntaxNode root, string filePath,
        int minStatements)
    {
        foreach (AccessorDeclarationSyntax accessor in root.DescendantNodes().OfType<AccessorDeclarationSyntax>())
        {
            if (accessor.Body != null)
            {
                var context = accessor.Parent?.Parent is PropertyDeclarationSyntax property
                    ? $"Property: {property.Identifier.Text}.{accessor.Keyword.Text}"
                    : $"Accessor: {accessor.Keyword.Text}";

                foreach (StatementSequence seq in ExtractSequencesFromBlock(accessor.Body, context, filePath,
                             minStatements))
                    yield return seq;
            }
        }
    }

    private static IEnumerable<StatementSequence> ExtractFromCatchBlocks(SyntaxNode root, string filePath,
        int minStatements)
    {
        foreach (CatchClauseSyntax catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (catchClause.Block != null)
            {
                var methodName = GetContainingMethodName(catchClause);
                var context = $"Method: {methodName} (catch block)";
                foreach (StatementSequence seq in ExtractSequencesFromBlock(catchClause.Block, context, filePath,
                             minStatements))
                    yield return seq;
            }
        }
    }

    private static IEnumerable<StatementSequence> ExtractFromTryBlocks(SyntaxNode root, string filePath,
        int minStatements)
    {
        foreach (TryStatementSyntax tryStatement in root.DescendantNodes().OfType<TryStatementSyntax>())
        {
            if (tryStatement.Block != null)
            {
                var methodName = GetContainingMethodName(tryStatement);
                var context = $"Method: {methodName} (try block)";
                foreach (StatementSequence seq in ExtractSequencesFromBlock(tryStatement.Block, context, filePath,
                             minStatements))
                    yield return seq;
            }
        }
    }

    private static string GetContainingMethodName(SyntaxNode node)
    {
        return node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "Unknown";
    }

    private static IEnumerable<StatementSequence> ExtractSequencesFromBlock(
        BlockSyntax block,
        string context,
        string filePath,
        int minStatements)
    {
        if (block.Statements.Count >= minStatements)
        {
            foreach (StatementSequence seq in ExtractFromBlock(block, context, filePath, minStatements))
                yield return seq;
        }
    }

    /// <summary>
    ///     Extracts all overlapping statement sequences from a block.
    ///     Uses a sliding window approach to find all possible subsequences.
    /// </summary>
    private static IEnumerable<StatementSequence> ExtractFromBlock(
        BlockSyntax block,
        string context,
        string filePath,
        int minStatements)
    {
        SyntaxList<StatementSyntax> statements = block.Statements;

        // Generate all subsequences using sliding window
        // For a block with N statements and minStatements=3:
        // - Extract sequences of length 3, 4, 5, ..., N
        // - Starting at positions 0, 1, 2, ..., N-minStatements
        for (var startIndex = 0; startIndex <= statements.Count - minStatements; startIndex++)
        {
            for (var length = minStatements; length <= statements.Count - startIndex; length++)
            {
                StatementSyntax[] sequence = statements.Skip(startIndex).Take(length).ToArray();
                yield return new StatementSequence(sequence, context, filePath);
            }
        }
    }
}
