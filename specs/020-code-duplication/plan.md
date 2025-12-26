# Implementation Plan: Code Duplication Detection

**Branch**: `020-code-duplication` | **Date**: 2025-12-25 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/020-code-duplication/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement workspace-level code duplication detection to identify exact and structurally similar code blocks across multiple files in a .NET solution. This feature introduces a new `IWorkspaceAnalyzer` abstraction that enables multi-file analysis, complementing the existing single-file `IAnalyzerRule` framework. The implementation will leverage Feature 009's solution and project parsing infrastructure (`ISolutionProvider`, `IProjectProvider`) to analyze all source files in a solution, using token-based hashing (Rabin-Karp) for exact duplication detection and AST normalization for structural similarity detection.

**Primary Deliverables**:
- `IWorkspaceAnalyzer` interface for multi-file analysis
- `WorkspaceAnalyzerEngine` to orchestrate workspace analyzers alongside existing rule-based analysis
- Token-based exact duplication detector with configurable thresholds
- AST-based structural similarity detector (advanced feature)
- Integration with existing reporting infrastructure (console, JSON, markdown output)
- CLI flags for threshold configuration (`--min-duplication-lines`, `--min-duplication-tokens`)

## Technical Context

**Language/Version**: C# 12.0 / .NET 8.0  
**Primary Dependencies**: 
- Roslyn (Microsoft.CodeAnalysis.CSharp 4.8.0+) - syntax tree parsing and token stream APIs
- Buildalyzer 6.0.2+ (via Feature 009) - MSBuild project evaluation
- System.CommandLine 2.0.0-beta4.22272.1 - CLI parsing
- xUnit 2.6.6 - testing framework

**Storage**: N/A (in-memory analysis only, no persistence required)  

**Testing**: xUnit with FluentAssertions for all unit/integration tests  

**Target Platform**: Cross-platform (Windows, Linux, macOS) .NET 8.0 runtime  

**Project Type**: Single .NET solution with multiple class library projects (AnalyzerEngine, CLI, Reporting)  

**Performance Goals**: 
- Analyze 100k LOC solution in <30 seconds
- Exact duplication detection: O(n) time complexity where n = total tokens across all files
- Structural similarity: O(n²) worst case for AST comparison, mitigated by pre-filtering with token hashing
- Memory: <2GB for 500k LOC solutions

**Constraints**: 
- Must maintain deterministic analysis (same codebase → same results)
- Must remain stateless (no mutable state in analyzers)
- Must integrate without breaking 219 existing tests
- Must respect constitutional layering (no I/O in AnalyzerEngine, DI only in CLI)

**Scale/Scope**: 
- Support solutions up to 100 projects, 500k LOC
- Handle duplication groups with 10+ instances efficiently
- Report duplications with complete location metadata (file, line, position)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Layered Architecture**: ✅ YES - Feature respects layer boundaries:
  - `IWorkspaceAnalyzer` and implementations belong in `Lintelligent.AnalyzerEngine` (core)
  - `WorkspaceAnalyzerEngine` orchestration belongs in `Lintelligent.AnalyzerEngine.Analysis` (core)
  - CLI integration (command flags, orchestration) belongs in `Lintelligent.Cli` (composition root)
  - Duplication reporting extensions belong in `Lintelligent.Reporting` (transformation layer)
  - No cross-layer violations: CLI → AnalyzerEngine → Analyzers (unidirectional dependency flow)

- [x] **DI Boundaries**: ✅ YES - Dependency injection confined to CLI layer only:
  - `IWorkspaceAnalyzer` implementations are instantiated directly (no DI required)
  - `WorkspaceAnalyzerEngine` accepts explicit dependencies via constructor (not DI container)
  - `ScanCommand` (CLI) uses DI to inject workspace analyzer instances
  - Core logic remains testable without DI infrastructure

- [x] **Rule Contracts**: ✅ YES - New abstraction follows constitutional principles:
  - `IWorkspaceAnalyzer` will be stateless (no mutable state)
  - `IWorkspaceAnalyzer` will be deterministic (same workspace → same results)
  - `IWorkspaceAnalyzer.Analyze(trees, context)` returns `IEnumerable<DiagnosticResult>` (reuses existing type)
  - Implementations perform no I/O (workspace context provided by caller)
  - Metadata contract matches `IAnalyzerRule` (Id, Severity, Category, Description)

- [x] **Explicit Execution**: ✅ YES - CLI follows build → execute → exit model:
  - No background services introduced
  - Duplication analysis runs synchronously as part of `ScanCommand.ExecuteAsync()`
  - Analysis completes before returning control to CLI executor
  - Exit codes remain deterministic based on analysis results

- [x] **Testing Discipline**: ✅ YES - Core logic testable without DI or full application:
  - `IWorkspaceAnalyzer` implementations can be unit tested with in-memory syntax trees
  - `WorkspaceAnalyzerEngine` can be integration tested with mock workspace contexts
  - Duplication algorithms (token hashing, AST comparison) are pure functions testable in isolation
  - CLI integration tests focus on command parsing and orchestration only

- [x] **Determinism**: ✅ YES - Feature produces consistent, reproducible results:
  - Token-based hashing is deterministic (same code → same hash)
  - AST normalization follows defined rules (identifier renaming, literal normalization)
  - No dependency on time, randomness, or environment variables
  - Duplication groups sorted consistently (e.g., by file path, then line number)

- [x] **Extensibility**: ✅ YES - Maintains stable public APIs and avoids breaking changes:
  - New `IWorkspaceAnalyzer` interface does not modify existing `IAnalyzerRule` contract
  - Existing rules and analysis workflows remain unchanged
  - Workspace analyzers return existing `DiagnosticResult` type (no new types, formatters unchanged)
  - Future workspace analyzers can be added without modifying framework
  - CLI flags are additive (no removal or modification of existing flags)

*Violations MUST be documented in Complexity Tracking section with justification.*

**GATE STATUS: ✅ PASSED - All constitutional principles satisfied, no violations to track.**

## Project Structure

### Documentation (this feature)

```text
specs/020-code-duplication/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── IWorkspaceAnalyzer.cs
│   ├── WorkspaceContext.cs
│   └── WorkspaceDiagnosticResult.cs
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Lintelligent.AnalyzerEngine/
│   ├── Abstractions/
│   │   ├── ICodeProvider.cs              # ✅ Existing
│   │   ├── IProjectProvider.cs           # ✅ Existing (Feature 009)
│   │   ├── ISolutionProvider.cs          # ✅ Existing (Feature 009)
│   │   ├── IWorkspaceAnalyzer.cs         # ⏳ NEW - Multi-file analyzer abstraction
│   │   └── Severity.cs                   # ✅ Existing
│   ├── Analysis/
│   │   ├── AnalyzerEngine.cs             # ✅ Existing - Single-file rule orchestrator
│   │   ├── AnalyzerManager.cs            # ✅ Existing
│   │   └── WorkspaceAnalyzerEngine.cs    # ⏳ NEW - Multi-file analyzer orchestrator
│   ├── ProjectModel/
│   │   ├── Solution.cs                   # ✅ Existing (Feature 009)
│   │   ├── Project.cs                    # ✅ Existing (Feature 009)
│   │   ├── CompileItem.cs                # ✅ Existing (Feature 009)
│   │   └── ProjectReference.cs           # ✅ Existing (Feature 009)
│   ├── Results/
│   │   ├── DiagnosticResult.cs           # ✅ Existing
│   │   ├── WorkspaceContext.cs           # ⏳ NEW - Multi-file analysis context
│   │   └── WorkspaceDiagnosticResult.cs  # ⏳ NEW - Multi-file diagnostic result
│   ├── Rules/
│   │   ├── IAnalyzerRule.cs              # ✅ Existing - Single-file rule contract
│   │   └── [8 existing rules]            # ✅ Existing (LNT001-LNT008)
│   ├── WorkspaceAnalyzers/               # ⏳ NEW - Workspace analyzer implementations
│   │   ├── CodeDuplication/
│   │   │   ├── DuplicationDetector.cs    # ⏳ NEW - Main duplication analyzer (IWorkspaceAnalyzer)
│   │   │   ├── DuplicationGroup.cs       # ⏳ NEW - Represents grouped duplications
│   │   │   ├── DuplicationInstance.cs    # ⏳ NEW - Single duplication occurrence
│   │   │   ├── ExactDuplicationFinder.cs # ⏳ NEW - Token-based exact matching
│   │   │   └── SimilarityDetector.cs     # ⏳ NEW - AST-based structural similarity (P3)
│   │   └── [future workspace analyzers]
│   └── Utilities/
│       ├── [existing utilities]          # ✅ Existing
│       └── TokenHasher.cs                # ⏳ NEW - Rabin-Karp rolling hash implementation
├── Lintelligent.Cli/
│   ├── Commands/
│   │   └── ScanCommand.cs                # 🔄 MODIFIED - Add workspace analyzer orchestration
│   ├── Bootstrapper.cs                   # 🔄 MODIFIED - Register workspace analyzers
│   └── [other CLI files]                 # ✅ Existing
└── Lintelligent.Reporting/
    ├── ReportGenerator.cs                # 🔄 MODIFIED - Handle workspace diagnostics
    ├── Formatters/
    │   ├── ConsoleFormatter.cs           # 🔄 MODIFIED - Display duplication groups
    │   ├── JsonFormatter.cs              # 🔄 MODIFIED - Serialize duplication metadata
    │   └── MarkdownFormatter.cs          # 🔄 MODIFIED - Format duplication reports
    └── [other reporting files]           # ✅ Existing

tests/
├── Lintelligent.AnalyzerEngine.Tests/
│   ├── WorkspaceAnalyzers/               # ⏳ NEW - Workspace analyzer tests
│   │   ├── CodeDuplication/
│   │   │   ├── DuplicationDetectorTests.cs
│   │   │   ├── ExactDuplicationFinderTests.cs
│   │   │   ├── SimilarityDetectorTests.cs
│   │   │   └── TokenHasherTests.cs
│   │   └── WorkspaceAnalyzerEngineTests.cs
│   └── [existing test files]             # ✅ Existing (219 tests)
├── Lintelligent.Cli.Tests/
│   └── ScanCommandTests.cs               # 🔄 MODIFIED - Add duplication CLI tests
└── Lintelligent.Reporting.Tests/
    └── [formatter tests]                 # 🔄 MODIFIED - Add duplication report tests
```

**Structure Decision**: 

This feature follows the **single .NET solution structure** (Option 1 from template) with multiple class library projects. The key structural decisions:

1. **New Abstraction Layer**: `IWorkspaceAnalyzer` added to `Abstractions/` alongside `IAnalyzerRule`, establishing two analysis paradigms:
   - **Single-file analysis** (`IAnalyzerRule`) - existing 8 rules
   - **Workspace-level analysis** (`IWorkspaceAnalyzer`) - new duplication detection

2. **New Namespace**: `WorkspaceAnalyzers/` directory created under `Lintelligent.AnalyzerEngine` to house multi-file analyzers, with `CodeDuplication/` as first implementation. This mirrors the existing `Rules/` directory structure.

3. **Existing Infrastructure Leveraged**:
   - Feature 009's `ISolutionProvider` and `IProjectProvider` for solution parsing
   - Feature 009's `Solution`, `Project`, `CompileItem` models for workspace context
   - Existing `DiagnosticResult` and reporting infrastructure (extended, not replaced)

4. **Minimal CLI Changes**: `ScanCommand` and `Bootstrapper` modified to orchestrate workspace analyzers after single-file rules, maintaining backward compatibility.

5. **Testing Isolation**: New test namespace `WorkspaceAnalyzers/` mirrors production structure, ensuring workspace analyzer tests are isolated from existing rule tests (preventing test pollution).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations identified.** All constitutional principles satisfied. This section intentionally empty.

---

## Phase 0: Research & Prerequisites

**Goal**: Resolve all technical unknowns and establish implementation foundation.

### Research Topics

**RT-001: Token-Based Hashing Algorithm Selection**
- **Question**: Which rolling hash algorithm provides best performance/collision trade-off for C# code tokens?
- **Investigation**: Compare Rabin-Karp vs. Murmur3 vs. FNV-1a for token stream hashing
- **Decision Criteria**: Low collision rate (<0.01%), O(1) update time for rolling window, minimal memory overhead
- **Outcome**: Document chosen algorithm in `research.md` with performance benchmarks

**RT-002: AST Normalization Rules**
- **Question**: What normalization transformations preserve semantic equivalence for structural similarity?
- **Investigation**: Research Roslyn AST rewriting patterns for:
  - Identifier renaming (variable/parameter names)
  - Literal value abstraction (numbers, strings → placeholders)
  - Statement reordering (commutative operations, independent statements)
  - Control flow equivalence (for → foreach, if-else → switch)
- **Decision Criteria**: Transformations must not change program semantics, must be reversible for debugging
- **Outcome**: Document normalization ruleset in `research.md` with examples

**RT-003: Roslyn Token Stream API**
- **Question**: What's the most efficient way to extract token sequences from SyntaxTree for hashing?
- **Investigation**: Explore `SyntaxTree.GetCompilationUnitRoot().DescendantTokens()` vs. `SyntaxNode.GetText().ToString()` tokenization
- **Decision Criteria**: Must exclude comments, trivia, and whitespace; must preserve token order
- **Outcome**: Code samples in `research.md` demonstrating token extraction patterns

**RT-004: Memory-Efficient Large Solution Analysis**
- **Question**: How to analyze 500k LOC solutions without loading all syntax trees into memory simultaneously?
- **Investigation**: Strategies for streaming analysis:
  - Option A: Two-pass analysis (hash all files, then compare matches)
  - Option B: Incremental hash table (process files one at a time, track duplications)
  - Option C: Chunked processing (analyze N files at a time, merge results)
- **Decision Criteria**: <2GB memory for 500k LOC, <30s analysis time for 100k LOC
- **Outcome**: Selected strategy documented in `research.md` with memory/time trade-offs

**RT-005: Integration with Existing AnalyzerEngine**
- **Question**: Should workspace analyzers run before, after, or in parallel with single-file rules?
- **Investigation**: Analyze dependencies between rule types:
  - Can workspace analysis benefit from single-file rule results?
  - Are there shared computations (e.g., syntax tree caching)?
- **Decision Criteria**: Minimize redundant parsing, maintain deterministic ordering
- **Outcome**: Execution order and orchestration pattern documented in `research.md`

### Research Deliverables

Create `specs/020-code-duplication/research.md` with:
- **Section 1: Algorithm Selection** - RT-001 and RT-002 findings
- **Section 2: Roslyn API Patterns** - RT-003 code samples and best practices
- **Section 3: Performance Strategy** - RT-004 memory management approach
- **Section 4: Integration Design** - RT-005 orchestration patterns
- **Section 5: Best Practices** - Consolidated recommendations for implementation

---

## Phase 1: Design & Contracts

**Goal**: Define precise interfaces, data models, and API contracts.

### Entity Design

**WorkspaceContext** (immutable data structure)
- Purpose: Provide workspace metadata to analyzers (syntax trees passed separately to Analyze() method)
- Properties:
  - `Solution Solution` - Parsed solution with project metadata (from Feature 009)
  - `IReadOnlyDictionary<string, Project> ProjectsByPath` - Fast project lookup by absolute path
- Design Rationale: Trees passed as separate parameter for clarity and memory efficiency. Context provides only metadata.
- Constraints: Immutable after construction

**DuplicationInstance** (record)
- Purpose: Represent single occurrence of duplicated code
- Properties:
  - `string FilePath` - Absolute path to file containing duplication
  - `string ProjectName` - Project containing this instance (for cross-project awareness)
  - `LinePositionSpan Location` - Start/end line and column positions
  - `int TokenCount` - Number of tokens in duplicated block
  - `ulong Hash` - Rolling hash value for this instance
  - `string SourceText` - Actual duplicated code (for reporting)
- Constraints: Immutable, implements `IEquatable<DuplicationInstance>` for deduplication

**DuplicationGroup** (class)
- Purpose: Represent set of related duplication instances (same code in multiple locations)
- Properties:
  - `ulong Hash` - Shared hash value across all instances
  - `IReadOnlyList<DuplicationInstance> Instances` - All occurrences (minimum 2)
  - `int LineCount` - Number of lines in duplicated block
  - `int TokenCount` - Number of tokens in duplicated block
- Methods:
  - `int GetSeverityScore()` - Calculate priority (instances.Count × LineCount)
  - `IEnumerable<string> GetAffectedProjects()` - List unique projects with duplications
- Constraints: Immutable after construction, sorted instances by (ProjectName, FilePath, Location)
- Note: SimilarityScore property will be added in P3 (US4 - Structural Similarity)

### Diagnostic Integration

**Workspace analyzers return existing `DiagnosticResult` type** (same as single-file rules).

- **Rationale**: Reusing `DiagnosticResult` ensures existing formatters work without modification
- **For multi-location findings** (e.g., duplications spanning files):
  - Primary location: First duplication instance
  - Message: Includes count and affected files ("Code duplicated in 3 files: A.cs, B.cs, C.cs")
  - Diagnostic properties encode duplication metadata via structured message format

### Interface Contracts

**IWorkspaceAnalyzer** (interface)
```csharp
namespace Lintelligent.AnalyzerEngine.Abstractions;

/// <summary>
/// Contract for workspace-level code analysis.
/// Analyzers must be stateless, deterministic, and free of external dependencies.
/// </summary>
public interface IWorkspaceAnalyzer
{
    /// <summary>Unique identifier for the workspace analyzer.</summary>
    string Id { get; }
    
    /// <summary>Human-readable description of what the analyzer checks.</summary>
    string Description { get; }
    
    /// <summary>Severity level of findings produced by this analyzer.</summary>
    Severity Severity { get; }
    
    /// <summary>Category for grouping related analyzers.</summary>
    string Category { get; }
    
    /// <summary>
    /// Analyzes syntax trees and returns zero or more diagnostic findings.
    /// Must be deterministic and not throw exceptions under normal operation.
    /// </summary>
    /// <param name="trees">All syntax trees in the workspace to analyze.</param>
    /// <param name="context">Workspace metadata (solution, projects) for contextual analysis.</param>
    /// <returns>Enumerable of diagnostic results. Never null.</returns>
    IEnumerable<DiagnosticResult> Analyze(
        IReadOnlyList<SyntaxTree> trees,
        WorkspaceContext context);
}
```

**Configuration Extension** (for `.lintelligent.json`)
```json
{
  "duplicationDetection": {
    "enabled": true,
    "minDuplicationLines": 10,
    "minDuplicationTokens": 50,
    "excludeGeneratedCode": true,
    "structuralSimilarity": {
      "enabled": false,
      "minimumSimilarity": 0.85
    },
    "exclusions": [
      "**/*.g.cs",
      "**/*.designer.cs"
    ]
  }
}
```

### Design Deliverables

1. **data-model.md**: Complete entity definitions with examples and constraints
2. **contracts/IWorkspaceAnalyzer.cs**: Full interface with XML documentation
3. **contracts/WorkspaceContext.cs**: Immutable context class implementation
4. **quickstart.md**: How to implement and test a workspace analyzer (with code samples)

### Agent Context Update

Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot` to add:
- `IWorkspaceAnalyzer` interface pattern
- `WorkspaceContext` usage pattern
- Rabin-Karp rolling hash implementation notes
- Integration points with existing `AnalyzerEngine`

---

## Phase 2: Implementation Planning (NOT EXECUTED BY /speckit.plan)

**This phase is executed by `/speckit.tasks` command, which generates `tasks.md`.**

Phase 2 planning will decompose the design from Phase 1 into:
- Granular implementation tasks organized by user story
- Test-first development workflow (write tests → approve → fail → implement)
- Task dependencies and execution order
- Acceptance criteria per task

**Deliverable**: `specs/020-code-duplication/tasks.md` (created by separate command)

---

## Constitutional Re-Check (Post-Design)

*Execute after Phase 1 design completion to verify no constitutional drift occurred during detailed planning.*

- [x] **Layered Architecture**: ✅ Design maintains strict boundaries (AnalyzerEngine core, CLI composition, Reporting transformation)
- [x] **DI Boundaries**: ✅ Workspace analyzers instantiated in CLI only, core logic remains DI-free
- [x] **Rule Contracts**: ✅ `IWorkspaceAnalyzer` mirrors `IAnalyzerRule` principles (stateless, deterministic, no I/O)
- [x] **Explicit Execution**: ✅ No background services, workspace analysis runs synchronously
- [x] **Testing Discipline**: ✅ All components testable with in-memory/mock implementations
- [x] **Determinism**: ✅ Token hashing and AST normalization are deterministic
- [x] **Extensibility**: ✅ New abstraction additive, no breaking changes to existing APIs

**FINAL GATE STATUS: ✅ PASSED - Constitution compliance verified post-design.**

---

## Next Steps

1. ✅ **Specification Complete**: `spec.md` reviewed and approved
2. ✅ **Plan Complete**: This document (`plan.md`) filled with technical context and design
3. ⏳ **Execute Phase 0**: Run research agents to resolve RT-001 through RT-005, generate `research.md`
4. ⏳ **Execute Phase 1**: Create `data-model.md`, `contracts/`, `quickstart.md`, and update agent context
5. ⏳ **Run `/speckit.tasks`**: Generate granular implementation tasks in `tasks.md`
6. ⏳ **Begin Implementation**: TDD workflow starting with P1 user stories (exact duplication, multi-project analysis)
