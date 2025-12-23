# Lintelligent Feature Roadmap

**Last Updated**: 2025-12-22  
**Constitution Version**: 1.0.0  
**Status**: Draft

## Overview

This roadmap outlines the planned feature development for Lintelligent, a production-focused .NET CLI static analysis tool. All features MUST comply with the architectural principles defined in [constitution.md](memory/constitution.md).

## Prioritization Methodology

- **P0 (Foundation)**: Core infrastructure required before any user-facing features
- **P1 (MVP)**: Minimum viable product - essential user value
- **P2 (Enhancement)**: Significant value-add, improves usability
- **P3 (Extension)**: Advanced features, integrations, nice-to-have

---

## Phase 0: Foundation (Current State Assessment)

### Current Implementation Status

**Existing Components:**
- ✅ Basic `AnalyzerEngine` with file traversal
- ✅ `IAnalyzerRule` interface contract
- ✅ Sample `LongMethodRule` implementation
- ✅ `ReportGenerator` with markdown output
- ✅ `ScanCommand` CLI orchestration
- ✅ Basic test structure

**Constitutional Compliance Gaps:**
- ⚠️ `AnalyzerEngine` performs IO directly (violates Principle I)
- ⚠️ `IAnalyzerRule` returns single result instead of collection (limits flexibility)
- ⚠️ No severity or category metadata on rules (violates Principle III)
- ⚠️ Missing explicit execution model in CLI entry point
- ⚠️ Incomplete test coverage

---

## Phase 1: Constitutional Alignment (P0)

**Goal**: Bring existing implementation into full compliance with constitution principles.

### Feature 001: Refactor AnalyzerEngine IO Boundary
**Priority**: P0  
**Constitutional Principle**: I (Layered Architecture)

**Objective**: Remove IO operations from `AnalyzerEngine`, introduce abstraction for file system access.

**User Value**: Enables testing with in-memory file systems, IDE integration, and alternative frontends.

**Deliverables**:
- `ICodeProvider` abstraction for file/project discovery
- `FileSystemCodeProvider` implementation (moved to CLI layer)
- Refactored `AnalyzerEngine` accepting `IEnumerable<SyntaxTree>` instead of path
- Updated tests demonstrating in-memory testing

**Spec Location**: `specs/001-io-boundary-refactor/`

---

### Feature 002: Enhanced Rule Contract
**Priority**: P0  
**Constitutional Principle**: III (Rule Implementation Contract)

**Objective**: Align `IAnalyzerRule` with constitutional requirements for metadata and multiple findings.

**User Value**: Enables filtering by severity, categorization of issues, and rules that emit multiple findings per file.

**Deliverables**:
- Updated `IAnalyzerRule` interface with:
  - `string Id { get; }`
  - `Severity Severity { get; }`
  - `string Category { get; }`
  - `IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)`
- Migration of existing `LongMethodRule`
- Rule metadata model (id, severity, category enums)

**Spec Location**: `specs/002-rule-contract-enhancement/`

---

### Feature 003: Explicit CLI Execution Model
**Priority**: P0  
**Constitutional Principle**: IV (Explicit Execution Model)

**Objective**: Implement `CliApplicationBuilder` pattern with explicit build → execute → exit flow.

**User Value**: Predictable, testable CLI behavior without hosting framework overhead.

**Deliverables**:
- `CliApplicationBuilder` class
- Explicit `Program.cs` entry point following constitutional pattern
- Command result model with exit codes
- Removal of any implicit async hosting

**Spec Location**: `specs/003-explicit-execution-model/`

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

## Phase 2: MVP Feature Set (P1)

**Goal**: Deliver essential user value - a usable static analysis tool.

### Feature 005: Core Rule Library
**Priority**: P1  
**Constitutional Principle**: III, VII

**Objective**: Implement essential C# code smell detection rules.

**User Value**: Practical code quality insights for .NET projects.

**Rule Candidates** (each rule is independently specifiable):
- **Long Method** (✅ exists, needs enhancement)
- **Long Parameter List** (>5 parameters)
- **Complex Conditional** (nested if depth >3)
- **Magic Numbers** (hardcoded literals without named constants)
- **God Class** (>500 LOC or >15 methods)
- **Dead Code** (unused private methods, fields)
- **Exception Swallowing** (empty catch blocks)
- **Missing XML Documentation** (public APIs)

**Deliverables**:
- 8 production-ready rules with tests
- Rule documentation (what, why, how to fix)
- Categorization: Code Smell, Design, Maintainability, Documentation

**Spec Location**: `specs/005-core-rule-library/`

---

### Feature 006: Structured Output Formats
**Priority**: P1  
**Constitutional Principle**: VI (Extensibility)

**Objective**: Support JSON, SARIF, and human-readable output formats.

**User Value**: CI/CD integration, IDE tooling, standardized reporting.

**Deliverables**:
- `IReportFormatter` abstraction
- JSON formatter (simple structured output)
- SARIF formatter (Static Analysis Results Interchange Format)
- Enhanced markdown formatter (existing, improved)
- CLI flag: `--format <json|sarif|markdown>` (default: markdown)
- Output to file: `--output <path>` (default: stdout)

**Spec Location**: `specs/006-structured-output-formats/`

---

### Feature 007: Rule Filtering & Configuration
**Priority**: P1  
**Constitutional Principle**: III, VII

**Objective**: Allow users to enable/disable rules and configure severity thresholds.

**User Value**: Tailored analysis for different project contexts and team standards.

**Deliverables**:
- `.lintelligent.json` configuration file schema
- Rule enable/disable by ID or category
- Severity threshold (e.g., only show errors, not warnings)
- CLI overrides: `--rules <id1,id2>`, `--severity <error|warning|info>`
- Deterministic config discovery (explicit path or cwd, no ambient search)

**Spec Location**: `specs/007-rule-filtering-configuration/`

---

### Feature 008: Exit Code Strategy
**Priority**: P1  
**Constitutional Principle**: IV, VI

**Objective**: Define exit codes for CI/CD integration (fail builds on threshold violations).

**User Value**: Automated quality gates in build pipelines.

**Deliverables**:
- Exit code model:
  - `0`: Success (no issues or below threshold)
  - `1`: Analysis completed with issues above threshold
  - `2`: Analysis failed (invalid config, missing files, etc.)
- CLI flag: `--fail-on <error|warning|info>` (default: none, exit 0)
- Documentation for CI integration (Azure Pipelines, GitHub Actions, GitLab CI)

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

### Feature 019: Roslyn Analyzer Bridge
**Priority**: P3  
**Constitutional Principle**: VI

**Objective**: Run Lintelligent rules as Roslyn analyzers (build-time diagnostics).

**User Value**: Fail builds in Visual Studio/Rider without CLI invocation.

**Deliverables**:
- Roslyn analyzer adapter for `IAnalyzerRule`
- NuGet package: `Lintelligent.Analyzers`
- MSBuild integration (`<PackageReference>` auto-enables)
- EditorConfig support for rule configuration

**Spec Location**: `specs/019-roslyn-analyzer-bridge/`

---

### Feature 020: Performance & Scalability
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

**Spec Location**: `specs/020-performance-scalability/`

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

- **1.0.0**: Phase 1 complete (constitutional alignment)
- **1.x.0**: Phase 2 features (MVP enhancements)
- **2.0.0**: Phase 3+ if breaking API changes required

---

## Success Metrics

**Phase 1 (Foundation)**: 
- 100% constitutional compliance
- All existing code tested
- CI pipeline green

**Phase 2 (MVP)**:
- ≥10 production-ready rules
- CI/CD integration documentation
- <10s analysis on typical project (10k LOC)

**Phase 3 (Enhancement)**:
- ≥5 early adopter projects
- Baseline support for legacy codebases
- Auto-fix for ≥50% of rule violations

**Phase 4 (Extension)**:
- IDE plugin with ≥1k downloads
- ≥1 third-party rule pack published
- Performance: 1M LOC in <5 minutes

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
1. Review and approve roadmap
2. Select first feature to spec (recommend: 001-io-boundary-refactor)
3. Run `/speckit.spec` command to begin feature development
