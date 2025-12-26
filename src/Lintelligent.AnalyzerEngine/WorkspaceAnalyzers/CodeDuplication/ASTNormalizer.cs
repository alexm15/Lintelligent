namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
///     Normalizes AST nodes to enable structural similarity detection.
///     Transforms code with different identifiers and literals into a canonical form
///     for comparing code structure independently of naming choices.
/// </summary>
public class AstNormalizer
{
    private int _identifierCounter;

    /// <summary>
    ///     Normalizes all identifiers in a syntax tree to canonical names.
    ///     This allows comparison of code structure regardless of variable/method naming.
    /// </summary>
    /// <param name="root">The root syntax node to normalize.</param>
    /// <returns>A new syntax node with all identifiers renamed to canonical forms (id_0, id_1, etc.).</returns>
    public SyntaxNode NormalizeIdentifiers(SyntaxNode root)
    {
        _identifierCounter = 0;
        var identifierMapping = new Dictionary<string, string>(StringComparer.Ordinal);

        // Use CSharpSyntaxRewriter to walk the tree and replace identifiers
        var rewriter = new IdentifierNormalizingRewriter(identifierMapping, () => _identifierCounter++);
        return rewriter.Visit(root);
    }

    /// <summary>
    ///     Normalizes all literals in a syntax tree to type placeholders.
    ///     This allows comparison of code structure regardless of literal values.
    /// </summary>
    /// <param name="root">The root syntax node to normalize.</param>
    /// <returns>A new syntax node with all literals replaced with type placeholders.</returns>
    public static SyntaxNode NormalizeLiterals(SyntaxNode root)
    {
        var rewriter = new LiteralNormalizingRewriter();
        return rewriter.Visit(root);
    }

    private sealed class IdentifierNormalizingRewriter : CSharpSyntaxRewriter
    {
        private readonly Func<int> _getNextId;
        private readonly Dictionary<string, string> _identifierMapping;

        public IdentifierNormalizingRewriter(Dictionary<string, string> identifierMapping, Func<int> getNextId)
        {
            _identifierMapping = identifierMapping;
            _getNextId = getNextId;
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var originalName = node.Identifier.Text;

            // Don't normalize built-in types or keywords
            if (SyntaxFacts.GetKeywordKind(originalName) != SyntaxKind.None)
                return base.VisitIdentifierName(node);

            if (!_identifierMapping.TryGetValue(originalName, out var normalizedName))
            {
                normalizedName = $"id_{_getNextId()}";
                _identifierMapping[originalName] = normalizedName;
            }

            return node.WithIdentifier(SyntaxFactory.Identifier(normalizedName));
        }

        public override SyntaxNode? VisitParameter(ParameterSyntax node)
        {
            var originalName = node.Identifier.Text;

            if (!_identifierMapping.TryGetValue(originalName, out var normalizedName))
            {
                normalizedName = $"id_{_getNextId()}";
                _identifierMapping[originalName] = normalizedName;
            }

            return node.WithIdentifier(SyntaxFactory.Identifier(normalizedName));
        }

        public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var originalName = node.Identifier.Text;

            if (!_identifierMapping.TryGetValue(originalName, out var normalizedName))
            {
                normalizedName = $"id_{_getNextId()}";
                _identifierMapping[originalName] = normalizedName;
            }

            return node.WithIdentifier(SyntaxFactory.Identifier(normalizedName));
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var originalName = node.Identifier.Text;

            if (!_identifierMapping.TryGetValue(originalName, out var normalizedName))
            {
                normalizedName = $"id_{_getNextId()}";
                _identifierMapping[originalName] = normalizedName;
            }

            return base.VisitMethodDeclaration(node.WithIdentifier(SyntaxFactory.Identifier(normalizedName)));
        }
    }

    private sealed class LiteralNormalizingRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            return node.Kind() switch
            {
                SyntaxKind.NumericLiteralExpression => SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0)),
                SyntaxKind.StringLiteralExpression => SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(string.Empty)),
                SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression =>
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.TrueLiteralExpression),
                _ => base.VisitLiteralExpression(node)
            };
        }
    }
}
