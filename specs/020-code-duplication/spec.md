# Feature Specification: Code Duplication Detection

**Feature Branch**: `020-code-duplication`  
**Created**: 2025-12-25  
**Status**: Draft  
**Input**: User description: "Detect duplicate or highly similar code blocks across multiple files in a solution to identify redundant code for refactoring opportunities, reduce maintenance burden, and improve code reusability."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Exact Code Duplication Detection (Priority: P1)

A developer runs Lintelligent on their solution and receives reports identifying blocks of identical code duplicated across multiple files. The tool highlights the exact file locations, line numbers, and number of duplicated lines, enabling the developer to consolidate the duplicated logic into reusable methods or classes.

**Why this priority**: Exact duplication is the most straightforward case to detect and provides immediate, actionable value. Developers can confidently refactor exact matches without concerns about subtle differences in logic.

**Independent Test**: Can be fully tested by analyzing a solution with intentionally duplicated code blocks and verifying that the tool reports all instances with accurate location information.

**Acceptance Scenarios**:

1. **Given** a solution with two files containing identical 15-line methods, **When** Lintelligent scans the solution with default settings, **Then** it reports one duplication instance listing both file locations with line ranges
2. **Given** a solution with three files containing the same 50-line class implementation, **When** Lintelligent scans the solution, **Then** it identifies all three instances and reports them as a duplication group
3. **Given** a solution with 8-line duplicated blocks, **When** Lintelligent scans with minimum threshold of 10 lines, **Then** no duplication is reported
4. **Given** a solution with whitespace-only differences in duplicated code, **When** Lintelligent scans the solution, **Then** the blocks are still identified as exact duplicates (whitespace normalized)

---

### User Story 2 - Configurable Duplication Thresholds (Priority: P2)

A developer configures minimum duplication thresholds to filter out trivial duplications (e.g., short getters/setters) and focus on significant code blocks. They can specify minimum line count, minimum token count, or both to tailor the analysis to their codebase characteristics.

**Why this priority**: Different projects have different tolerance levels for duplication. Configuration enables developers to reduce noise and focus on meaningful refactoring opportunities without being overwhelmed by trivial matches.

**Independent Test**: Can be tested by analyzing a codebase with various duplication sizes and verifying that only blocks meeting the configured thresholds are reported.

**Acceptance Scenarios**:

1. **Given** a solution with duplications ranging from 5 to 50 lines, **When** developer sets `--min-duplication-lines 15`, **Then** only duplications of 15+ lines are reported
2. **Given** a solution with short but token-dense duplications, **When** developer sets `--min-duplication-tokens 100`, **Then** only duplications with 100+ tokens are reported
3. **Given** a configuration file with `minDuplicationLines: 20`, **When** Lintelligent scans the solution, **Then** the configured threshold is applied without requiring CLI flags
4. **Given** both CLI flag `--min-duplication-lines 10` and config file setting `minDuplicationLines: 20`, **When** Lintelligent runs, **Then** the CLI flag takes precedence (10 lines)

---

### User Story 3 - Solution-Wide Multi-Project Analysis (Priority: P1)

A developer analyzes an entire Visual Studio solution containing multiple projects. Lintelligent discovers duplications across project boundaries, identifying cases where similar logic exists in different layers or modules that could be consolidated into shared libraries.

**Why this priority**: Modern .NET solutions often have multiple projects, and duplication across project boundaries represents significant refactoring opportunities. This is a core value proposition that differentiates workspace-level analysis from single-file tools.

**Independent Test**: Can be tested by creating a multi-project solution with intentional cross-project duplications and verifying that all instances are discovered regardless of project boundaries.

**Acceptance Scenarios**:

1. **Given** a solution with 5 projects, **When** Lintelligent scans the solution file, **Then** all projects are analyzed and cross-project duplications are reported
2. **Given** identical utility methods in ProjectA and ProjectB, **When** analysis completes, **Then** the report indicates the duplication exists across different projects
3. **Given** a solution with conditional compilation symbols, **When** Lintelligent evaluates projects, **Then** duplication detection respects the compilation context (code excluded by #if is not analyzed)
4. **Given** a solution with project dependencies, **When** analysis completes, **Then** duplications are reported with awareness of the dependency graph (e.g., noting if duplicated code exists in a dependent and dependency)

---

### User Story 4 - Structural Similarity Detection (Priority: P3)

A developer enables structural similarity detection to identify code blocks that perform the same logic with minor variations (different variable names, reordered statements, or equivalent expressions). The tool reports similarity percentages and highlights the structural similarities, enabling the developer to refactor near-duplicates into parameterized methods.

**Why this priority**: While highly valuable, structural similarity is more complex to implement and interpret. It should come after exact duplication detection is stable. It provides advanced refactoring insights but with a higher risk of false positives.

**Independent Test**: Can be tested by creating code blocks with identical structure but different identifiers, and verifying that similarity percentages are accurately calculated and reported.

**Acceptance Scenarios**:

1. **Given** two methods with identical control flow but different variable names, **When** structural similarity analysis runs, **Then** they are reported as 95%+ similar with locations
2. **Given** a method duplicated with statements reordered but semantically equivalent, **When** analysis runs with AST normalization, **Then** the blocks are identified as highly similar (80%+ similarity)
3. **Given** two blocks with similar structure but different literals, **When** similarity threshold is set to 85%, **Then** only blocks meeting the threshold are reported
4. **Given** structurally similar code in different language constructs (e.g., for vs foreach), **When** AST-based analysis runs, **Then** the structural equivalence is recognized

---

### User Story 5 - Detailed Duplication Reports (Priority: P2)

A developer receives comprehensive duplication reports in multiple formats (console, JSON, markdown) showing grouped duplication instances, similarity metrics, file locations, line ranges, and the number of occurrences. The reports enable prioritization of refactoring efforts based on duplication severity and frequency.

**Why this priority**: Effective reporting is essential for actionable insights, but can be developed incrementally after core detection works. The reporting format builds on existing Lintelligent report infrastructure.

**Independent Test**: Can be tested by analyzing a known codebase and verifying that all output formats contain complete and accurate duplication information.

**Acceptance Scenarios**:

1. **Given** 10 duplication instances detected, **When** console format is used, **Then** duplications are displayed grouped by similarity with file paths and line numbers
2. **Given** duplication analysis results, **When** JSON format is requested, **Then** the output includes structured data with duplication groups, file locations, line ranges, token counts, and similarity percentages
3. **Given** duplication analysis results, **When** markdown format is requested, **Then** the report includes collapsible sections for each duplication group with code snippets (first 10 lines of each block)
4. **Given** 50 duplication groups, **When** report is generated, **Then** groups are sorted by severity (most duplicated code first, based on: occurrences × line count)

---

### Edge Cases

- What happens when a file contains multiple independent duplications (e.g., two different 20-line blocks duplicated elsewhere)?
- How does the system handle partial overlaps (e.g., lines 10-30 duplicate File A, lines 20-40 duplicate File B)?
- What happens when generated code (e.g., designer files, auto-generated POCOs) contains duplications?
- How does analysis handle large solutions (100+ projects, 1M+ LOC) without excessive memory consumption?
- What happens when two files are identical in their entirety (100% duplication)?
- How does the system distinguish between legitimate patterns (e.g., multiple similar test setups) versus code smells?
- What happens when duplication exists only within comments or XML documentation?
- How does the tool handle incremental analysis (detecting only new duplications introduced in changed files)?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST introduce an `IWorkspaceAnalyzer` abstraction separate from `IAnalyzerRule` to enable multi-file analysis
- **FR-002**: System MUST integrate workspace analyzers with the existing `AnalyzerEngine` execution pipeline without breaking single-file rule analysis
- **FR-003**: System MUST detect exact code duplications using token-based comparison (whitespace and comment variations normalized)
- **FR-004**: System MUST support configurable minimum thresholds for reporting duplications (minimum lines and minimum tokens)
- **FR-005**: System MUST analyze all projects in a solution when a solution file is provided, using Feature 009 infrastructure (`ISolutionProvider`, `IProjectProvider`)
- **FR-006**: System MUST report duplication instances with complete location metadata (file paths, start/end line numbers, start/end positions)
- **FR-007**: System MUST group related duplications together (e.g., same block appearing in 5 files = one group with 5 instances)
- **FR-008**: System MUST calculate and report the number of duplicated tokens for each instance
- **FR-009**: System MUST support CLI flags `--min-duplication-lines <n>` and `--min-duplication-tokens <n>` for threshold configuration
- **FR-010**: System MUST include duplication results in existing output formats (console, JSON, markdown)
- **FR-011**: System MUST respect project compilation context (conditional symbols, target frameworks) when evaluating which code to analyze
- **FR-012**: System MUST use deterministic algorithms (same input always produces same results) in compliance with Constitutional Principle VII
- **FR-013**: System MUST implement workspace analyzers as stateless components (no mutable state, no I/O) in compliance with Constitutional Principle I
- **FR-014**: System SHOULD implement AST-based structural similarity detection as an advanced capability (normalized syntax tree comparison)
- **FR-015**: System SHOULD support similarity percentage thresholds when structural similarity is enabled (default: 85% minimum)
- **FR-016**: System SHOULD provide severity ranking of duplication groups (based on occurrences × line count or similar metric)
- **FR-017**: System SHOULD exclude generated code files from duplication analysis (files with auto-generated headers, .g.cs, .designer.cs patterns)

### Key Entities

- **WorkspaceContext**: Represents the complete compilation context including all syntax trees, project metadata, and dependency relationships. Provides workspace-level analyzers with comprehensive view of the solution.
- **DuplicationInstance**: Represents a single occurrence of duplicated code (file path, line range, token range, source text).
- **DuplicationGroup**: Represents a set of related duplication instances (e.g., same block appearing in multiple files). Contains instances, similarity metric, token count, line count.
- **WorkspaceDiagnosticResult**: Extends or relates to existing `DiagnosticResult` but represents findings from multi-file analysis rather than single-file rules.
- **IWorkspaceAnalyzer**: Abstraction for analyzers that require access to multiple files simultaneously. Contract includes `Id`, `Severity`, `Category`, `Description`, and `Analyze(WorkspaceContext)` method.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can identify exact duplications of 10+ lines across an entire solution in under 30 seconds for typical projects (100k LOC)
- **SC-002**: System accurately detects 100% of exact duplications that meet configured thresholds (validated against manually curated test corpus)
- **SC-003**: Duplication reports include all required metadata (file paths, line ranges, token counts) enabling developers to locate and refactor duplications without additional investigation
- **SC-004**: System analyzes multi-project solutions (10+ projects) and reports cross-project duplications with correct project context
- **SC-005**: Configurable thresholds reduce false positives by 80% compared to fixed thresholds (measured via user feedback or test corpus with known trivial duplications)
- **SC-006**: Workspace analyzer framework integrates with existing analyzer engine without breaking any existing rule functionality (all 219 existing tests continue to pass)
- **SC-007**: Duplication detection completes deterministically (same codebase analyzed twice produces identical results)
- **SC-008**: System handles solutions up to 500k LOC without exceeding 2GB memory consumption

## Dependencies *(mandatory)*

### Required Features

- **Feature 009: Solution & Project File Support** ✅ COMPLETE
  - Provides `ISolutionProvider` for parsing .sln files
  - Provides `IProjectProvider` for evaluating MSBuild projects
  - Provides `Solution` and `Project` models with compilation context
  - Provides access to all source files via `Project.CompileItems`

### Required Infrastructure

- Existing `AnalyzerEngine` and rule execution pipeline (Feature 001-004)
- Existing `DiagnosticResult` model and reporting infrastructure (Feature 005-006)
- Roslyn syntax tree parsing and token stream APIs (existing dependency)

## Assumptions *(mandatory)*

- **A-001**: Exact duplication detection takes priority over structural similarity (delivered in phases: exact first, similarity later)
- **A-002**: Default minimum thresholds are 10 lines OR 50 tokens (either condition triggers reporting)
- **A-003**: Whitespace normalization is sufficient for exact matching (leading/trailing whitespace, blank lines, indentation differences ignored)
- **A-004**: Comment content is excluded from duplication analysis (only code tokens are compared)
- **A-005**: Generated code files are excluded by default but can be included via configuration flag
- **A-006**: Duplication reporting uses existing severity model (default: Warning level for duplications)
- **A-007**: Workspace analyzers run after single-file rules in the analysis pipeline (sequential execution)
- **A-008**: Token-based hashing (Rabin-Karp rolling hash) is appropriate for exact duplication detection at scale
- **A-009**: AST normalization for structural similarity will normalize: identifier names, literal values, statement order (where semantically equivalent)
- **A-010**: Initial implementation focuses on C# language only (VB.NET and F# support can be added later if needed)

## Out of Scope

- **Cross-language duplication detection** (e.g., C# vs VB.NET duplications) - current focus is C# only
- **Semantic equivalence detection** (e.g., recognizing that LINQ and foreach produce same result) - too complex for MVP
- **Automatic refactoring** (generating extract method refactorings) - Feature 011 (Auto-Fix) may address this later
- **Binary/IL-level duplication detection** (analyzing compiled output) - source-level only
- **Real-time duplication detection in IDE** (Feature 014/015 will address IDE integration)
- **Incremental duplication analysis** (caching previous results) - Feature 010 covers incremental analysis broadly
- **Duplication trend analysis over time** (tracking duplication metrics across commits) - Feature 016 (Web Dashboard) may address this
- **Cross-repository duplication** (finding duplications across multiple solutions/repos) - single solution scope only

## Notes

- Constitutional compliance is critical: workspace analyzers must be stateless, deterministic, and perform no I/O
- Performance considerations: large solutions may require streaming/chunking strategies to avoid loading all syntax trees into memory simultaneously
- Integration with existing `AnalyzerEngine` should be minimally invasive - consider separate `WorkspaceAnalyzerEngine` that can be orchestrated alongside existing engine
- Duplication ID format: `LNT-DUP-001` (Code Duplication rule, instance 001 within analysis)
- Consider configuration schema extension: `.lintelligent.json` should support duplication-specific settings (thresholds, exclusions, structural similarity toggle)
