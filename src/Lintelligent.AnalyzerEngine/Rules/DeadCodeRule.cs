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

        var root = tree.GetRoot();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var classDecl in classes)
        {
            // Find all private methods
            var privateMethods = classDecl.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword));

            foreach (var method in privateMethods)
            {
                var methodName = method.Identifier.ValueText;
                
                // Heuristic: exclude methods that might implement interface members
                if (MightImplementInterface(methodName, classDecl))
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

            // Find all private fields
            var privateFields = classDecl.DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .Where(f => f.Modifiers.Any(SyntaxKind.PrivateKeyword));

            foreach (var fieldDecl in privateFields)
            {
                foreach (var variable in fieldDecl.Declaration.Variables)
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
    }

    private static bool IsMethodReferenced(string methodName, ClassDeclarationSyntax classDecl, MethodDeclarationSyntax excludeMethod)
    {
        // Search for any identifier tokens matching the method name (excluding the declaration itself)
        var identifiers = classDecl.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => id.Identifier.ValueText == methodName);

        foreach (var identifier in identifiers)
        {
            // Skip if this is the method declaration itself
            if (identifier.Ancestors().Any(a => a == excludeMethod))
                continue;

            // Found a reference
            return true;
        }

        return false;
    }

    private static bool IsFieldReferenced(string fieldName, ClassDeclarationSyntax classDecl, FieldDeclarationSyntax excludeField)
    {
        // Search for any identifier tokens matching the field name (excluding the declaration itself)
        var identifiers = classDecl.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => id.Identifier.ValueText == fieldName);

        foreach (var identifier in identifiers)
        {
            // Skip if this is the field declaration itself (in initializer)
            if (identifier.Ancestors().Any(a => a == excludeField))
                continue;

            // Found a reference
            return true;
        }

        return false;
    }

    private static bool MightImplementInterface(string memberName, ClassDeclarationSyntax classDecl)
    {
        // Heuristic: if the class has a base list, check if any interface might define this member
        // This is a simple name-based check (not full semantic analysis)
        return classDecl.BaseList != null &&
               // If the class implements any interfaces, we assume the member might be an explicit implementation
               // This is a conservative heuristic to avoid false positives
               classDecl.BaseList.Types.Any();
    }

    private static bool IsGeneratedCode(SyntaxTree tree)
    {
        var fileName = Path.GetFileName(tree.FilePath);
        if (fileName.EndsWith(".Designer.cs") || 
            fileName.EndsWith(".g.cs") || 
            fileName.Contains(".Generated."))
            return true;

        var root = tree.GetRoot();
        var leadingTrivia = root.GetLeadingTrivia().Take(10);
        return leadingTrivia.Any(t => 
            t.ToString().Contains("<auto-generated>") ||
            t.ToString().Contains("<auto-generated />"));
    }
}
