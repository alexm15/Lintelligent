# Data Model: Refactor AnalyzerEngine IO Boundary

**Feature**: 001-io-boundary-refactor  
**Date**: 2025-12-23  
**Status**: Complete

## Purpose

Define the entities, interfaces, and relationships for the IO boundary refactor. This model describes the abstractions and data structures without implementation details.

---

## Core Entities

### ICodeProvider

**Purpose**: Abstraction for discovering and providing source code for analysis.

**Responsibilities**:
- Discover code from various sources (file system, memory, IDE buffers, network)
- Parse source text into SyntaxTree objects
- Handle source-specific errors gracefully
- Provide SyntaxTrees in a streaming fashion

**Contract**:
- MUST yield only valid, non-null SyntaxTree objects
- MUST set meaningful FilePath on each SyntaxTree for diagnostic reporting
- MUST handle errors internally (log, skip, but don't propagate to caller)
- MAY filter or transform source code before parsing

**Returns**: `IEnumerable<SyntaxTree>` - Lazy sequence of parsed syntax trees

**State**: Stateless - implementations should be instantiable without DI

**Location**: `Lintelligent.AnalyzerEngine/Abstrations/ICodeProvider.cs`

**Relationship**: Used by CLI layer, not by AnalyzerEngine directly

---

### FileSystemCodeProvider

**Purpose**: Concrete implementation of ICodeProvider that discovers C# files from the file system.

**Responsibilities**:
- Accept root path (directory or single file)
- Recursively discover all .cs files in directory
- Read file contents from disk
- Parse files using Roslyn CSharpSyntaxTree.ParseText
- Handle file system errors (access denied, file not found, encoding issues)
- Log errors without failing entire analysis

**Properties**:
- `string RootPath` - Directory or file path to scan
- `ILogger Logger` (optional) - For logging file discovery/parsing errors

**Behavior**:
- If RootPath is directory: recursively enumerate all .cs files
- If RootPath is file: return single SyntaxTree for that file
- Empty directory: return empty enumerable (no error)
- Invalid path: throw ArgumentException during construction
- File read error: log warning, skip file, continue with next

**State**: Initialized with root path, otherwise stateless

**Location**: `Lintelligent.Cli/Providers/FileSystemCodeProvider.cs`

**Dependencies**: System.IO (file operations), Microsoft.CodeAnalysis.CSharp (parsing)

**Relationship**: Implements ICodeProvider, instantiated by CLI layer

---

### AnalyzerEngine (Refactored)

**Purpose**: Core analysis orchestrator - processes syntax trees through registered rules.

**Responsibilities**:
- Accept enumerable collection of SyntaxTrees
- Pass each tree through AnalyzerManager
- Yield diagnostic results in streaming fashion
- Maintain deterministic behavior

**Dependencies**:
- `AnalyzerManager` - Manages rule execution (unchanged)

**Changed Signature**:
```csharp
// Before: Coupled to file system
public IEnumerable<DiagnosticResult> Analyze(string projectPath)

// After: Decoupled from IO
public IEnumerable<DiagnosticResult> Analyze(IEnumerable<SyntaxTree> syntaxTrees)
```

**Properties**:
- `AnalyzerManager Manager` - Rule manager (existing dependency)

**Behavior**:
- For each syntax tree in input:
  - Pass to AnalyzerManager.Analyze(tree)
  - Yield each diagnostic result
- Empty input: return empty enumerable (no error)
- Invalid tree: assumes provider contract guarantees valid trees

**State**: Stateless except for AnalyzerManager dependency

**Location**: `Lintelligent.AnalyzerEngine/Analysis/AnalyzerEngine.cs`

**Relationship**: Core engine, used by CLI layer

---

### SyntaxTree (Roslyn)

**Purpose**: Roslyn's representation of parsed C# source code.

**Key Properties**:
- `string FilePath` - Original file path (or descriptive identifier for in-memory sources)
- `SyntaxNode Root` - Root of abstract syntax tree
- `TextSpan Span` - Source text span

**Immutability**: Immutable once created

**Creation**: `CSharpSyntaxTree.ParseText(string text, CSharpParseOptions? options, string path)`

**Usage**: Common currency between code providers and analyzer engine

**Location**: Microsoft.CodeAnalysis assembly (Roslyn library)

**Relationship**: Produced by ICodeProvider implementations, consumed by AnalyzerEngine

---

### AnalyzerManager (Unchanged)

**Purpose**: Manages registered analyzer rules and executes them against syntax trees.

**Responsibilities**:
- Store collection of IAnalyzerRule instances
- Execute all rules against a given SyntaxTree
- Aggregate results from all rules

**Properties**:
- `IEnumerable<IAnalyzerRule> Rules` - Registered rules

**Behavior**:
- For each rule, call `rule.Analyze(tree)`
- Aggregate results from all rules
- Return collection of DiagnosticResult

**State**: Stateful - holds rule collection

**Location**: `Lintelligent.AnalyzerEngine/Analysis/AnalyzerManager.cs`

**Relationship**: Dependency of AnalyzerEngine

---

### DiagnosticResult (Unchanged)

**Purpose**: Represents a single finding/issue detected by a rule.

**Properties**:
- `string RuleId` - Identifier of rule that produced this result
- `string Severity` - Error, Warning, Info
- `string Message` - Human-readable description
- `string FilePath` - File where issue was found (from SyntaxTree.FilePath)
- `int Line` - Line number of issue
- `int Column` - Column number of issue

**Immutability**: Should be immutable once created

**Location**: `Lintelligent.AnalyzerEngine/Results/DiagnosticResult.cs`

**Relationship**: Produced by rules via AnalyzerManager, consumed by ReportGenerator

---

## Relationships & Flow

### Data Flow (Analysis Execution)

```
┌─────────────────────────────────────────────────────┐
│ CLI Layer (Composition Root)                        │
│                                                     │
│  ScanCommand                                        │
│    ├─> FileSystemCodeProvider.GetSyntaxTrees()     │
│    │     └─> Directory.EnumerateFiles("*.cs")      │
│    │         └─> File.ReadAllText(path)             │
│    │             └─> CSharpSyntaxTree.ParseText()   │
│    │                 └─> IEnumerable<SyntaxTree>    │
│    │                                                 │
│    └─> AnalyzerEngine.Analyze(syntaxTrees)         │
│          └─> (passes to core layer)                 │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ Core Layer (AnalyzerEngine)                         │
│                                                     │
│  AnalyzerEngine                                     │
│    └─> foreach tree in syntaxTrees:                │
│          ├─> AnalyzerManager.Analyze(tree)          │
│          │     └─> foreach rule in Rules:           │
│          │           └─> rule.Analyze(tree)         │
│          │                 └─> DiagnosticResult?    │
│          │                                           │
│          └─> yield DiagnosticResult                 │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ Reporting Layer                                     │
│                                                     │
│  ReportGenerator                                    │
│    └─> GenerateMarkdown(results)                   │
│          └─> Format DiagnosticResults as text       │
└─────────────────────────────────────────────────────┘
```

### Dependency Graph

```
CLI Layer:
  ScanCommand ──> FileSystemCodeProvider (creates)
  ScanCommand ──> AnalyzerEngine (uses)
  FileSystemCodeProvider ──> ICodeProvider (implements)

Core Layer:
  AnalyzerEngine ──> AnalyzerManager (depends on)
  AnalyzerManager ──> IAnalyzerRule[] (depends on)
  IAnalyzerRule ──> DiagnosticResult (produces)

Abstraction:
  ICodeProvider ──> SyntaxTree (yields)
```

### Layer Boundaries

```
┌─────────────────────────────────────────┐
│          CLI Layer                       │
│  - FileSystemCodeProvider                │
│  - ScanCommand                           │
│  - Bootstrapper (DI configuration)       │
│  - Program (entry point)                 │
│                                          │
│  Dependencies: System.IO, DI framework   │
└─────────────────────────────────────────┘
                   ↓ (uses)
┌─────────────────────────────────────────┐
│     AnalyzerEngine Layer (Core)         │
│  - AnalyzerEngine                        │
│  - AnalyzerManager                       │
│  - ICodeProvider (abstraction only)      │
│  - DiagnosticResult                      │
│                                          │
│  Dependencies: Roslyn only, NO System.IO │
└─────────────────────────────────────────┘
                   ↓ (uses)
┌─────────────────────────────────────────┐
│          Rules Layer                     │
│  - IAnalyzerRule                         │
│  - LongMethodRule                        │
│  - (future rules)                        │
│                                          │
│  Dependencies: Roslyn only               │
└─────────────────────────────────────────┘
```

**Key Boundary**: AnalyzerEngine layer defines ICodeProvider abstraction but does NOT depend on any implementation. CLI layer provides concrete implementations.

---

## Validation Rules

### ICodeProvider Implementations MUST:
1. Yield only valid, non-null SyntaxTree objects
2. Set SyntaxTree.FilePath to meaningful value for diagnostics
3. Handle errors internally (log/skip, don't throw)
4. Use lazy evaluation (yield, don't materialize entire collection)

### AnalyzerEngine MUST:
1. Accept any IEnumerable<SyntaxTree> without knowledge of source
2. Process trees in streaming fashion (yield results)
3. NOT perform any file system operations
4. Produce deterministic results for same input trees

### FileSystemCodeProvider MUST:
1. Support both directory and single file paths
2. Recursively discover .cs files in directories
3. Skip files that fail to read or parse
4. Log errors without failing entire analysis
5. Use Directory.EnumerateFiles for lazy discovery

---

## State Transitions

### FileSystemCodeProvider Lifecycle:
```
[Created] 
   ↓ (constructor accepts RootPath)
[Initialized]
   ↓ (GetSyntaxTrees() called)
[Discovering Files]
   ↓ (enumerate .cs files)
[Reading & Parsing]
   ↓ (for each file, read text, parse to SyntaxTree)
[Yielding Trees]
   ↓ (yield each successfully parsed tree)
[Complete]
```

**Note**: Provider is stateless - can call GetSyntaxTrees() multiple times.

### AnalyzerEngine Analysis Lifecycle:
```
[Idle]
   ↓ (Analyze(trees) called)
[Processing Trees]
   ↓ (for each tree)
   ├─> [Analyzing Tree]
   │      ↓ (pass to AnalyzerManager)
   │   [Running Rules]
   │      ↓ (for each rule)
   │   [Collecting Results]
   │      ↓ (yield diagnostics)
   └─> [Next Tree]
        ↓
[Complete]
```

**Note**: Engine is stateless - can analyze multiple tree collections independently.

---

## Extension Points

### Future ICodeProvider Implementations:

1. **InMemoryCodeProvider** (for tests):
   - Accepts dictionary of `<filePath, sourceCode>`
   - Yields SyntaxTrees from in-memory strings
   - No IO operations

2. **IdeBufferCodeProvider** (for IDE plugins):
   - Reads from IDE's in-memory document buffers
   - Yields trees for open/modified files only
   - Updates in real-time as user types

3. **GitDiffCodeProvider** (for CI/CD):
   - Discovers only changed files in git diff
   - Filters trees by commit range
   - Optimizes analysis for PRs

4. **NetworkCodeProvider** (for remote analysis):
   - Fetches source code from HTTP/API
   - Caches remotely fetched content
   - Supports authentication

**All implementations share ICodeProvider contract** - AnalyzerEngine works identically with any provider.

---

## Performance Characteristics

| Entity | Memory | Time Complexity | Streaming |
|--------|--------|-----------------|-----------|
| ICodeProvider | O(1) | O(n) - n = files | Yes (yield) |
| FileSystemCodeProvider | O(1) per file | O(n) - n = files | Yes (lazy enum) |
| AnalyzerEngine | O(1) per tree | O(n*m) - n = trees, m = rules | Yes (yield) |
| SyntaxTree (Roslyn) | O(k) - k = file size | O(k) parse time | N/A (immutable) |

**Key**: Entire pipeline uses streaming (yield) - total memory usage is O(largest single file), not O(all files).

---

## Invariants

1. **AnalyzerEngine never touches file system** - all IO in CLI layer
2. **Same SyntaxTrees → Same DiagnosticResults** - deterministic analysis
3. **ICodeProvider contract enforced** - only valid trees yielded
4. **Streaming maintained** - no full materialization of tree collection
5. **Layer boundaries respected** - Core doesn't depend on CLI

---

## Data Model Complete

All entities, relationships, and contracts defined. Ready for interface/contract generation.
