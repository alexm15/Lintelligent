using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.AnalyzerEngine.Rules;

public class DeadCodeRule : IAnalyzerRule
{
    public string Id => "LNT006";
    public string Description => "Unused private members should be removed";
    public Severity Severity => Severity.Info;
    public string Category => DiagnosticCategories.Maintainability;

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
    {
        if (IsGeneratedCode(tree))
            yield break;

        SyntaxNode root = tree.GetRoot();
        IEnumerable<ClassDeclarationSyntax> classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (ClassDeclarationSyntax classDecl in classes)
        {
            foreach (DiagnosticResult result in CheckPrivateMethods(tree, classDecl))
                yield return result;

            foreach (DiagnosticResult result in CheckPrivateFields(tree, classDecl))
                yield return result;
        }
    }

    private IEnumerable<DiagnosticResult> CheckPrivateMethods(SyntaxTree tree, ClassDeclarationSyntax classDecl)
    {
        // Find all private methods
        IEnumerable<MethodDeclarationSyntax> privateMethods = classDecl.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword));

        foreach (MethodDeclarationSyntax method in privateMethods)
        {
            var methodName = method.Identifier.ValueText;

            // Heuristic: exclude methods that might implement interface members
            if (MightImplementInterface(classDecl))
                continue;

            // Search for references within the class
            if (IsMethodReferenced(methodName, classDecl, method)) continue;
            var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var message = $"Private method '{methodName}' is never used. Consider removing dead code.";

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

    private IEnumerable<DiagnosticResult> CheckPrivateFields(SyntaxTree tree, ClassDeclarationSyntax classDecl)
    {
        // Find all private fields
        IEnumerable<FieldDeclarationSyntax> privateFields = classDecl.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(SyntaxKind.PrivateKeyword));

        foreach (FieldDeclarationSyntax fieldDecl in privateFields)
        {
            foreach (VariableDeclaratorSyntax variable in fieldDecl.Declaration.Variables)
            {
                var fieldName = variable.Identifier.ValueText;

                // Search for references within the class
                if (!IsFieldReferenced(fieldName, classDecl, fieldDecl))
                {
                    var line = fieldDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var message = $"Private field '{fieldName}' is never used. Consider removing dead code.";

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
    }

    private static bool IsMethodReferenced(string methodName, ClassDeclarationSyntax classDecl,
        MethodDeclarationSyntax excludeMethod)
    {
        // Search for any identifier tokens matching the method name (excluding the declaration itself)
        IEnumerable<IdentifierNameSyntax> identifiers = classDecl.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => string.Equals(id.Identifier.ValueText, methodName, StringComparison.Ordinal));

        foreach (IdentifierNameSyntax identifier in identifiers)
        {
            // Skip if this is the method declaration itself
            if (identifier.Ancestors().Any(a => a == excludeMethod))
                continue;

            // Found a reference
            return true;
        }

        return false;
    }

    private static bool IsFieldReferenced(string fieldName, ClassDeclarationSyntax classDecl,
        FieldDeclarationSyntax excludeField)
    {
        // Search for any identifier tokens matching the field name (excluding the declaration itself)
        IEnumerable<IdentifierNameSyntax> identifiers = classDecl.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => string.Equals(id.Identifier.ValueText, fieldName, StringComparison.Ordinal));

        foreach (IdentifierNameSyntax identifier in identifiers)
        {
            // Skip if this is the field declaration itself (in initializer)
            if (identifier.Ancestors().Any(a => a == excludeField))
                continue;

            // Found a reference
            return true;
        }

        return false;
    }

    private static bool MightImplementInterface(ClassDeclarationSyntax classDecl)
    {
        // Heuristic: if the class has a base list, check if any interface might define a member
        // This is a simple name-based check (not full semantic analysis)
        return classDecl.BaseList != null &&
               // If the class implements any interfaces, we assume the member might be an explicit implementation
               // This is a conservative heuristic to avoid false positives
               classDecl.BaseList.Types.Any();
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
