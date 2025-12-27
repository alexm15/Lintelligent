# WorkspaceAnalyzers

Workspace-level analysis components for Lintelligent.AnalyzerEngine.

This directory contains analyzers that operate across multiple files, projects, or the entire solution, enabling advanced diagnostics such as code duplication detection.

## CodeDuplication

The `CodeDuplication/` subfolder implements all logic for detecting code duplication at various granularities:

### Key Components

- **ASTNormalizer.cs**
  - Normalizes C# syntax trees for structural comparison, enabling robust duplication detection across formatting and minor code differences.
- **DuplicationDetector.cs**
  - Orchestrates the overall duplication analysis process, coordinating the extraction and comparison of code blocks.
- **DuplicationGroup.cs**
  - Represents a group of duplicated code instances found across the workspace.
- **DuplicationInstance.cs**
  - Represents a single occurrence of a duplicated code block.
- **ExactDuplicationFinder.cs**
  - Detects exact matches of code blocks (whole-file or sub-block) using token and structure comparison.
- **SimilarityDetector.cs**
  - Identifies similar (but not identical) code blocks using heuristics and similarity metrics.
- **StatementSequence.cs**
  - Models a sequence of statements for analysis and comparison.
- **StatementSequenceExtractor.cs**
  - Extracts candidate statement sequences from syntax trees for duplication analysis.

## Usage

These components are used internally by the analyzer engine to:
- Detect whole-file and sub-block code duplication
- Group and report duplicated code instances
- Support workspace-level diagnostics (e.g., LNT100)

## Extensibility

You can extend or customize duplication detection by modifying or subclassing these components. The architecture supports both exact and fuzzy (similarity-based) duplication analysis.

---

For more details, see the main [AnalyzerEngine README](../README.md).
