<!--
SYNC IMPACT REPORT
==================
Version: 0.0.0 → 1.0.0 (Initial constitution ratification)

Modified Principles: N/A (initial creation)
Added Sections:
  - Core Principles (I-VII)
  - Architectural Boundaries
  - Execution Model
  - Testing Philosophy
  - Governance

Templates Status:
  ✅ plan-template.md - Updated constitution check references
  ✅ spec-template.md - Aligned with determinism and testability requirements
  ✅ tasks-template.md - Aligned with architectural boundaries

Follow-up TODOs: None

Bump Rationale: MAJOR version (1.0.0) - Initial constitution establishing
foundational governance and architectural principles.
-->

# Lintelligent Constitution

## Core Principles

### I. Layered Architecture with Strict Boundaries

**MUST enforce separation of concerns:**

- **AnalyzerEngine** (core): Framework-agnostic analysis logic; MUST NOT depend on hosting, logging, IO, or
  configuration files
- **Rules** (domain): Stateless, deterministic implementations; MUST NOT depend on CLI, reporting, configuration, or
  other rules
- **Reporting** (transformation): Pure data transformation; MUST NOT perform IO or depend on CLI
- **CLI** (composition root): ONLY layer permitted to use dependency injection; orchestrates all other layers

**MUST prohibit:**

- DI usage outside the CLI layer
- Rules directly depending on external services, file IO, or mutable state
- AnalyzerEngine depending on hosting frameworks (e.g., Generic Host) or logging abstractions
- Cross-layer dependencies that violate the hierarchy: CLI → Reporting/AnalyzerEngine → Rules

**Rationale**: Layered boundaries ensure testability, portability, and long-term maintainability. The AnalyzerEngine
MUST be usable in CI, IDE plugins, or alternative frontends without requiring the CLI infrastructure.

### II. Dependency Injection Boundaries

**Dependency injection is ONLY permitted in:**

- `Lintelligent.Cli` project (composition root)

**DI MUST NOT be used in:**

- AnalyzerEngine (use constructor injection for explicit dependencies only)
- Rules (MUST be stateless and instantiable without DI)
- Reporting (pure functions or simple constructors)

**Rationale**: DI is a hosting concern. Restricting it to the CLI prevents framework coupling and ensures core logic
remains portable and testable without container infrastructure.

### III. Rule Implementation Contract

**All analyzer rules MUST:**

- Implement `IAnalyzerRule` interface
- Be stateless and deterministic (same input → same output, always)
- NOT perform IO, logging, or access configuration files
- Expose metadata: unique identifier, severity, category
- Support emitting zero or more findings per analyzed target

**Rules MUST NOT:**

- Depend on other rules
- Access global state or singletons
- Perform side effects
- Require runtime configuration (compile-time configuration via constructor is acceptable)

**Rationale**: Rules are the core domain logic. Statelessness ensures parallelizability, determinism ensures
reproducibility, and zero external dependencies ensure testability in isolation.

### IV. Explicit Execution Model

**CLI execution MUST follow:**

1. **Build**: Construct dependency graph, validate configuration
2. **Execute**: Run analysis/command to completion
3. **Exit**: Terminate with appropriate exit code

**MUST prohibit:**

- Background services or long-running processes by default
- Implicit use of `IHost.RunAsync()` or similar patterns for standard CLI operations
- Event loops or async workflows that outlive the command execution

**Exceptions**: Future extensions (e.g., watch mode, language server) MAY use background execution when explicitly
designed and documented.

**Rationale**: Explicitness over magic. The CLI is a tool, not a service. Users expect deterministic, finite execution.
Long-running patterns obscure control flow and complicate testing.

### V. Testing Discipline

**MUST ensure:**

- **Rules**: Unit tested in complete isolation without external dependencies
- **AnalyzerEngine**: Testable with in-memory or mock implementations of analyzable targets
- **CLI**: Integration tests focus ONLY on orchestration and command parsing, NOT business logic
- **Reporting**: Pure transformation logic tested with sample inputs/outputs

**DI usage in tests**: Acceptable as a mechanism to swap implementations (e.g., in-memory file system, mock rules) for
CLI integration tests.

**Rationale**: Core logic MUST be testable without spinning up the full application. Tests that require DI indicate
architectural violations (logic leaking into the composition root).

### VI. Extensibility and Stability

**MUST design for:**

- Future rule packs (third-party or internal)
- CI/CD integration (e.g., exit codes, structured output)
- IDE integration (language server protocol, extensions)
- Alternative frontends (GUI, web service, notebook)

**Public APIs MUST:**

- Be boring and stable (avoid clever abstractions)
- Favor explicit contracts over implicit conventions
- Maintain backward compatibility or follow semantic versioning strictly

**Rationale**: Lintelligent is infrastructure tooling. Breaking changes have high user cost. Extensibility ensures the
tool grows with user needs without requiring architectural rewrites.

### VII. Determinism and Predictability

**MUST ensure:**

- Same codebase analyzed with same rules → same results (reproducibility)
- Analysis results are independent of execution order, environment variables, or machine state
- No implicit configuration discovery (explicit paths or defaults MUST be documented)

**MUST avoid:**

- Non-deterministic random seeding in analysis logic
- Time-based or hardware-dependent analysis outcomes
- Hidden global state or ambient context

**Rationale**: Users trust static analysis tools that produce consistent, explainable results. Non-determinism
undermines trust and complicates debugging.

## Architectural Boundaries

**Directory structure (MUST NOT violate):**

```text
src/
  Lintelligent.AnalyzerEngine/    # Framework-agnostic core (NO DI, NO IO)
  Lintelligent.Cli/               # Composition root (DI allowed ONLY here)
  Lintelligent.Reporting/         # Pure transformation (NO IO, NO DI)

tests/
  Lintelligent.AnalyzerEngine.Tests/  # Unit tests (NO DI required)
  Lintelligent.Cli.Tests/              # Integration tests (DI for orchestration)
```

**Dependency flow (MUST enforce):**

- CLI → AnalyzerEngine, Reporting
- AnalyzerEngine → Rules (interface contracts)
- Rules → (no dependencies outside BCL/.NET standard APIs)
- Reporting → (no dependencies outside data models)

**Violations of these boundaries MUST be rejected in code review.**

## Execution Model

**Standard CLI operation:**

```csharp
// Acceptable: Explicit build → execute → exit
var app = new CliApplicationBuilder(args).Build();
var result = app.Execute();
Environment.Exit(result.ExitCode);
```

**NOT acceptable for standard commands:**

```csharp
// PROHIBITED: Implicit background execution
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(/* ... */)
    .Build();
await host.RunAsync(); // Violates explicit execution model
```

**Rationale**: The latter hides control flow and implies service-like behavior. CLI commands MUST be transparent, finite
operations.

## Testing Philosophy

**Test categories:**

1. **Unit tests**: Rules in isolation (zero dependencies)
2. **Integration tests**: AnalyzerEngine with real or mock file system
3. **End-to-end tests**: Full CLI execution with sample codebases

**Coverage expectations:**

- Rules: 100% logic coverage (no untested code paths)
- AnalyzerEngine: All analysis workflows covered
- CLI: Command parsing and orchestration verified, NOT business logic re-tested

**Test-first discipline**: When implementing new rules or analysis features, tests MUST be written first, approved, and
shown to fail before implementation begins.

## Extensibility

**Future-proofing for:**

- **Rule packs**: Plugin-like loading of rule assemblies
- **CI integration**: Structured JSON output, fail-on-threshold exit codes
- **IDE integration**: Language server for real-time analysis
- **Alternative frontends**: Web dashboard, VS Code extension, JetBrains plugin

**Extension points MUST:**

- Use stable interfaces (e.g., `IAnalyzerRule`, `IReportFormatter`)
- Allow third-party implementations without forking
- Avoid breaking changes to public APIs

## Governance

**Amendment Process:**

1. Proposed changes MUST be documented with rationale
2. Architectural violations MUST be explicitly justified before approval
3. Amendments require version bump per semantic versioning:
    - **MAJOR**: Backward-incompatible governance changes, principle removal/redefinition
    - **MINOR**: New principle added or materially expanded guidance
    - **PATCH**: Clarifications, wording fixes, non-semantic refinements

**Compliance:**

- All pull requests MUST verify constitutional alignment
- Code reviews MUST reject violations of core principles
- Complexity or principle exceptions MUST be documented and justified

**Versioning Policy:**

This constitution follows semantic versioning. Breaking changes to principles require community/team approval and
migration documentation.

**Constitution Supersedes:**

This document supersedes all other development practices, coding guidelines, or architectural decisions. In case of
conflict, the constitution prevails unless formally amended.

**Version**: 1.0.0 | **Ratified**: 2025-12-22 | **Last Amended**: 2025-12-22
