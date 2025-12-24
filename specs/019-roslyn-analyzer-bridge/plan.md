# Implementation Plan: Roslyn Analyzer Bridge

**Branch**: `019-roslyn-analyzer-bridge` | **Date**: December 24, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/019-roslyn-analyzer-bridge/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Enable Lintelligent rules to run as Roslyn analyzers for build-time diagnostics in Visual Studio/Rider/VS Code. Developers get real-time code quality feedback without CLI invocation, with configurable severity via EditorConfig. Deliver as NuGet package (`Lintelligent.Analyzers`) with zero-configuration MSBuild integration.

**Technical Approach**: Create a DiagnosticAnalyzer adapter that wraps existing IAnalyzerRule implementations, converting DiagnosticResult to Roslyn Diagnostic format. Package as netstandard2.0 analyzer assembly in standard NuGet structure (analyzers/dotnet/cs directory). Use Roslyn's AnalyzerConfigOptionsProvider for EditorConfig integration.

## Technical Context

**Language/Version**: C# .NET 10.0 (netstandard2.0 for analyzer assembly - Roslyn host compatibility)  
**Primary Dependencies**: Microsoft.CodeAnalysis.CSharp 4.0+, Microsoft.CodeAnalysis.Analyzers 3.11+, Lintelligent.AnalyzerEngine (existing)  
**Storage**: N/A (read-only analysis, no persistence)  
**Testing**: xUnit 2.9.3, FluentAssertions 6.8.0, Microsoft.CodeAnalysis.Testing (Roslyn analyzer test framework)  
**Target Platform**: Multi-IDE (Visual Studio 2022, Rider 2024.3+, VS Code with C# DevKit), .NET SDK 6.0+  
**Project Type**: Single project (Roslyn analyzer library)  
**Performance Goals**: <10% build overhead on 100-file solution (<2s added), <10ms per rule per file  
**Constraints**: netstandard2.0 target (Roslyn analyzer host requirement), no semantic model usage (syntax-only), stateless execution (parallel-safe)  
**Scale/Scope**: 8 rules (LNT001-LNT008), support 1000+ file solutions, cross-IDE compatibility

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Layered Architecture**: ✅ PASS - New Lintelligent.Analyzers project sits at AnalyzerEngine layer (wraps IAnalyzerRule, no CLI dependencies)
- [x] **DI Boundaries**: ✅ PASS - No DI used (DiagnosticAnalyzer instantiated by Roslyn host, rules loaded via reflection)
- [x] **Rule Contracts**: ✅ PASS - Uses existing IAnalyzerRule implementations (LNT001-LNT008), no new rules added
- [x] **Explicit Execution**: ✅ PASS - Analyzer runs in Roslyn compilation pipeline (not CLI-triggered), deterministic build-time execution
- [x] **Testing Discipline**: ✅ PASS - Analyzer testable without DI using Microsoft.CodeAnalysis.Testing framework (in-memory compilation)
- [x] **Determinism**: ✅ PASS - Inherits IAnalyzerRule determinism, same code → same diagnostics (no state, no IO)
- [x] **Extensibility**: ✅ PASS - NuGet package pattern follows Roslyn conventions, maintains IAnalyzerRule contract stability

*Violations MUST be documented in Complexity Tracking section with justification.*

**Initial Assessment**: ✅ ALL CHECKS PASS - No constitutional violations detected

## Project Structure

### Documentation (this feature)

```text
specs/019-roslyn-analyzer-bridge/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── ILintelligentDiagnosticAnalyzer.cs
│   ├── IRuleDescriptorFactory.cs
│   └── DiagnosticDescriptorMetadata.cs
├── checklists/
│   └── requirements.md  # Specification quality validation (completed)
└── spec.md              # Feature specification (completed)
```

### Source Code (repository root)

```text
src/
├── Lintelligent.Analyzers/         # NEW: Roslyn analyzer adapter project
│   ├── Lintelligent.Analyzers.csproj
│   ├── LintelligentDiagnosticAnalyzer.cs      # Main DiagnosticAnalyzer implementation
│   ├── Adapters/
│   │   ├── RuleDescriptorFactory.cs           # IAnalyzerRule → DiagnosticDescriptor
│   │   └── DiagnosticConverter.cs             # DiagnosticResult → Roslyn Diagnostic
│   ├── Metadata/
│   │   ├── DiagnosticCategories.cs            # Category constants (reuse from AnalyzerEngine)
│   │   ├── RuleMetadataProvider.cs            # Help URLs, tags per rule
│   │   └── SeverityMapper.cs                  # Severity enum → DiagnosticSeverity
│   └── Configuration/
│       └── EditorConfigProvider.cs            # Read dotnet_diagnostic.*.severity settings
├── Lintelligent.AnalyzerEngine/    # EXISTING: Core analysis logic (unchanged)
│   ├── Rules/IAnalyzerRule.cs
│   ├── Results/DiagnosticResult.cs
│   └── Rules/ (LNT001-LNT008 implementations)
├── Lintelligent.Cli/               # EXISTING: CLI layer (unchanged)
└── Lintelligent.Reporting/         # EXISTING: Reporting layer (unchanged)

tests/
├── Lintelligent.Analyzers.Tests/   # NEW: Analyzer tests
│   ├── Lintelligent.Analyzers.Tests.csproj
│   ├── Infrastructure/
│   │   └── AnalyzerTestHelper.cs              # Roslyn test infrastructure setup
│   ├── Integration/
│   │   ├── AllRulesIntegrationTests.cs        # All 8 rules execute correctly
│   │   ├── EditorConfigIntegrationTests.cs    # Severity override tests
│   │   └── MultiTargetingTests.cs             # Multi-TFM scenarios
│   └── Unit/
│       ├── RuleDescriptorFactoryTests.cs
│       ├── DiagnosticConverterTests.cs
│       └── SeverityMapperTests.cs
├── Lintelligent.AnalyzerEngine.Tests/  # EXISTING: Rule tests (unchanged)
└── Lintelligent.Cli.Tests/             # EXISTING: CLI tests (unchanged)
```

**Structure Decision**: New project `Lintelligent.Analyzers` added at AnalyzerEngine layer. This project:
- Targets netstandard2.0 (Roslyn analyzer host requirement)
- References Lintelligent.AnalyzerEngine (accesses IAnalyzerRule implementations)
- References Microsoft.CodeAnalysis.CSharp 4.0+ (Roslyn APIs)
- No CLI dependencies (constitutionally compliant layer boundary)
- Packaged as NuGet analyzer (development dependency)

**NuGet Package Structure** (build output):

```text
Lintelligent.Analyzers.nupkg/
├── analyzers/
│   └── dotnet/
│       └── cs/
│           ├── Lintelligent.Analyzers.dll          # Analyzer assembly
│           └── Lintelligent.AnalyzerEngine.dll     # Rule implementations
├── lib/                                             # Empty (analyzer-only package)
└── [package metadata]
    ├── .nuspec (id, version, dependencies, developmentDependency=true)
    └── build/
        └── Lintelligent.Analyzers.props             # Optional: MSBuild integration hooks
```

## Complexity Tracking

**No Constitutional Violations Detected**

All constitutional principles satisfied:

- **Layered Architecture**: Lintelligent.Analyzers sits at AnalyzerEngine layer, wraps IAnalyzerRule (no CLI dependencies) ✅
- **DI Boundaries**: No DI used (Roslyn host instantiates analyzer, rules discovered via reflection) ✅
- **Rule Contracts**: Reuses existing IAnalyzerRule implementations (no new rules) ✅
- **Explicit Execution**: Analyzer runs in Roslyn compilation pipeline (deterministic, build-time) ✅
- **Testing Discipline**: Testable via Microsoft.CodeAnalysis.Testing (no DI required) ✅
- **Determinism**: Inherits IAnalyzerRule determinism (same code → same diagnostics) ✅
- **Extensibility**: Follows Roslyn NuGet conventions, maintains IAnalyzerRule stability ✅

**Complexity Assessment**: LOW - Simple adapter pattern, leverages existing infrastructure, follows Roslyn conventions

---

## Post-Design Constitutional Re-Evaluation

*Re-checked after Phase 1 design completion*

### Final Compliance Check

- [x] **Layered Architecture**: ✅ CONFIRMED - New project at AnalyzerEngine layer, no violations
- [x] **DI Boundaries**: ✅ CONFIRMED - Zero DI usage (Roslyn host manages lifecycle)
- [x] **Rule Contracts**: ✅ CONFIRMED - No changes to existing IAnalyzerRule implementations
- [x] **Explicit Execution**: ✅ CONFIRMED - Roslyn compilation pipeline (finite, deterministic)
- [x] **Testing Discipline**: ✅ CONFIRMED - Analyzer testable without full application spin-up
- [x] **Determinism**: ✅ CONFIRMED - Stateless execution, immutable descriptors, no caching
- [x] **Extensibility**: ✅ CONFIRMED - Standard NuGet analyzer pattern, no breaking changes

### Architecture Validation

**Dependencies** (unidirectional, constitutional):
```
Lintelligent.Analyzers
    ↓ (references)
Lintelligent.AnalyzerEngine
    ↓ (contains)
IAnalyzerRule implementations (LNT001-LNT008)
```

**No reverse dependencies**: AnalyzerEngine does NOT reference Analyzers project ✅

**No CLI coupling**: Analyzers project does NOT reference Lintelligent.Cli ✅

**No DI framework references**: Zero Microsoft.Extensions.DependencyInjection usage ✅

### Design Patterns Compliance

| Pattern | Usage | Constitutional Alignment |
|---------|-------|--------------------------|
| Reflection-based Discovery | DiscoverRules() uses Assembly.GetTypes() | ✅ No DI, explicit discovery |
| Static Initialization | _rules, _descriptors computed once | ✅ Immutable, deterministic |
| Stateless Adapter | DiagnosticConverter, SeverityMapper | ✅ Thread-safe, no side effects |
| Factory Pattern | RuleDescriptorFactory.Create() | ✅ Pure transformation |

**Assessment**: ALL PATTERNS CONSTITUTIONAL ✅

### Risk Analysis

**Identified Risks**: NONE

All design decisions align with constitutional principles:
1. No abstraction violations (stays within layer boundaries)
2. No hidden state (all data immutable or static readonly)
3. No implicit dependencies (reflection explicit, documented)
4. No performance anti-patterns (O(n) execution, no caching needed)

**Final Verdict**: ✅ CONSTITUTIONAL - Ready for implementation

---

## Planning Summary

**Total Planning Artifacts Created**:
1. ✅ plan.md (this file) - Technical context, structure, constitutional validation
2. ✅ research.md - 8 technical decisions with rationale
3. ✅ data-model.md - 5 entities, data flow diagrams, state management
4. ✅ contracts/ - 4 interface contracts (ILintelligentDiagnosticAnalyzer, IRuleDescriptorFactory, IDiagnosticConverter, ISeverityMapper)
5. ✅ quickstart.md - 5-phase implementation guide (2-3 hour estimate)
6. ✅ copilot-instructions.md - Agent context updated with Roslyn technology

**Next Command**: `/speckit.tasks` - Generate detailed task breakdown

**Estimated Implementation Time**: 8-12 hours (setup, adapters, analyzer, testing, packaging)

**Prerequisites Complete**: All research resolved, contracts defined, architecture validated ✅
