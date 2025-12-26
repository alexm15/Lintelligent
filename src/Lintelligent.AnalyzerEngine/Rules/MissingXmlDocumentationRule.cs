using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.AnalyzerEngine.Rules;

public class MissingXmlDocumentationRule : IAnalyzerRule
{
    public string Id => "LNT008";
    public string Description => "Public APIs should have XML documentation";
    public Severity Severity => Severity.Info;
    public string Category => DiagnosticCategories.Documentation;

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
    {
        if (IsGeneratedCode(tree))
            yield break;

        SyntaxNode root = tree.GetRoot();

        foreach (DiagnosticResult result in CheckClasses(tree, root))
            yield return result;

        foreach (DiagnosticResult result in CheckMethods(tree, root))
            yield return result;

        foreach (DiagnosticResult result in CheckProperties(tree, root))
            yield return result;
    }

    private IEnumerable<DiagnosticResult> CheckClasses(SyntaxTree tree, SyntaxNode root)
    {
        foreach (ClassDeclarationSyntax classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            if (IsPublicOrProtected(classDecl.Modifiers) && !HasXmlDocumentation(classDecl))
            {
                var className = classDecl.Identifier.ValueText;
                var line = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                var message = $"Public class '{className}' is missing XML documentation. " +
                              "Add a /// <summary> comment to describe the API.";

                yield return new DiagnosticResult(
                    tree.FilePath,
                    Id,
                    message,
                    line,
                    Severity,
                    Category
                );
            }
        }
    }

    private IEnumerable<DiagnosticResult> CheckMethods(SyntaxTree tree, SyntaxNode root)
    {
        foreach (MethodDeclarationSyntax methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (IsPublicOrProtected(methodDecl.Modifiers) && !HasXmlDocumentation(methodDecl))
            {
                var methodName = methodDecl.Identifier.ValueText;
                var line = methodDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                var message = $"Public method '{methodName}' is missing XML documentation. " +
                              "Add a /// <summary> comment to describe the API.";

                yield return new DiagnosticResult(
                    tree.FilePath,
                    Id,
                    message,
                    line,
                    Severity,
                    Category
                );
            }
        }
    }

    private IEnumerable<DiagnosticResult> CheckProperties(SyntaxTree tree, SyntaxNode root)
    {
        foreach (PropertyDeclarationSyntax propertyDecl in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (!IsPublicOrProtected(propertyDecl.Modifiers) || HasXmlDocumentation(propertyDecl)) continue;
            var propertyName = propertyDecl.Identifier.ValueText;
            var line = propertyDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var message = $"Public property '{propertyName}' is missing XML documentation. " +
                          "Add a /// <summary> comment to describe the API.";

            yield return new DiagnosticResult(
                tree.FilePath,
                Id,
                message,
                line,
                Severity,
                Category
            );
        }
    }

    private static bool IsPublicOrProtected(SyntaxTokenList modifiers)
    {
        return modifiers.Any(SyntaxKind.PublicKeyword) ||
               modifiers.Any(SyntaxKind.ProtectedKeyword);
    }

    private static bool HasXmlDocumentation(SyntaxNode node)
    {
        SyntaxTriviaList leadingTrivia = node.GetLeadingTrivia();

        // Check for XML documentation comment trivia
        return (from trivia in leadingTrivia
                where trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                      trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                select trivia.ToString())
            .Any(triviaText => triviaText.Contains("<summary") || triviaText.Contains("<inheritdoc"));
    }

    private static bool IsGeneratedCode(SyntaxTree tree)
    {
        var fileName = Path.GetFileName(tree.FilePath);
        if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains(".Generated."))
            return true;

        SyntaxNode root = tree.GetRoot();
        IEnumerable<SyntaxTrivia> leadingTrivia = root.GetLeadingTrivia().Take(10);
        return leadingTrivia.Any(t =>
            t.ToString().Contains("<auto-generated>") ||
            t.ToString().Contains("<auto-generated />"));
    }
}
