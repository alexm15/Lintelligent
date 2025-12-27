# Changelog - Lintelligent.Analyzers

All notable changes to the **Lintelligent.Analyzers** NuGet package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.0] - 2025-12-27

### Added
- 

### Changed
- 

### Fixed
- 


### Added

### Changed

### Fixed

---

## [1.2.0] - 2025-12-27

### Added

#### Core Quality Analyzers

- **LNT001**: Long Method Detection - Flags methods exceeding complexity thresholds (default: 50 lines)
- **LNT002**: Magic Number Detection - Identifies unexplained numeric literals in code
- **LNT003**: Deep Nesting Detection - Warns when control flow nesting exceeds healthy depth (default: 4 levels)
- **LNT004**: Complex Conditional Detection - Flags boolean expressions with excessive logical operators
- **LNT005**: God Class Detection - Identifies classes with too many responsibilities (high member count)
- **LNT006**: Long Parameter List Detection - Warns about methods with excessive parameters
- **LNT007**: Primitive Obsession Detection - Detects overuse of primitive types instead of domain objects
- **LNT008**: Empty Catch Block Detection - Flags exception handlers that swallow errors silently

#### Code Duplication Analysis

- **LNT100**: Duplicate Code Detection - Uses token-based similarity analysis to identify copy-pasted code blocks
  - Configurable similarity threshold
  - Ignores trivial duplications (usings, common patterns)
  - Reports exact locations of duplicate blocks

#### Functional Programming Support (language-ext)

- **LNT200**: Nullable to Option Detection - Suggests replacing nullable reference types with `Option<T>` for explicit optionality
- **LNT201**: Exception Throwing Detection - Recommends `Either<Error, T>` or `Try<T>` instead of throwing exceptions
- **LNT202**: Null Check Pattern Detection - Identifies manual null checks that could use Option pattern matching

### Features

- **Roslyn Integration**: Seamless integration with Visual Studio, Rider, and dotnet CLI
- **Real-time Analysis**: Diagnostics appear as you type in supported IDEs
- **Configurable Rules**: All analyzers support .editorconfig customization
- **Zero Dependencies**: Analyzer runs entirely in-process, no external tools required
- **netstandard2.0**: Compatible with .NET Framework 4.7.2+ and all .NET Core/.NET 5+ versions

### Performance

- **Lightweight**: Minimal IDE performance impact
- **Incremental Analysis**: Only re-analyzes changed files
- **Efficient Duplication Detection**: Scales to large codebases without slowdown

### Package Structure

- Main analyzer assembly: `Lintelligent.Analyzers.dll`
- Core rule engine: `Lintelligent.AnalyzerEngine.dll` (bundled)
- Development dependency (doesn't add runtime references to your project)

---

## Package Scope

This changelog documents changes specific to the **Lintelligent.Analyzers** NuGet package (Roslyn analyzer).

For changes to other components:
- **Repository-wide changes**: See `/CHANGELOG.md`
- **Lintelligent.CodeFixes**: See `../Lintelligent.CodeFixes/CHANGELOG.md` (future)
- **Lintelligent.Cli**: See `../Lintelligent.Cli/CHANGELOG.md` (future)
