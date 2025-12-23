# Research: Refactor AnalyzerEngine IO Boundary

**Feature**: 001-io-boundary-refactor  
**Date**: 2025-12-23  
**Status**: Complete

## Purpose

Document research findings and design decisions for removing IO operations from AnalyzerEngine and introducing the ICodeProvider abstraction.

## Research Questions & Findings

### 1. Roslyn API for SyntaxTree Creation

**Question**: What Roslyn APIs are available for creating SyntaxTree objects from different sources (files, strings, streams)?

**Finding**:
- **From text**: `CSharpSyntaxTree.ParseText(string text, CSharpParseOptions? options = null, string path = "")` 
  - Accepts source code as string
  - Optional path parameter for diagnostic reporting (doesn't require actual file)
  - Parse options allow specifying language version, preprocessor symbols
- **From file**: Currently used - `File.ReadAllText()` + `ParseText()`
- **From stream**: Can read stream to string first, then use ParseText
- **Lazy parsing**: SyntaxTree creation is already lazy - parsing happens on-demand when tree is accessed

**Decision**: Use `CSharpSyntaxTree.ParseText()` as the common parsing method. ICodeProvider implementations are responsible for reading source text from their respective sources (file system, memory, IDE buffer) and yielding parsed SyntaxTree objects.

**Rationale**: ParseText is the standard Roslyn API, supports all source types, and allows file path tracking for diagnostics without requiring actual files.

---

### 2. IEnumerable vs IAsyncEnumerable for Code Provider

**Question**: Should ICodeProvider return `IEnumerable<SyntaxTree>` or `IAsyncEnumerable<SyntaxTree>` for better async IO support?

**Finding**:
- **IEnumerable**: Synchronous iteration, simple implementation
  - File IO can still be async internally, just yielded synchronously
  - AnalyzerEngine already uses yield return pattern (synchronous)
  - No async/await needed in AnalyzerEngine core
  
- **IAsyncEnumerable**: Asynchronous iteration, better for true async sources
  - Requires `await foreach` in AnalyzerEngine
  - Adds async complexity to core analysis logic
  - File IO is fast enough that async doesn't provide significant benefit
  - Future async sources (network, database) would benefit

**Decision**: Use `IEnumerable<SyntaxTree>` for initial implementation.

**Rationale**: 
- Keeps AnalyzerEngine core simple and synchronous
- File system IO is fast enough for local analysis
- FileSystemCodeProvider can use async IO internally but yield synchronously
- Can evolve to IAsyncEnumerable in future if async sources become common
- Aligns with constitutional principle of simplicity and explicitness

**Alternative Considered**: IAsyncEnumerable rejected because it adds complexity without current need. If future IDE integration requires truly async sources, we can create IAsyncCodeProvider alongside ICodeProvider.

---

### 3. Error Handling Strategy for Invalid/Null SyntaxTrees

**Question**: How should AnalyzerEngine handle null or malformed SyntaxTrees from code providers?

**Finding**:
- **Option 1**: AnalyzerEngine validates each tree, skips invalid ones
  - Adds defensive code to core engine
  - Mixes concerns (analysis + validation)
  
- **Option 2**: ICodeProvider contract guarantees only valid trees
  - Cleaner separation of concerns
  - Provider implementations handle errors at source
  - AnalyzerEngine trusts provider contract
  
- **Option 3**: Allow malformed trees, let rules handle gracefully
  - Rules may not be designed for invalid syntax
  - Could cause analysis failures

**Decision**: ICodeProvider contract MUST yield only valid, non-null SyntaxTree objects. Providers handle errors at the source.

**Rationale**:
- Respects layered architecture - IO/parsing errors belong in CLI layer, not core
- AnalyzerEngine remains focused on analysis, not error handling
- FileSystemCodeProvider logs and skips files that fail to parse
- Enables deterministic behavior - same valid trees always produce same results

**Implementation**: 
- ICodeProvider contract documented: "MUST yield only valid SyntaxTree objects"
- FileSystemCodeProvider catches parsing exceptions, logs error, continues with next file
- AnalyzerEngine assumes all yielded trees are valid

---

### 4. File Path Tracking for Diagnostics

**Question**: How do we track original file paths for diagnostic reporting when syntax trees come from memory?

**Finding**:
- SyntaxTree.FilePath property stores the path used during ParseText
- Can set FilePath to any string - doesn't require actual file to exist
- Diagnostic results use this path for reporting location
- In-memory trees can use descriptive paths like `<memory:test-code>` or empty string

**Decision**: 
- ICodeProvider implementations MUST set meaningful FilePath when creating SyntaxTrees
- FileSystemCodeProvider uses actual file path
- In-memory providers (tests, IDE) use descriptive identifiers
- AnalyzerEngine doesn't modify FilePath - passes through to diagnostics

**Rationale**: Preserves diagnostic accuracy while maintaining flexibility for non-file sources.

---

### 5. Streaming vs Batch Processing

**Question**: Should AnalyzerEngine process syntax trees one at a time (streaming) or collect them into a list first (batch)?

**Finding**:
- **Streaming** (yield pattern):
  - Constant memory usage regardless of project size
  - Results available immediately as files are analyzed
  - Natural fit for IEnumerable<SyntaxTree> input
  - Can't do multi-pass analysis without re-iteration
  
- **Batch** (ToList() first):
  - Memory usage grows with project size
  - Enables multi-pass analysis
  - Can compute cross-file metrics
  - Simpler to parallelize

**Decision**: Streaming (yield pattern) for initial implementation.

**Rationale**:
- Aligns with performance goal: support 10k+ files without memory exhaustion (FR-004)
- Current rules are single-file analysis - no cross-file dependencies
- Can add multi-pass support later if needed for architectural rules
- Constitutional principle: explicitness - streaming makes memory behavior transparent

**Implementation**:
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

---

### 6. Dependency Injection for ICodeProvider

**Question**: Should ICodeProvider be injected into AnalyzerEngine or passed as parameter to Analyze()?

**Finding**:
- **Constructor injection** (AnalyzerEngine depends on ICodeProvider):
  - Provider becomes part of engine's dependencies
  - Violates single responsibility - engine shouldn't know about code discovery
  - Harder to test with different providers
  
- **Method parameter** (IEnumerable<SyntaxTree> passed to Analyze):
  - Engine only depends on syntax trees, not how they're discovered
  - Caller responsible for obtaining trees via provider
  - Perfect separation of concerns
  - Easier to test - just pass in-memory trees

**Decision**: AnalyzerEngine.Analyze() accepts `IEnumerable<SyntaxTree>` directly, NOT ICodeProvider.

**Rationale**:
- Stronger layer boundary - AnalyzerEngine doesn't know code providers exist
- CLI layer uses ICodeProvider to get trees, then passes trees to engine
- Enables testing without any abstraction - just create trees directly
- Constitutional principle: explicit dependencies - engine only needs trees, not discovery mechanism

**Implementation**:
```csharp
// AnalyzerEngine (core) - NO knowledge of ICodeProvider
public IEnumerable<DiagnosticResult> Analyze(IEnumerable<SyntaxTree> syntaxTrees) { }

// ScanCommand (CLI) - uses ICodeProvider
var provider = new FileSystemCodeProvider(path);
var trees = provider.GetSyntaxTrees();
var results = _engine.Analyze(trees);
```

---

### 7. File Discovery Patterns & Performance

**Question**: How should FileSystemCodeProvider discover .cs files efficiently for large projects?

**Finding**:
- `Directory.EnumerateFiles("*.cs", SearchOption.AllDirectories)` - Lazy evaluation
- `Directory.GetFiles("*.cs", SearchOption.AllDirectories)` - Eager evaluation (loads all paths)
- EnumerateFiles is better for streaming - yields paths as discovered
- Can use `EnumerationOptions` (.NET Core 3.0+) for advanced filtering

**Decision**: Use `Directory.EnumerateFiles()` with lazy enumeration.

**Rationale**: Aligns with streaming approach, memory-efficient for large projects.

**Implementation**:
```csharp
public IEnumerable<SyntaxTree> GetSyntaxTrees()
{
    var files = Directory.EnumerateFiles(_rootPath, "*.cs", SearchOption.AllDirectories);
    
    foreach (var filePath in files)
    {
        string source;
        try
        {
            source = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            // Log error, continue with next file
            _logger.LogWarning($"Failed to read {filePath}: {ex.Message}");
            continue;
        }
        
        yield return CSharpSyntaxTree.ParseText(source, path: filePath);
    }
}
```

---

### 8. Testing Best Practices for Refactor

**Question**: What testing strategy ensures refactor doesn't break existing functionality?

**Finding**:
- **Characterization tests**: Capture current behavior before refactor, ensure it's preserved
- **Golden tests**: Store expected outputs for known inputs, compare after refactor
- **In-memory tests**: New capability enabled by refactor - tests without file system
- **Integration tests**: CLI tests ensure FileSystemCodeProvider works end-to-end

**Decision**: Multi-layer testing approach:
1. Add characterization tests for current AnalyzerEngine behavior (before refactor)
2. Implement in-memory unit tests (target architecture)
3. Ensure all existing integration tests pass with new implementation
4. Add FileSystemCodeProvider-specific tests

**Rationale**: Validates both correctness (same results) and new capability (in-memory testing).

---

## Key Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Parsing API | `CSharpSyntaxTree.ParseText()` | Standard Roslyn API, flexible, supports all sources |
| Provider Return Type | `IEnumerable<SyntaxTree>` | Simplicity, synchronous core, can evolve to async later |
| Error Handling | Provider responsibility | Clean separation, AnalyzerEngine stays focused on analysis |
| File Path Tracking | SyntaxTree.FilePath property | Built-in Roslyn feature, works with any source |
| Processing Model | Streaming (yield) | Memory efficient, supports large projects |
| Dependency Injection | Engine accepts trees directly | Stronger boundaries, easier testing |
| File Discovery | `Directory.EnumerateFiles()` | Lazy evaluation, memory efficient |
| Testing Strategy | Multi-layer (characterization + in-memory + integration) | Validates correctness and new capabilities |

---

## Technologies & Patterns Used

**Roslyn APIs**:
- `Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText()` - Syntax tree creation
- `Microsoft.CodeAnalysis.SyntaxTree` - Abstract syntax tree representation
- `Microsoft.CodeAnalysis.CSharpParseOptions` - Language version configuration

**Design Patterns**:
- **Strategy Pattern**: ICodeProvider abstraction with multiple implementations
- **Iterator Pattern**: IEnumerable<T> for lazy evaluation
- **Dependency Inversion**: AnalyzerEngine depends on abstraction (SyntaxTree), not concrete IO

**.NET APIs**:
- `System.IO.Directory.EnumerateFiles()` - File discovery
- `System.IO.File.ReadAllText()` - File reading
- `System.Collections.Generic.IEnumerable<T>` - Streaming iteration

---

## Alternatives Considered & Rejected

### Alternative 1: Keep IO in AnalyzerEngine, Mock File System
**Rejected because**: Violates Constitution Principle I, doesn't truly eliminate IO coupling, mocking file system is complex and brittle.

### Alternative 2: Use Roslyn Workspace APIs
**Rejected because**: Workspace APIs add complexity (project loading, references, compilation), overkill for syntax-only analysis, harder to test.

### Alternative 3: Pass File Paths to AnalyzerEngine, Abstract Reading
**Rejected because**: AnalyzerEngine still knows about files (even if abstracted), doesn't enable in-memory testing, partial solution.

### Alternative 4: IAsyncEnumerable for Provider
**Rejected because**: Adds async complexity without current benefit, can add later if needed, synchronous is simpler and explicit.

---

## Open Questions for Future Iterations

1. **Parallel Processing**: Should AnalyzerEngine support parallel analysis of syntax trees? (Not needed for P0, but valuable for performance)
2. **Caching**: Should parsed SyntaxTrees be cached to avoid re-parsing? (Out of scope for this refactor, consider in performance feature)
3. **Multi-pass Analysis**: Future architectural rules may need multiple passes - how to support without loading all trees in memory?
4. **Progress Reporting**: How should long-running analysis report progress to CLI? (Not in current spec, consider for UX enhancement)

---

## References

- [Roslyn Syntax Tree Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtree)
- [CSharpSyntaxTree.ParseText API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxtree.parsetext)
- Constitution Principle I: Layered Architecture with Strict Boundaries
- Constitution Principle V: Testing Discipline

---

**Research Complete** - All technical unknowns resolved. Ready for Phase 1 design.
