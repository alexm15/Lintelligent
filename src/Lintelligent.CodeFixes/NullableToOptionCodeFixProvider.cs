using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.CodeFixes;

/// <summary>
///     Code fix provider for LNT200 - converts nullable return types to Option&lt;T&gt;.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableToOptionCodeFixProvider))]
[Shared]
public class NullableToOptionCodeFixProvider : CodeFixProvider
{
    private const string Title = "Convert to Option<T>";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("LNT200");

    public override FixAllProvider GetFixAllProvider()
    {
        // Allow fixing all instances in document/project/solution
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the method declaration
        var methodDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration == null) return;

        // Register the code fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => ConvertToOptionAsync(context.Document, methodDeclaration, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> ConvertToOptionAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // Get the non-nullable type (e.g., "string" from "string?")
        var returnType = methodDeclaration.ReturnType;
        var baseTypeName = GetNonNullableTypeName(returnType);

        // Create new return type: Option<T>
        var newReturnType = SyntaxFactory.ParseTypeName($"Option<{baseTypeName}>")
            .WithLeadingTrivia(returnType.GetLeadingTrivia())
            .WithTrailingTrivia(returnType.GetTrailingTrivia());

        // Transform all return statements
        var returnStatements = methodDeclaration.DescendantNodes().OfType<ReturnStatementSyntax>().ToList();
        var newMethod = methodDeclaration.WithReturnType(newReturnType);

        foreach (var returnStatement in returnStatements)
        {
            var newReturnStatement = TransformReturnStatement(returnStatement, baseTypeName);
            newMethod = newMethod.ReplaceNode(
                newMethod.DescendantNodes().OfType<ReturnStatementSyntax>()
                    .First(r => r.IsEquivalentTo(returnStatement)),
                newReturnStatement);
        }

        // Replace the old method with the new one
        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);

        // Add using LanguageExt; if not already present
        if (newRoot is CompilationUnitSyntax compilationUnit && !HasLanguageExtUsing(compilationUnit))
        {
            var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("LanguageExt"))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            newRoot = compilationUnit.AddUsings(usingDirective);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static ReturnStatementSyntax TransformReturnStatement(ReturnStatementSyntax returnStatement, string typeName)
    {
        if (returnStatement.Expression == null)
            return returnStatement;

        var expression = returnStatement.Expression;

        if (IsNullLiteral(expression))
        {
            var noneExpression = SyntaxFactory.ParseExpression($"Option<{typeName}>.None")
                .WithLeadingTrivia(expression.GetLeadingTrivia())
                .WithTrailingTrivia(expression.GetTrailingTrivia());

            return returnStatement.WithExpression(noneExpression);
        }

        // Keep non-null returns as-is - LanguageExt has implicit conversion from T to Option<T>
        // This avoids the Option<T>.Some(value) pattern which uses lazy evaluation
        return returnStatement;
    }

    private static string GetNonNullableTypeName(TypeSyntax type)
    {
        if (type is NullableTypeSyntax nullableType)
            return nullableType.ElementType.ToString();

        var typeString = type.ToString();
        return typeString.EndsWith("?", StringComparison.Ordinal) 
            ? typeString.Substring(0, typeString.Length - 1) 
            : typeString;
    }

    private static bool IsNullLiteral(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.IsKind(SyntaxKind.NullLiteralExpression);
    }

    private static bool HasLanguageExtUsing(CompilationUnitSyntax compilationUnit)
    {
        return compilationUnit.Usings.Any(u => string.Equals(u.Name?.ToString(), "LanguageExt", StringComparison.Ordinal));
    }
}
