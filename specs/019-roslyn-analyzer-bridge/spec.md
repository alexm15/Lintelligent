# Feature Specification: Roslyn Analyzer Bridge

**Feature Branch**: `019-roslyn-analyzer-bridge`  
**Created**: December 24, 2025  
**Status**: Draft  
**Input**: User description: "Feature 019: Roslyn Analyzer Bridge. Priority: P3. Constitutional Principle: VI. Objective: Run Lintelligent rules as Roslyn analyzers (build-time diagnostics). User Value: Fail builds in Visual Studio/Rider without CLI invocation. Deliverables: Roslyn analyzer adapter for IAnalyzerRule, NuGet package: Lintelligent.Analyzers, MSBuild integration (<PackageReference> auto-enables), EditorConfig support for rule configuration"

## User Scenarios & Testing

### User Story 1 - Build-Time Analysis (Priority: P1)

Developers want code quality issues detected during build (not post-build CLI) so violations are caught immediately in their IDE workflow and can optionally fail builds.

**Why this priority**: Core feature - enables real-time feedback in Visual Studio/Rider. Developers see diagnostics as they type, preventing defects before commit. This is the primary value proposition.

**Independent Test**: Install Lintelligent.Analyzers NuGet package, build project with code violating LNT001 (long method >20 statements), verify build produces diagnostic warning in IDE and build output.

**Acceptance Scenarios**:

1. **Given** a C# project with Lintelligent.Analyzers package referenced, **When** developer builds code violating LNT002 (6 parameters), **Then** build output shows "LNT002: Method 'CreateUser' has 6 parameters (max: 5)" as compiler warning
2. **Given** a project with diagnostic severity set to Error in .editorconfig, **When** developer builds code violating any rule, **Then** build fails with exit code 1
3. **Given** Visual Studio with Roslyn analyzer enabled, **When** developer types code violating LNT007 (empty catch), **Then** IDE shows squiggly underline and tooltip with diagnostic message in real-time

---

### User Story 2 - EditorConfig Rule Configuration (Priority: P2)

Developers want to configure rule severity (Info/Warning/Error/None) per project via .editorconfig so teams can enforce custom code standards without rebuilding the analyzer.

**Why this priority**: Enables team-specific policies. Different projects may treat magic numbers as warnings vs errors. Without configuration, teams would fork the analyzer or suffer one-size-fits-all rules.

**Independent Test**: Set `dotnet_diagnostic.LNT004.severity = error` in .editorconfig, build project with magic number literal, verify build fails with error-level diagnostic.

**Acceptance Scenarios**:

1. **Given** .editorconfig with `dotnet_diagnostic.LNT001.severity = none`, **When** developer builds code with 30-statement method, **Then** no LNT001 diagnostic appears (rule suppressed)
2. **Given** .editorconfig with `dotnet_diagnostic.LNT003.severity = error`, **When** code has nesting depth 4, **Then** build fails with error-level diagnostic
3. **Given** .editorconfig with `dotnet_diagnostic.LNT006.severity = suggestion`, **When** IDE analyzes unused private method, **Then** IDE shows gray fade-out hint (not warning squiggly)

---

### User Story 3 - NuGet Package Distribution (Priority: P1)

Developers want to add Lintelligent analysis to any C# project via standard `<PackageReference>` so setup is trivial (no custom build steps, scripts, or manual installs).

**Why this priority**: Adoption barrier removal. If setup requires more than one line in .csproj, teams won't use it. NuGet is the standard .NET distribution channel - must support it for ecosystem fit.

**Independent Test**: Run `dotnet add package Lintelligent.Analyzers`, build project, verify all 8 rules execute without additional configuration.

**Acceptance Scenarios**:

1. **Given** empty C# project, **When** developer runs `dotnet add package Lintelligent.Analyzers`, **Then** next build automatically runs all Lintelligent rules with zero extra setup
2. **Given** project with package reference, **When** developer opens solution in Visual Studio, **Then** IDE shows Lintelligent diagnostics in Error List window without requiring analyzer installation
3. **Given** CI/CD pipeline running `dotnet build`, **When** Lintelligent.Analyzers package is referenced, **Then** build logs include Lintelligent diagnostics without additional CLI commands

---

### User Story 4 - Diagnostic Location Mapping (Priority: P2)

Developers want IDE navigation to work correctly (F8 to next diagnostic, Ctrl+Click to jump to code) so they can quickly fix issues using standard IDE workflows.

**Why this priority**: Developer productivity multiplier. If diagnostics appear but navigation is broken, developers must manually search for violations. Correct line/column mapping is table stakes for Roslyn analyzers.

**Independent Test**: Build project with LNT005 (god class), double-click diagnostic in Error List, verify IDE jumps to exact class declaration line.

**Acceptance Scenarios**:

1. **Given** code violating LNT002 (long parameter list), **When** diagnostic appears in Error List, **Then** double-clicking navigates IDE to exact method declaration line
2. **Given** multiple diagnostics in one file, **When** developer presses F8 (next error), **Then** cursor jumps to each diagnostic location in correct order
3. **Given** diagnostic with line/column info, **When** displayed in build output, **Then** output format is `File.cs(25,10): warning LNT001: ...` for tool compatibility

---

### User Story 5 - Roslyn Analyzer Metadata (Priority: P3)

Developers want proper analyzer metadata (help links, categories, tags) so IDE features work correctly (tooltips show help URLs, Error List filters by category, suppressions work via attributes).

**Why this priority**: Professional polish. Without metadata, diagnostics look amateur (no help links, can't filter by category). Lower priority because diagnostics still function, but user experience suffers.

**Independent Test**: Hover over diagnostic in IDE, verify tooltip shows help URL linking to rules-documentation.md for that specific rule.

**Acceptance Scenarios**:

1. **Given** LNT004 diagnostic in code, **When** developer hovers over squiggly, **Then** tooltip shows help URL to magic number rule documentation
2. **Given** multiple Lintelligent diagnostics, **When** developer filters Error List by Category, **Then** can filter to show only "Code Smell" or "Documentation" diagnostics
3. **Given** diagnostic on method, **When** developer adds `[SuppressMessage("Lintelligent", "LNT001")]` attribute, **Then** analyzer suppresses diagnostic for that method

---

### Edge Cases

- What happens when project targets multiple frameworks (.NET 8.0 + net472)? Analyzer must run for all TFMs.
- How does analyzer handle generated code (*.g.cs files)? Must skip analysis (inherited from IAnalyzerRule.IsGeneratedCode).
- What if .editorconfig has conflicting severity settings (project-level vs directory-level)? Follow standard EditorConfig precedence (nearest wins).
- How does analyzer perform with 100+ files in solution? Must complete analysis in <10s total for acceptable build performance.
- What if rule throws exception during analysis? Analyzer must catch and log error without crashing build.
- How does analyzer handle partial classes across files? Each part analyzed independently (limitation from syntax-only analysis).
- What if NuGet package is referenced but .NET SDK version is too old (< .NET 6)? Package install succeeds but analyzer may not load - document minimum SDK requirement.

## Requirements

### Functional Requirements

- **FR-001**: System MUST provide a Roslyn DiagnosticAnalyzer adapter that wraps IAnalyzerRule implementations
- **FR-002**: System MUST execute all 8 Lintelligent rules (LNT001-LNT008) during Roslyn analysis pass
- **FR-003**: System MUST convert DiagnosticResult to Roslyn Diagnostic with correct severity, location, and message
- **FR-004**: System MUST package adapter as `Lintelligent.Analyzers` NuGet package with analyzer DLL in analyzers/dotnet/cs directory
- **FR-005**: System MUST automatically register all IAnalyzerRule implementations when package is referenced (no manual registration)
- **FR-006**: System MUST respect .editorconfig `dotnet_diagnostic.[RULE_ID].severity` settings for each rule
- **FR-007**: System MUST provide diagnostic metadata: help link, category, default severity, tags
- **FR-008**: System MUST map DiagnosticResult line numbers to Roslyn Location with correct line/column (1-indexed to 0-indexed conversion)
- **FR-009**: System MUST execute in parallel with other analyzers (no blocking, stateless execution)
- **FR-010**: System MUST skip generated code files automatically (inherited IsGeneratedCode check)
- **FR-011**: NuGet package MUST include dependencies: Lintelligent.AnalyzerEngine and Microsoft.CodeAnalysis.CSharp
- **FR-012**: System MUST expose diagnostic descriptors with unique IDs (LNT001-LNT008) matching existing rule IDs
- **FR-013**: System MUST support suppression via `[SuppressMessage("Lintelligent", "LNT001")]` attributes
- **FR-014**: System MUST log analyzer initialization and errors to MSBuild diagnostic output
- **FR-015**: System MUST work in Visual Studio 2022, Rider 2024+, and VS Code with C# extension

### Key Entities

- **LintelligentDiagnosticAnalyzer**: Roslyn DiagnosticAnalyzer that loads all IAnalyzerRule implementations and executes them during compilation
- **RuleAdapter**: Converts IAnalyzerRule to Roslyn diagnostic descriptors, maps DiagnosticResult to Roslyn Diagnostic
- **DiagnosticDescriptor**: Roslyn metadata for each rule (ID, title, message format, category, severity, help link)
- **EditorConfigSeverity**: Configuration reader that parses .editorconfig for dotnet_diagnostic severity overrides
- **NuGet Package Metadata**: Package identity (id, version), dependencies (AnalyzerEngine, Roslyn APIs), development dependency flag

## Success Criteria

### Measurable Outcomes

- **SC-001**: Developers can install analyzer via `dotnet add package Lintelligent.Analyzers` and build produces diagnostics within 5 seconds (first build after package add)
- **SC-002**: All 8 rules (LNT001-LNT008) execute during Roslyn analysis pass and produce diagnostics visible in IDE Error List
- **SC-003**: Build performance overhead is <10% compared to baseline (analyzer adds <2s to 100-file solution build)
- **SC-004**: EditorConfig severity overrides work for all rules (dotnet_diagnostic.LNT*.severity = none|suggestion|warning|error)
- **SC-005**: Diagnostic locations are accurate to exact line/column (F8 navigation jumps to correct position)
- **SC-006**: 100% of existing IAnalyzerRule tests pass when rules run via Roslyn adapter (no regression)
- **SC-007**: NuGet package installs successfully on .NET 6.0+ SDK without manual configuration
- **SC-008**: Help links in diagnostic metadata navigate to correct rule documentation
- **SC-009**: Analyzer handles exceptions gracefully (logs error, continues analysis, doesn't crash build)
- **SC-010**: Package works in Visual Studio 2022, Rider 2024.3, and VS Code with C# DevKit

## Assumptions

- **A-001**: Target .NET SDK 6.0+ (Roslyn 4.0+ APIs available)
- **A-002**: IAnalyzerRule implementations are stateless and thread-safe (can run in parallel)
- **A-003**: Existing rules use syntax-only analysis (no semantic model) so Roslyn SyntaxNodeAnalysisContext is sufficient
- **A-004**: DiagnosticResult.LineNumber is 1-indexed (Roslyn Location uses 0-indexed LinePosition)
- **A-005**: Rules are packaged in Lintelligent.AnalyzerEngine assembly (separate from CLI project)
- **A-006**: EditorConfig support requires Microsoft.CodeAnalysis.CSharp 4.0+ (built-in analyzer config support)
- **A-007**: NuGet package is published to nuget.org or private feed (not distributed as .nupkg file)
- **A-008**: Developers have basic familiarity with .editorconfig for Roslyn analyzer configuration

## Constraints

- **C-001**: Analyzer MUST NOT reference CLI-specific assemblies (Spectre.Console, command handlers)
- **C-002**: Package MUST be marked as development dependency (`<developmentDependency>true</developmentDependency>`)
- **C-003**: Analyzer assembly MUST be compiled for netstandard2.0 (Roslyn analyzer host compatibility)
- **C-004**: Package MUST include analyzer DLL in `analyzers/dotnet/cs` directory (MSBuild convention)
- **C-005**: Diagnostic IDs MUST match existing rule IDs (LNT001-LNT008) for consistency with CLI output
- **C-006**: Analyzer MUST NOT modify source code (read-only analysis only, no code fixes in MVP)
- **C-007**: Performance MUST NOT degrade for large solutions (>500 files) - analysis must be incremental
- **C-008**: Package version MUST align with AnalyzerEngine version (shared version numbering)

## Dependencies

- **D-001**: Requires Lintelligent.AnalyzerEngine project with IAnalyzerRule implementations (Feature 005)
- **D-002**: Requires Microsoft.CodeAnalysis.CSharp 4.0+ NuGet package (Roslyn API)
- **D-003**: Requires .NET SDK 6.0+ for netstandard2.0 compilation and pack tooling
- **D-004**: Requires NuGet feed (nuget.org or private) for package distribution
- **D-005**: Requires existing rules-documentation.md for help link URLs

## Out of Scope

- **Code Fixes**: Auto-fix suggestions (e.g., "Extract method" quick action) are NOT included in MVP - diagnostics only
- **Custom Thresholds**: EditorConfig configuration for rule parameters (e.g., max statement count) - only severity configuration supported
- **IDE Extension**: Standalone VSIX or Rider plugin - functionality delivered via NuGet package only
- **Analyzer Performance Profiling**: Built-in telemetry for analyzer execution time - teams must use external profiling tools
- **Multi-Language Support**: Analyzer only supports C# (no VB.NET, F#)
- **Legacy Framework**: No support for .NET Framework < 4.7.2 (requires Roslyn 4.0+)
- **Incremental Analysis Optimization**: Basic incremental compilation support only - advanced caching deferred to future
- **Localized Messages**: Diagnostic messages in English only (no i18n in MVP)
