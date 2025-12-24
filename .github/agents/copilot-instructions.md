# Lintelligent Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-23

## Active Technologies
- C# / .NET 10.0 + Microsoft.CodeAnalysis.CSharp 4.12.0 (Roslyn), xUnit 2.9.3 (002-rule-contract-enhancement)
- N/A (in-memory analysis) (002-rule-contract-enhancement)
- C# / .NET 10.0 + Microsoft.CodeAnalysis.CSharp 4.12.0 (AnalyzerEngine), Microsoft.Extensions.Hosting 10.0.1 (CLI - to be REMOVED) (003-explicit-cli-execution)
- File system only (read .cs files, output reports to stdout) (003-explicit-cli-execution)
- C# / .NET 10.0 + xUnit 2.9.3, FluentAssertions 6.8.0, Microsoft.CodeAnalysis.CSharp.Workspaces 4.12.0, Coverlet 6.0.4 (004-test-coverage-ci)
- N/A (static analysis tool, no persistent storage) (004-test-coverage-ci)

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
- 004-test-coverage-ci: Added C# / .NET 10.0 + xUnit 2.9.3, FluentAssertions 6.8.0, Microsoft.CodeAnalysis.CSharp.Workspaces 4.12.0, Coverlet 6.0.4
- 003-explicit-cli-execution: Added C# / .NET 10.0 + Microsoft.CodeAnalysis.CSharp 4.12.0 (AnalyzerEngine), Microsoft.Extensions.Hosting 10.0.1 (CLI - to be REMOVED)
- 002-rule-contract-enhancement: Added C# / .NET 10.0 + Microsoft.CodeAnalysis.CSharp 4.12.0 (Roslyn), xUnit 2.9.3


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
