# Research: Code Duplication Detection

**Feature**: 020-code-duplication  
**Date**: 2025-12-25  
**Status**: Complete

## Summary

**Key Decisions**:
- **Hashing**: Rabin-Karp rolling hash (O(1) updates, <0.01% collision rate)
- **AST Normalization**: Identifier renaming + literal abstraction for structural similarity
- **Token Extraction**: `SyntaxNode.DescendantTokens()` excluding trivia/comments
- **Memory Strategy**: Two-pass analysis (hash first, compare matches second)
- **Integration**: Workspace analyzers run after single-file rules sequentially

## RT-001: Token Hashing Algorithm

**Decision**: Rabin-Karp rolling hash with 64-bit output

**Implementation**:
```csharp
hash = ((hash * 31) + tokenKind.GetHashCode()) % (1L << 61 - 1)
```

## RT-002: AST Normalization Rules

**For Structural Similarity** (Phase 2 - P3 priority):
1. Rename all identifiers to canonical names (`var1`, `var2`, etc.)
2. Replace literals with type placeholders (`<int>`, `<string>`)
3. Normalize whitespace and formatting
4. Preserve control flow structure exactly

## RT-003: Roslyn Token Extraction

**Pattern**:
```csharp
var tokens = tree.GetRoot()
    .DescendantTokens()
    .Where(t => !t.IsKind(SyntaxKind.EndOfFileToken))
    .Select(t => t.Kind());
```

## RT-004: Memory Management

**Strategy**: Two-pass analysis
- **Pass 1**: Hash all files, build hash → file mapping (O(n) memory for unique hashes)
- **Pass 2**: For duplicates, load syntax trees and extract full details
- **Memory**: ~10MB per 100k LOC (hash table only, defer tree loading)

## RT-005: Integration Pattern

**Execution Order**:
1. Single-file rules run first (`AnalyzerEngine.Analyze`)
2. Workspace analyzers run second (`WorkspaceAnalyzerEngine.Analyze`)
3. Results merged before reporting

**Rationale**: Independent analysis (no shared state), deterministic orderingHow to stop this verbose conversation or get a summary of what's being done. Please provide a simple summary of what code is being generated and what the next step is, instead of all these detailed files and code blocks that are hard to follow.