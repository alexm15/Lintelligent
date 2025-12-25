# Data Model: Code Duplication Detection

**Feature**: 020-code-duplication  
**Date**: 2025-12-25

## Core Entities

### WorkspaceContext (Immutable)

Provides workspace metadata to analyzers (syntax trees passed separately).

**Properties**:
- `Solution Solution` - Parsed solution with project metadata (from Feature 009)
- `IReadOnlyDictionary<string, Project> ProjectsByPath` - Fast project lookup by absolute path

**Design Rationale**: Trees passed as separate parameter to `Analyze()` for clarity and memory efficiency. Context provides only metadata needed for contextual analysis (project structure, dependencies).

### DuplicationInstance (Record)

Single occurrence of duplicated code.

**Properties**:
- `string FilePath` - Absolute path to file
- `string ProjectName` - Containing project
- `LinePositionSpan Location` - Start/end position
- `int TokenCount` - Number of tokens
- `ulong Hash` - Rolling hash value
- `string SourceText` - Actual code

### DuplicationGroup (Class)

Set of related duplication instances.

**Properties**:
- `ulong Hash` - Shared hash value
- `IReadOnlyList<DuplicationInstance> Instances` - All occurrences (min 2)
- `int LineCount` - Lines in duplicated block
- `int TokenCount` - Tokens in duplicated block

**Methods**:
- `int GetSeverityScore()` → `Instances.Count × LineCount`

### Diagnostic Integration

Workspace analyzers return existing `DiagnosticResult` type (same as single-file rules).

**For multi-location findings** (e.g., duplications spanning multiple files):
- Primary location: First duplication instance
- Message: Includes count and affected files (e.g., "Code duplicated in 3 files: ClassA.cs, ClassB.cs, ClassC.cs")
- Diagnostic properties encode duplication metadata via structured message format

**Rationale**: Reusing `DiagnosticResult` ensures existing formatters (console, JSON, markdown) work without modification.
