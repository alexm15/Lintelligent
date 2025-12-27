# Lintelligent.AnalyzerEngine

Core analysis engine for Lintelligent static code analysis tools.

## Overview

This library provides the core abstractions, rule contracts, and analysis logic for all Lintelligent analyzers, CLI, and reporting components. It is designed to be stateless, deterministic, and extensible for C#/.NET code analysis.

## Features

- **Rule Engine:**
  - Implements all core Lintelligent rules (maintainability, complexity, code smell, dead code, documentation, error handling)
  - Supports workspace-level and cross-file analysis
  - Extensible via `IAnalyzerRule` interface
- **Duplication Detection:**
  - Whole-file and sub-block code duplication detection
  - Configurable via `DuplicationOptions`
- **Monad/Functional Diagnostics:**
  - Rules for language-ext monad usage (Option<T>, Either<L,R>, Validation<T>)
- **Abstractions:**
  - `ICodeProvider`, `IProjectProvider`, `ISolutionProvider` for flexible code input
- **Results:**
  - Standardized `DiagnosticResult` and `DiagnosticCategories`
- **Performance:**
  - Streaming/yield-based analysis for large codebases
  - Parallel and incremental analysis support

## Target Frameworks

- .NET 10.0
- .NET Standard 2.0 (compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 6+)

## Key Directories

- `Abstractions/` — Core interfaces for providers and analyzers
- `Analysis/` — Analyzer engine, manager, and orchestration
- `Rules/` — All built-in rule implementations (including monad detection)
- `Results/` — Diagnostic result and category types
- `WorkspaceAnalyzers/CodeDuplication/` — Duplication detection logic
- `ProjectModel/` — Project and solution model abstractions
- `Configuration/` — Options/configs for analysis
- `Utilities/` — Helpers and polyfills

## Usage

This library is not intended for direct use by end-users. It is consumed by:
- Lintelligent.Analyzers (Roslyn analyzer package)
- Lintelligent.Cli (command-line tool)
- Lintelligent.Reporting (output formatting)

To implement a custom rule, inherit from `IAnalyzerRule` and register it with `AnalyzerManager`.

## Development

- Internals are visible to `Lintelligent.Analyzers` and `Lintelligent.AnalyzerEngine.Tests` for extensibility and testing.
- Uses Microsoft.CodeAnalysis.CSharp for syntax tree analysis.

## License

MIT License. See [LICENSE](../../LICENSE) for details.
