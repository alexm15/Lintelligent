# Lintelligent Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-23

## Active Technologies
- C# / .NET 10.0 + Microsoft.CodeAnalysis.CSharp 4.12.0 (Roslyn), xUnit 2.9.3 (002-rule-contract-enhancement)
- N/A (in-memory analysis) (002-rule-contract-enhancement)
- C# / .NET 10.0 + Microsoft.CodeAnalysis.CSharp 4.12.0 (AnalyzerEngine), Microsoft.Extensions.Hosting 10.0.1 (CLI - to be REMOVED) (003-explicit-cli-execution)
- File system only (read .cs files, output reports to stdout) (003-explicit-cli-execution)
- C# / .NET 10.0 + xUnit 2.9.3, FluentAssertions 6.8.0, Microsoft.CodeAnalysis.CSharp.Workspaces 4.12.0, Coverlet 6.0.4 (004-test-coverage-ci)
- N/A (static analysis tool, no persistent storage) (004-test-coverage-ci)
- C# / .NET 10.0 + Microsoft.CodeAnalysis.CSharp (Roslyn), existing IAnalyzerRule interface, DiagnosticResult class, DiagnosticCategories constants (005-core-rule-library)
- N/A (rules are stateless, no persistence required) (005-core-rule-library)
- C# .NET 10.0 (netstandard2.0 for analyzer assembly - Roslyn host compatibility) + Microsoft.CodeAnalysis.CSharp 4.0+, Microsoft.CodeAnalysis.Analyzers 3.11+, Lintelligent.AnalyzerEngine (existing) (019-roslyn-analyzer-bridge)
- N/A (read-only analysis, no persistence) (019-roslyn-analyzer-bridge)
- C# / .NET 10.0 + Microsoft.CodeAnalysis.Sarif (v4.x), System.Text.Json (built-in) (006-structured-output-formats)
- N/A (in-memory processing, optional file output) (006-structured-output-formats)
- C# 13 / .NET 10.0 + Buildalyzer (MSBuild wrapper), Microsoft.Build (if needed for advanced scenarios), Microsoft.Extensions.Logging.Abstractions (existing) (009-solution-project-support)
- N/A (file-based input only, no persistent storage) (009-solution-project-support)
- C# 12.0 / .NET 8.0 (020-code-duplication)
- N/A (in-memory analysis only, no persistence required) (020-code-duplication)
- C# 12, .NET 10.0 (target framework for implementation), netstandard2.0 (Roslyn analyzer compatibility) + Roslyn SDK (Microsoft.CodeAnalysis 4.12.0), language-ext (detection target, not analyzer dependency) (022-monad-analyzer)
- N/A (stateless rules) (022-monad-analyzer)

- C# / .NET 10.0 + Microsoft.CodeAnalysis.CSharp 4.12.0 (Roslyn APIs), Microsoft.Extensions.DependencyInjection 10.0.1 (CLI layer only) (001-io-boundary-refactor)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# / .NET 10.0

## Code Style

C# / .NET 10.0: Follow standard conventions

## Recent Changes
- 022-monad-analyzer: Added C# 12, .NET 10.0 (target framework for implementation), netstandard2.0 (Roslyn analyzer compatibility) + Roslyn SDK (Microsoft.CodeAnalysis 4.12.0), language-ext (detection target, not analyzer dependency)
- 020-code-duplication: Added C# 12.0 / .NET 8.0
- 009-solution-project-support: Added C# 13 / .NET 10.0 + Buildalyzer (MSBuild wrapper), Microsoft.Build (if needed for advanced scenarios), Microsoft.Extensions.Logging.Abstractions (existing)


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
