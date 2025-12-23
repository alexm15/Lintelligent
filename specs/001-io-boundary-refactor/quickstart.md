# Quickstart: Refactor AnalyzerEngine IO Boundary

**Feature**: 001-io-boundary-refactor  
**Audience**: Developers implementing this refactor  
**Last Updated**: 2025-12-23

## What You're Building

A refactoring that removes file system IO operations from the AnalyzerEngine core, introducing an `ICodeProvider` abstraction to enable:
- ✅ In-memory testing without file system dependencies
- ✅ IDE plugin development with in-memory buffers
- ✅ Alternative frontends (web service, notebook, etc.)
- ✅ Constitutional compliance (Principle I: Layered Architecture)

## Before & After

### Before (Current - Constitutional Violation)
```csharp
// AnalyzerEngine is coupled to file system
var engine = new AnalyzerEngine(manager);
var results = engine.Analyze("/path/to/project"); // Directly performs IO

// Testing requires actual files on disk
[Fact]
public void TestAnalysis()
{
    // Must create real files - slow, brittle, not isolated
    File.WriteAllText("test.cs", "class Test { }");
    var engine = new AnalyzerEngine(manager);
    var results = engine.Analyze("test.cs");
    // Cleanup required
}
```

### After (Target - Constitutional Compliance)
```csharp
// AnalyzerEngine accepts syntax trees from ANY source
var provider = new FileSystemCodeProvider("/path/to/project");
var trees = provider.GetSyntaxTrees();
var results = engine.Analyze(trees); // Core engine has no IO

// Testing uses in-memory syntax trees - fast, isolated, deterministic
[Fact]
public void TestAnalysis()
{
    var tree = CSharpSyntaxTree.ParseText("class Test { }");
    var engine = new AnalyzerEngine(manager);
    var results = engine.Analyze(new[] { tree }); // No file system!
    Assert.NotEmpty(results);
}
```

## Quick Implementation Guide

### Step 1: Add ICodeProvider Interface

**File**: `src/Lintelligent.AnalyzerEngine/Abstractions/ICodeProvider.cs`

```csharp
using Microsoft.CodeAnalysis;

namespace Lintelligent.AnalyzerEngine.Abstractions;

public interface ICodeProvider
{
    IEnumerable<SyntaxTree> GetSyntaxTrees();
}
```

**Why**: Abstraction lives in AnalyzerEngine but implementations live in CLI layer.

---

### Step 2: Refactor AnalyzerEngine.Analyze()

**File**: `src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs`

**Before**:
```csharp
public IEnumerable<DiagnosticResult> Analyze(string projectPath)
{
    var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
    
    foreach (var file in files)
    {
        var source = File.ReadAllText(file);
        var tree = CSharpSyntaxTree.ParseText(source, path: file);
        
        foreach (var diagnostic in manager.Analyze(tree))
        {
            yield return diagnostic;
        }
    }
}
```

**After**:
```csharp
public IEnumerable<DiagnosticResult> Analyze(IEnumerable<SyntaxTree> syntaxTrees)
{
    foreach (var tree in syntaxTrees)
    {
        foreach (var diagnostic in _manager.Analyze(tree))
        {
            yield return diagnostic;
        }
    }
}
```

**Why**: Engine now accepts trees from any source, no IO operations.

---

### Step 3: Create FileSystemCodeProvider

**File**: `src/Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`

```csharp
using Microsoft.CodeAnalysis.CSharp;
using Lintelligent.AnalyzerEngine.Abstractions;

namespace Lintelligent.Cli.Providers;

public class FileSystemCodeProvider : ICodeProvider
{
    private readonly string _rootPath;
    
    public FileSystemCodeProvider(string rootPath)
    {
        if (!Directory.Exists(rootPath) && !File.Exists(rootPath))
            throw new ArgumentException($"Path does not exist: {rootPath}", nameof(rootPath));
            
        _rootPath = rootPath;
    }
    
    public IEnumerable<SyntaxTree> GetSyntaxTrees()
    {
        var files = File.Exists(_rootPath)
            ? new[] { _rootPath }
            : Directory.EnumerateFiles(_rootPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (var filePath in files)
        {
            string source;
            try
            {
                source = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to read {filePath}: {ex.Message}");
                continue; // Skip problematic files
            }
            
            yield return CSharpSyntaxTree.ParseText(source, path: filePath);
        }
    }
}
```

**Why**: IO operations moved to CLI layer, implements ICodeProvider contract.

---

### Step 4: Update ScanCommand

**File**: `src/Lintelligent.Cli/Commands/ScanCommand.cs`

**Before**:
```csharp
public async Task ExecuteAsync(string[] args)
{
    var path = args.FirstOrDefault() ?? ".";
    var results = engine.Analyze(path);
    var report = reporter.GenerateMarkdown(results);
    Console.WriteLine(report);
    await Task.CompletedTask;
}
```

**After**:
```csharp
public async Task ExecuteAsync(string[] args)
{
    var path = args.FirstOrDefault() ?? ".";
    
    var provider = new FileSystemCodeProvider(path);
    var trees = provider.GetSyntaxTrees();
    var results = _engine.Analyze(trees);
    
    var report = _reporter.GenerateMarkdown(results);
    Console.WriteLine(report);
    await Task.CompletedTask;
}
```

**Why**: CLI now uses provider to get trees, then passes to engine.

---

### Step 5: Add In-Memory Tests

**File**: `tests/Lintelligent.AnalyzerEngine.Tests/Analysis/AnalyzerEngineTests.cs`

```csharp
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public class AnalyzerEngineTests
{
    [Fact]
    public void Analyze_WithInMemorySyntaxTree_ReturnsResults()
    {
        // Arrange
        var sourceCode = @"
            public class TestClass
            {
                public void VeryLongMethodWithTooManyLines()
                {
                    // 100+ lines of code to trigger LongMethodRule
                }
            }";
        
        var tree = CSharpSyntaxTree.ParseText(sourceCode, path: "test.cs");
        var manager = new AnalyzerManager(new[] { new LongMethodRule() });
        var engine = new AnalyzerEngine(manager);
        
        // Act
        var results = engine.Analyze(new[] { tree }).ToList();
        
        // Assert
        Assert.NotEmpty(results); // LongMethodRule should trigger
        Assert.All(results, r => Assert.Equal("test.cs", r.FilePath));
    }
    
    [Fact]
    public void Analyze_WithEmptyCollection_ReturnsEmptyResults()
    {
        // Arrange
        var engine = new AnalyzerEngine(new AnalyzerManager(Array.Empty<IAnalyzerRule>()));
        
        // Act
        var results = engine.Analyze(Enumerable.Empty<SyntaxTree>()).ToList();
        
        // Assert
        Assert.Empty(results);
    }
    
    [Fact]
    public void Analyze_WithMultipleTrees_ProcessesAll()
    {
        // Arrange
        var tree1 = CSharpSyntaxTree.ParseText("class A { }", path: "a.cs");
        var tree2 = CSharpSyntaxTree.ParseText("class B { }", path: "b.cs");
        var trees = new[] { tree1, tree2 };
        
        var engine = new AnalyzerEngine(new AnalyzerManager(Array.Empty<IAnalyzerRule>()));
        
        // Act
        var results = engine.Analyze(trees).ToList();
        
        // Assert - should process both trees without errors
        Assert.Empty(results); // No rules registered, so no diagnostics
    }
}
```

**Why**: Demonstrates new capability - testing without file system.

---

### Step 6: Update Integration Tests

**File**: `tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs`

```csharp
[Fact]
public async Task ScanCommand_WithRealDirectory_ProducesReport()
{
    // Arrange - create test files
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(tempDir);
    File.WriteAllText(Path.Combine(tempDir, "test.cs"), "class Test { }");
    
    var manager = new AnalyzerManager(new[] { new LongMethodRule() });
    var engine = new AnalyzerEngine(manager);
    var reporter = new ReportGenerator();
    var command = new ScanCommand(engine, reporter);
    
    // Act
    await command.ExecuteAsync(new[] { tempDir });
    
    // Assert - should complete without errors
    
    // Cleanup
    Directory.Delete(tempDir, true);
}
```

**Why**: Validates FileSystemCodeProvider works end-to-end.

---

## Verification Checklist

After implementing, verify:

- [ ] AnalyzerEngine project has zero `using System.IO` statements
- [ ] AnalyzerEngine.Analyze() signature changed from `string` to `IEnumerable<SyntaxTree>`
- [ ] FileSystemCodeProvider exists in CLI project, not AnalyzerEngine
- [ ] In-memory tests pass without creating any files
- [ ] Existing CLI integration tests still pass
- [ ] ScanCommand still works identically from user perspective
- [ ] Constitution Check passes (all boxes checked in plan.md)

---

## Testing Strategy

### Unit Tests (AnalyzerEngine.Tests)
- ✅ In-memory syntax trees only
- ✅ No file system operations
- ✅ Fast (<100ms total)
- ✅ Isolated (no shared state)

### Integration Tests (Cli.Tests)
- ✅ FileSystemCodeProvider with real temp files
- ✅ ScanCommand end-to-end
- ✅ Cleanup temp files after tests

### Manual Testing
```bash
# Build project
dotnet build

# Run analysis on Lintelligent itself
dotnet run --project src/Lintelligent.Cli -- ./src

# Should produce same results as before refactor
```

---

## Common Pitfalls to Avoid

### ❌ Don't: Keep IO in AnalyzerEngine
```csharp
// WRONG - violates constitutional boundary
public class AnalyzerEngine
{
    public IEnumerable<DiagnosticResult> Analyze(ICodeProvider provider)
    {
        var trees = provider.GetSyntaxTrees(); // Engine knows about provider!
        // ...
    }
}
```

### ✅ Do: AnalyzerEngine only knows about SyntaxTrees
```csharp
// CORRECT - engine decoupled from provider
public class AnalyzerEngine
{
    public IEnumerable<DiagnosticResult> Analyze(IEnumerable<SyntaxTree> syntaxTrees)
    {
        // Engine doesn't know WHERE trees came from
    }
}
```

---

### ❌ Don't: Throw exceptions for file read errors in provider
```csharp
// WRONG - failing on one file breaks entire analysis
public IEnumerable<SyntaxTree> GetSyntaxTrees()
{
    foreach (var file in files)
    {
        var source = File.ReadAllText(file); // Throws on error!
        yield return CSharpSyntaxTree.ParseText(source, path: file);
    }
}
```

### ✅ Do: Log and skip problematic files
```csharp
// CORRECT - graceful degradation
public IEnumerable<SyntaxTree> GetSyntaxTrees()
{
    foreach (var file in files)
    {
        try
        {
            var source = File.ReadAllText(file);
            yield return CSharpSyntaxTree.ParseText(source, path: file);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Skipping {file}: {ex.Message}");
            continue; // Skip this file, continue with others
        }
    }
}
```

---

### ❌ Don't: Materialize entire tree collection
```csharp
// WRONG - loads all files into memory
public IEnumerable<DiagnosticResult> Analyze(IEnumerable<SyntaxTree> syntaxTrees)
{
    var allTrees = syntaxTrees.ToList(); // BAD! Defeats lazy evaluation
    foreach (var tree in allTrees)
    {
        // ...
    }
}
```

### ✅ Do: Stream trees lazily
```csharp
// CORRECT - processes trees one at a time
public IEnumerable<DiagnosticResult> Analyze(IEnumerable<SyntaxTree> syntaxTrees)
{
    foreach (var tree in syntaxTrees) // Lazy enumeration
    {
        foreach (var result in _manager.Analyze(tree))
        {
            yield return result; // Stream results
        }
    }
}
```

---

## Success Criteria (From Spec)

When complete, verify:

1. **SC-001**: AnalyzerEngine unit tests run without file IO (0 operations)
2. **SC-002**: Existing CLI tests pass with ±5% performance
3. **SC-003**: AnalyzerEngine code coverage ≥90%
4. **SC-004**: FileSystemCodeProvider handles 10k+ files without memory exhaustion
5. **SC-005**: Mock ICodeProvider implementation proves extensibility
6. **SC-006**: AnalyzerEngine project has zero System.IO dependencies
7. **SC-007**: Same syntax trees always produce same results (determinism)

---

## Next Steps After Implementation

1. Run `/speckit.tasks` to generate task breakdown
2. Implement following TDD: tests first, then implementation
3. Ensure all existing tests continue to pass
4. Add new in-memory test suite
5. Update documentation with new API usage

---

## Questions?

Refer to:
- **[research.md](research.md)** - Technical decisions and alternatives
- **[data-model.md](data-model.md)** - Entity definitions and relationships
- **[contracts/ICodeProvider.cs](contracts/ICodeProvider.cs)** - Interface contract
- **[Constitution](../../.specify/memory/constitution.md)** - Architectural principles

---

**Ready to implement!** Start with tests, then interfaces, then implementations.
