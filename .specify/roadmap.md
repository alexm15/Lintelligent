# Lintelligent Feature Roadmap

**Last Updated**: 2025-12-25  
**Constitution Version**: 1.0.0  
**Status**: Active Development

## Overview

This roadmap outlines the planned feature development for Lintelligent, a production-focused .NET CLI static analysis tool. All features MUST comply with the architectural principles defined in [constitution.md](memory/constitution.md).

## Prioritization Methodology

- **P0 (Foundation)**: Core infrastructure required before any user-facing features
- **P1 (MVP)**: Minimum viable product - essential user value
- **P2 (Enhancement)**: Significant value-add, improves usability
- **P3 (Extension)**: Advanced features, integrations, nice-to-have

---

## Phase 0: Foundation (Current State Assessment)

### Current Implementation Status ✅ COMPLETE

**Existing Components:**
- ✅ `AnalyzerEngine` with configurable code providers
- ✅ `IAnalyzerRule` interface with full metadata support
- ✅ 8 production rules implemented (LNT001-LNT008)
- ✅ `ReportGenerator` with markdown, JSON, and console output
- ✅ `ScanCommand` CLI orchestration with DI
- ✅ Comprehensive test suite (186 tests passing)
- ✅ Roslyn analyzer bridge (`Lintelligent.Analyzers`)
- ✅ CI/CD pipeline (GitHub Actions)
- ✅ Enterprise-grade static analyzers integrated (Roslynator, SonarAnalyzer, Meziantou)
- ✅ Centralized build configuration (Directory.Build.props)

**Constitutional Compliance Status:**
- ✅ `AnalyzerEngine` uses `ICodeProvider` abstraction (Principle I)
- ✅ `IAnalyzerRule` returns `IEnumerable<DiagnosticResult>` with full metadata (Principle III)
- ✅ Severity (`Error`, `Warning`, `Info`) and category metadata on all rules (Principle III)
- ✅ Explicit CLI execution model with `Bootstrapper` and DI (Principle IV)
- ✅ 186 unit/integration tests with CI enforcement (Principle V)

---

## Phase 1: Constitutional Alignment (P0) ✅ COMPLETE

**Goal**: Bring existing implementation into full compliance with constitution principles.

### Feature 001: Refactor AnalyzerEngine IO Boundary ✅ COMPLETE
**Priority**: P0  
**Constitutional Principle**: I (Layered Architecture)  
**Status**: ✅ Delivered

**Objective**: Remove IO operations from `AnalyzerEngine`, introduce abstraction for file system access.

**User Value**: Enables testing with in-memory file systems, IDE integration, and alternative frontends.

**Deliverables**:
- ✅ `ICodeProvider` abstraction for file/project discovery
- ✅ `FileSystemCodeProvider` implementation in CLI layer
- ✅ Refactored `AnalyzerEngine` accepting `IEnumerable<SyntaxTree>`
- ✅ `InMemoryCodeProvider` and `FilteringCodeProvider` for testing
- ✅ Updated tests demonstrating in-memory testing

**Spec Location**: `specs/001-io-boundary-refactor/`

---

### Feature 002: Enhanced Rule Contract ✅ COMPLETE
**Priority**: P0  
**Constitutional Principle**: III (Rule Implementation Contract)  
**Status**: ✅ Delivered

**Objective**: Align `IAnalyzerRule` with constitutional requirements for metadata and multiple findings.

**User Value**: Enables filtering by severity, categorization of issues, and rules that emit multiple findings per file.

**Deliverables**:
- ✅ Updated `IAnalyzerRule` interface with:
  - ✅ `string Id { get; }`
  - ✅ `Severity Severity { get; }`
  - ✅ `string Category { get; }`
  - ✅ `string Description { get; }`
  - ✅ `IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)`
- ✅ Migration of all existing rules to new contract
- ✅ `Severity` enum (Error, Warning, Info)
- ✅ `DiagnosticResult` record with full location metadata

**Spec Location**: `specs/002-rule-contract-enhancement/`

---

### Feature 003: Explicit CLI Execution Model ✅ COMPLETE
**Priority**: P0  
**Constitutional Principle**: IV (Explicit Execution Model)  
**Status**: ✅ Delivered

**Objective**: Implement explicit build → execute → exit flow with dependency injection.

**User Value**: Predictable, testable CLI behavior without hosting framework overhead.

**Deliverables**:
- ✅ `Bootstrapper` class with explicit DI container setup
- ✅ Explicit `Program.cs` entry point with `Main` method
- ✅ `ICommand` and `IAsyncCommand` interfaces for commands
- ✅ Command result model with exit codes
- ✅ No implicit async hosting (pure Console app)

**Spec Location**: `specs/003-explicit-execution-model/`

---

### Feature 004: Test Coverage & CI Setup ✅ COMPLETE
**Priority**: P0  
**Constitutional Principle**: V (Testing Discipline)  
**Status**: ✅ Delivered

**Objective**: Achieve constitutional test coverage requirements and automated validation.

**User Value**: Confidence in code quality, regression prevention.

**Deliverables**:
- ✅ Unit tests for all 8 rules (100% coverage)
- ✅ Integration tests for `AnalyzerEngine` workflows
- ✅ CLI orchestration tests
- ✅ GitHub Actions CI pipeline (build, test, restore validation)
- ✅ 186 tests passing consistently
- ✅ Enterprise-grade analyzers enforced (Roslynator, SonarAnalyzer, Meziantou)
- ✅ Centralized build properties (Directory.Build.props)

**Spec Location**: `specs/004-test-coverage-ci/`

---

### Feature 004: Test Coverage & CI Setup
**Priority**: P0  
**Constitutional Principle**: V (Testing Discipline)

**Objective**: Achieve constitutional test coverage requirements and automated validation.

**User Value**: Confidence in code quality, regression prevention.

**Deliverables**:
- Unit tests for all existing rules (100% coverage)
- Integration tests for `AnalyzerEngine` workflows
- CLI orchestration tests (no business logic)
- GitHub Actions CI pipeline (build, test, coverage reporting)
- Coverage enforcement (90%+ threshold)

**Spec Location**: `specs/004-test-coverage-ci/`

---

## Phase 2: MVP Feature Set (P1) 🚧 IN PROGRESS

**Goal**: Deliver essential user value - a usable static analysis tool.

### Feature 005: Core Rule Library ✅ COMPLETE
**Priority**: P1  
**Constitutional Principle**: III, VII  
**Status**: ✅ Delivered

**Objective**: Implement essential C# code smell detection rules.

**User Value**: Practical code quality insights for .NET projects.

**Implemented Rules**:
- ✅ **LNT001: Long Method** (>60 lines)
- ✅ **LNT002: Long Parameter List** (>5 parameters)
- ✅ **LNT003: Complex Conditional** (nested if depth >3)
- ✅ **LNT004: Magic Numbers** (hardcoded literals without named constants)
- ✅ **LNT005: God Class** (>500 LOC or >15 methods)
- ✅ **LNT006: Dead Code** (unused private methods, fields)
- ✅ **LNT007: Exception Swallowing** (empty catch blocks)
- ✅ **LNT008: Missing XML Documentation** (public APIs)

**Deliverables**:
- ✅ 8 production-ready rules with comprehensive tests
- ✅ Full metadata: Id, Severity, Category, Description
- ✅ Categorization: CodeSmell, Design, Maintainability, Documentation

**Spec Location**: `specs/005-core-rule-library/`

---

### Feature 006: Structured Output Formats 🚧 PARTIAL
**Priority**: P1  
**Constitutional Principle**: VI (Extensibility)  
**Status**: 🚧 Partial (JSON, Console, Markdown implemented; SARIF pending)

**Objective**: Support JSON, SARIF, and human-readable output formats.

**User Value**: CI/CD integration, IDE tooling, standardized reporting.

**Deliverables**:
- ✅ `IReportFormatter` abstraction (implied by multiple formatters)
- ✅ JSON formatter (simple structured output)
- ⏳ SARIF formatter (Static Analysis Results Interchange Format) - **PENDING**
- ✅ Markdown formatter (enhanced with file grouping)
- ✅ Console formatter (human-readable terminal output)
- ⏳ CLI flag: `--format <json|sarif|markdown|console>` - **PENDING** (currently hardcoded)
- ⏳ Output to file: `--output <path>` - **PENDING** (currently stdout only)

**Spec Location**: `specs/006-structured-output-formats/`

---

### Feature 007: Rule Filtering & Configuration ⏳ NOT STARTED
**Priority**: P1  
**Constitutional Principle**: III, VII  
**Status**: ⏳ Not Started

**Objective**: Allow users to enable/disable rules and configure severity thresholds.

**User Value**: Tailored analysis for different project contexts and team standards.

**Deliverables**:
- ⏳ `.lintelligent.json` configuration file schema
- ⏳ Rule enable/disable by ID or category
- ⏳ Severity threshold (e.g., only show errors, not warnings)
- ⏳ CLI overrides: `--rules <id1,id2>`, `--severity <error|warning|info>`
- ⏳ Deterministic config discovery (explicit path or cwd, no ambient search)

**Spec Location**: `specs/007-rule-filtering-configuration/`

---

### Feature 008: Exit Code Strategy ⏳ NOT STARTED
**Priority**: P1  
**Constitutional Principle**: IV, VI  
**Status**: ⏳ Not Started

**Objective**: Define exit codes for CI/CD integration (fail builds on threshold violations).

**User Value**: Automated quality gates in build pipelines.

**Deliverables**:
- ⏳ Exit code model:
  - `0`: Success (no issues or below threshold)
  - `1`: Analysis completed with issues above threshold
  - `2`: Analysis failed (invalid config, missing files, etc.)
- ⏳ CLI flag: `--fail-on <error|warning|info>` (default: none, exit 0)
- ⏳ Documentation for CI integration (Azure Pipelines, GitHub Actions, GitLab CI)

**Spec Location**: `specs/008-exit-code-strategy/`

---

## Phase 3: Usability Enhancements (P2)

**Goal**: Improve developer experience and practical adoption.

### Feature 009: Solution & Project File Support
**Priority**: P2  
**Constitutional Principle**: I, VII

**Objective**: Analyze .sln and .csproj files to understand project structure and dependencies.

**User Value**: Accurate analysis respecting compilation context (conditional compilation, linked files).

**Deliverables**:
- Solution/project file parsing (use MSBuild APIs or Buildalyzer)
- Respect `<Compile>` includes/excludes
- Multi-project analysis aggregation
- Dependency graph awareness (future: cross-project rules)

**Spec Location**: `specs/009-solution-project-support/`

---

### Feature 010: Incremental Analysis
**Priority**: P2  
**Constitutional Principle**: VII (Determinism)

**Objective**: Analyze only changed files (cache previous results).

**User Value**: Faster analysis on large codebases, practical for pre-commit hooks.

**Deliverables**:
- Result caching mechanism (`.lintelligent-cache/` folder)
- File hash-based invalidation
- CLI flag: `--incremental` (default: false)
- Clear cache command: `lintelligent cache clear`

**Spec Location**: `specs/010-incremental-analysis/`

---

### Feature 011: Auto-Fix Suggestions
**Priority**: P2  
**Constitutional Principle**: III, VI

**Objective**: Provide automated code fixes for specific rule violations.

**User Value**: Reduce manual effort to address issues, accelerate remediation.

**Deliverables**:
- `ICodeFixProvider` abstraction
- Code fix implementations for safe rules (magic numbers → constants, dead code removal)
- CLI command: `lintelligent fix <rule-id> --file <path>`
- Interactive mode: `lintelligent fix --interactive` (prompt for each fix)
- Dry-run mode: `--dry-run` (show changes without applying)

**Spec Location**: `specs/011-auto-fix-suggestions/`

---

### Feature 012: Baseline & Suppression
**Priority**: P2  
**Constitutional Principle**: VII

**Objective**: Establish baseline of existing issues, suppress false positives.

**User Value**: Adopt tool on legacy codebases without noise, focus on new issues.

**Deliverables**:
- Baseline file generation: `lintelligent baseline --output baseline.json`
- Baseline comparison: `--baseline <file>` (only show new issues)
- Inline suppressions: `// lintelligent-disable-next-line RULE123`
- Suppression file: `.lintelligent-suppressions.json`

**Spec Location**: `specs/012-baseline-suppression/`

---

## Phase 4: Advanced Features & Integrations (P3)

**Goal**: Extend Lintelligent into broader ecosystem and advanced scenarios.

### Feature 013: Custom Rule SDK
**Priority**: P3  
**Constitutional Principle**: VI (Extensibility)

**Objective**: Enable third-party rule development with NuGet package template.

**User Value**: Community-driven rule ecosystem, organization-specific rules.

**Deliverables**:
- NuGet package: `Lintelligent.RuleSdk` (rule base classes, test helpers)
- Rule pack loading mechanism (assembly discovery)
- Template: `dotnet new lintelligent-rule`
- Documentation: "Writing Custom Rules" guide
- Example: `Lintelligent.Rules.Security` (sample third-party pack)

**Spec Location**: `specs/013-custom-rule-sdk/`

---

### Feature 014: IDE Integration (VS Code Extension)
**Priority**: P3  
**Constitutional Principle**: VI

**Objective**: Real-time analysis feedback in Visual Studio Code.

**User Value**: Immediate feedback during coding, no context switching.

**Deliverables**:
- VS Code extension: `lintelligent-vscode`
- Language Server Protocol (LSP) implementation
- Real-time diagnostics (squiggles)
- Quick fix integration
- Extension marketplace publication

**Spec Location**: `specs/014-vscode-integration/`

---

### Feature 015: IDE Integration (JetBrains Rider)
**Priority**: P3  
**Constitutional Principle**: VI

**Objective**: Real-time analysis in JetBrains Rider.

**User Value**: Native Rider experience for Lintelligent rules.

**Deliverables**:
- Rider plugin: `lintelligent-rider`
- IntelliJ Platform inspection integration
- Quick fix integration
- Plugin marketplace publication

**Spec Location**: `specs/015-rider-integration/`

---

### Feature 016: Web Dashboard
**Priority**: P3  
**Constitutional Principle**: VI (Alternative Frontends)

**Objective**: Visualize analysis trends over time, multi-project overview.

**User Value**: Management visibility, trend analysis, team metrics.

**Deliverables**:
- Web frontend (React/Blazor)
- API for result ingestion (ASP.NET Core)
- Trend visualization (issues over time)
- Project comparison
- Export capabilities (PDF, CSV)

**Spec Location**: `specs/016-web-dashboard/`

---

### Feature 017: Git Integration & PR Comments
**Priority**: P3  
**Constitutional Principle**: VI

**Objective**: Post analysis results as GitHub/Azure DevOps PR comments.

**User Value**: Inline feedback during code review, automated quality comments.

**Deliverables**:
- GitHub Action: `lintelligent-action`
- Azure DevOps pipeline task
- PR comment integration (SARIF to inline comments)
- Diff-aware analysis (only comment on changed lines)

**Spec Location**: `specs/017-git-pr-integration/`

---

### Feature 018: Advanced Rules (Architectural)
**Priority**: P3  
**Constitutional Principle**: III, VI

**Objective**: Multi-file, cross-project architectural rules.

**User Value**: Enforce layering, dependency constraints, naming conventions.

**Rule Examples**:
- **Layer Violation Detection** (e.g., data layer depends on UI)
- **Circular Dependency Detection**
- **Namespace Conventions** (e.g., `Company.Product.Layer.Feature`)
- **Test Naming Conventions** (e.g., `MethodName_Scenario_ExpectedResult`)
- **Immutability Enforcement** (DTOs must be records or readonly)

**Deliverables**:
- Multi-file analysis framework (workspace-level context)
- 5 architectural rules with tests
- Performance optimization (parallel analysis)

**Spec Location**: `specs/018-advanced-architectural-rules/`

---

### Feature 019: Roslyn Analyzer Bridge ✅ COMPLETE
**Priority**: P3  
**Constitutional Principle**: VI  
**Status**: ✅ Delivered

**Objective**: Run Lintelligent rules as Roslyn analyzers (build-time diagnostics).

**User Value**: Fail builds in Visual Studio/Rider without CLI invocation.

**Deliverables**:
- ✅ Roslyn analyzer adapter for `IAnalyzerRule` (`LintelligentDiagnosticAnalyzer`)
- ✅ NuGet package: `Lintelligent.Analyzers` (local packaging ready)
- ✅ Automatic rule discovery via reflection
- ✅ EditorConfig severity override support
- ✅ Comprehensive tests (15 analyzer tests passing)
- ✅ SARIF-compatible diagnostic descriptors
- ⏳ MSBuild integration via PackageReference (pending NuGet publishing)

**Note**: Package reference temporarily removed from Directory.Build.props due to CI restore failures (package not yet published to nuget.org). Projects can reference via ProjectReference or local NuGet feed.

**Spec Location**: `specs/019-roslyn-analyzer-bridge/`

---

### Feature 020: Code Duplication Detection
**Priority**: P3  
**Constitutional Principle**: III, VI, VII

**Objective**: Detect duplicate or highly similar code blocks across multiple files in a solution.

**User Value**: Identify redundant code for refactoring opportunities, reduce maintenance burden, improve code reusability.

**Deliverables**:
- ⏳ `IWorkspaceAnalyzer` abstraction for multi-file analysis
- ⏳ Workspace analyzer engine integration with existing `AnalyzerEngine`
- ⏳ Token-based exact duplication detection (Rabin-Karp rolling hash)
- ⏳ AST-based structural similarity detection (normalized syntax tree comparison)
- ⏳ Configurable thresholds (minimum lines, minimum tokens)
- ⏳ CLI flags: `--min-duplication-lines <n>`, `--min-duplication-tokens <n>`
- ⏳ Duplication reports with file locations and similarity percentages
- ⏳ Support for solution-wide analysis using Feature 009 infrastructure

**Dependencies**:
- ✅ Feature 009: Solution & Project File Support (provides multi-file access)

**Spec Location**: `specs/020-code-duplication-detection/`

---

### Feature 021: Performance & Scalability
**Priority**: P3  
**Constitutional Principle**: VII

**Objective**: Optimize for large codebases (1M+ LOC).

**User Value**: Practical analysis on enterprise-scale projects.

**Deliverables**:
- Parallel syntax tree parsing
- Rule parallelization (when deterministic)
- Memory profiling and optimization
- Benchmark suite (track performance regressions)
- Target: <30s for 100k LOC on standard hardware

**Spec Location**: `specs/021-performance-scalability/`

---

### Feature 022: Language-Ext Monad Detection Analyzer
**Priority**: P3  
**Constitutional Principle**: III, VI  
**Status**: ⏳ Not Started

**Objective**: Detect opportunities to use functional monads from the language-ext C# library (opt-in).

**User Value**: Educational suggestions for adopting functional programming patterns, reduce null reference exceptions and improve error handling through type-safe monads.

**Deliverables**:
- ⏳ EditorConfig opt-in setting (`language_ext_monad_detection = true/false`, default: false)
- ⏳ Nullable type → `Option<T>` detection with educational explanations
- ⏳ Try/catch → `Either<L, R>` detection for error-as-value patterns
- ⏳ Sequential validation → `Validation<T>` detection for error accumulation
- ⏳ Detect common language-ext patterns: `Option<T>`, `Either<L, R>`, `Validation<T>`, `Try<T>`
- ⏳ Unique diagnostic IDs (LNT200-LNT203) with Info/Warning severity
- ⏳ Code examples in diagnostics showing before/after transformations
- ⏳ Check for language-ext package reference before reporting
- ⏳ Configurable complexity thresholds to avoid noise
- ⏳ Performance: <10% overhead when enabled

**Dependencies**:
- Feature 019: Roslyn Analyzer Bridge (provides per-file analysis framework)
- Feature 007: Rule Filtering & Configuration (provides EditorConfig integration)

**Spec Location**: `specs/022-monad-analyzer/`

---

## Implementation Strategy

### Feature Development Workflow

Each feature follows the SpecKit methodology:

1. **Spec Creation** (`/speckit.spec`): User scenarios, requirements, edge cases
2. **Planning** (`/speckit.plan`): Technical design, constitution check, structure
3. **Task Breakdown** (`/speckit.tasks`): Implementation tasks organized by user story
4. **Implementation**: TDD (tests first, approved, fail, then implement)
5. **Review**: Constitutional compliance verification

### Constitutional Gates

Before merging any feature:

- ✅ Constitution check passed (in plan.md)
- ✅ Layered architecture preserved
- ✅ DI confined to CLI layer
- ✅ Rules remain stateless and deterministic
- ✅ Tests written first and passing
- ✅ Public APIs stable and documented

### Versioning

Follow semantic versioning:

- **0.9.0**: Current state - Phase 1 complete, Phase 2 in progress
- **1.0.0**: Phase 2 complete (MVP with full output formats, config, exit codes)
- **1.x.0**: Phase 2/3 enhancements (incremental analysis, auto-fix, baseline)
- **2.0.0**: Phase 3+ if breaking API changes required

---

## Success Metrics

**Phase 1 (Foundation)**: ✅ ACHIEVED
- ✅ 100% constitutional compliance
- ✅ All existing code tested (186 tests)
- ✅ CI pipeline green (GitHub Actions)
- ✅ Enterprise-grade analyzers integrated

**Phase 2 (MVP)**: 🚧 IN PROGRESS
- ✅ 8 production-ready rules (target: ≥10) - **80% complete**
- ⏳ CI/CD integration documentation - **PENDING**
- ⏳ <10s analysis on typical project (10k LOC) - **NOT MEASURED**
- 🚧 Full output format support (JSON ✅, SARIF ⏳, Markdown ✅, Console ✅)

**Phase 3 (Enhancement)**: ⏳ NOT STARTED
- ⏳ ≥5 early adopter projects
- ⏳ Baseline support for legacy codebases
- ⏳ Auto-fix for ≥50% of rule violations

**Phase 4 (Extension)**: 🚧 PARTIAL
- ✅ Roslyn analyzer bridge implemented (awaiting NuGet publish)
- ⏳ IDE plugin with ≥1k downloads
- ⏳ ≥1 third-party rule pack published
- ⏳ Performance: 1M LOC in <5 minutes

---

## Current Status Summary (2025-12-25)

**Completed Work**:
- ✅ Full constitutional alignment (Phase 1: 4/4 features)
- ✅ Core rule library (8 rules: LNT001-LNT008)
- ✅ Roslyn analyzer bridge with EditorConfig support
- ✅ Multiple output formatters (JSON, Markdown, Console)
- ✅ Comprehensive test suite (186 tests passing)
- ✅ CI/CD pipeline (GitHub Actions)
- ✅ Centralized build configuration (Directory.Build.props)

**In Progress**:
- 🚧 SARIF output formatter (Feature 006)
- 🚧 CLI format selection (`--format` flag)
- 🚧 File output (`--output` flag)

**Next Priorities**:
1. Complete Feature 006: Structured Output Formats (SARIF + CLI flags)
2. Feature 007: Rule Filtering & Configuration
3. Feature 008: Exit Code Strategy for CI/CD
4. Publish Lintelligent.Analyzers to NuGet
5. Feature 009: Solution & Project File Support

---

## Maintenance & Evolution

**Ongoing Responsibilities**:
- Rule accuracy refinements (reduce false positives)
- .NET version compatibility (new language features)
- Performance optimization
- Documentation updates
- Community engagement (issues, PRs, discussions)

**Constitutional Review**:
- Quarterly review of constitution alignment
- Annual roadmap refresh
- Version bump and amendment process as needed

---

**Next Steps**: 
1. ✅ ~~Review and approve roadmap~~ (Updated 2025-12-25)
2. 🚧 Complete Feature 006: SARIF formatter + CLI flags
3. ⏳ Begin Feature 007: Rule Filtering & Configuration
4. ⏳ Publish Lintelligent.Analyzers NuGet package
5. ⏳ Benchmark and document performance metrics
