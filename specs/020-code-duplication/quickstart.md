# Quickstart: Implementing a Workspace Analyzer

**Feature**: 020-code-duplication

## Creating a Workspace Analyzer

### 1. Implement IWorkspaceAnalyzer

```csharp
public class DuplicationDetector : IWorkspaceAnalyzer
{
    public string Id => "LNT-DUP";
    public string Description => "Detects duplicate code blocks across files";
    public Severity Severity => Severity.Warning;
    public string Category => "Maintainability";

    public IEnumerable<DiagnosticResult> Analyze(
        IReadOnlyList<SyntaxTree> trees,
        WorkspaceContext context)
    {
        // Extract tokens from all files
        var hashedBlocks = HashAllCodeBlocks(trees);
        
        // Find duplications
        var duplications = FindDuplicates(hashedBlocks);
        
        // Convert to diagnostics
        foreach (var group in duplications)
        {
            yield return CreateDiagnostic(group, context);
        }
    }
}
```

### 2. Register in CLI

```csharp
// Bootstrapper.cs
services.AddSingleton<IWorkspaceAnalyzer, DuplicationDetector>();
```

### 3. Test

```csharp
[Fact]
public void Analyze_TwoDuplicateBlocks_ReportsOneDuplication()
{
    // Arrange
    var tree1 = ParseCode("class A { void M() { DoStuff(); } }");
    var tree2 = ParseCode("class B { void M() { DoStuff(); } }");
    var trees = new[] { tree1, tree2 };
    var context = new WorkspaceContext 
    { 
        Solution = CreateTestSolution(),
        ProjectsByPath = CreateTestProjects()
    };
    var analyzer = new DuplicationDetector();

    // Act
    var results = analyzer.Analyze(trees, context).ToList();

    // Assert
    results.Should().ContainSingle();
    results[0].Message.Should().Contain("duplicated in 2 files");
}
```

## Token Hashing Pattern

```csharp
private ulong HashTokens(IEnumerable<SyntaxToken> tokens)
{
    const ulong Prime = 31;
    ulong hash = 0;
    
    foreach (var token in tokens)
    {
        hash = (hash * Prime) + (ulong)token.Kind().GetHashCode();
    }
    
    return hash;
}
```
