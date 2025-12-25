# Implementation Plan: Structured Output Formats

**Branch**: `006-structured-output-formats` | **Date**: 2025-12-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-structured-output-formats/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add structured output formatters (JSON, SARIF, enhanced Markdown) to enable CI/CD integration, IDE interoperability, and improved developer experience. Implementation will create an `IReportFormatter` abstraction with three concrete formatters, integrate new `--format` and `--output` CLI flags into `ScanCommand`, and validate outputs against published schemas (JSON schema inline, SARIF 2.1.0 official schema).

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: Microsoft.CodeAnalysis.Sarif (v4.x), System.Text.Json (built-in)  
**Storage**: N/A (in-memory processing, optional file output)  
**Testing**: xUnit, FluentAssertions (existing test infrastructure)  
**Target Platform**: Cross-platform CLI (.NET 10.0 runtime)  
**Project Type**: Single solution with layered projects (AnalyzerEngine, Reporting, CLI)  
**Performance Goals**: Format 10,000 violations in under 10 seconds (SC-008)  
**Constraints**: UTF-8 encoding, SARIF 2.1.0 schema conformance, no memory buffering for large datasets (streaming/batching)  
**Scale/Scope**: 3 new formatter classes, 2 CLI flags, ~15 unit tests per formatter, 8 integration tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Layered Architecture**: YES - Formatters belong in Reporting layer (pure transformation), CLI orchestrates. No violations.
- [x] **DI Boundaries**: YES - Formatters instantiated in CLI via DI, not used in AnalyzerEngine/Reporting. Compliant.
- [x] **Rule Contracts**: N/A - This feature does not add or modify analyzer rules.
- [x] **Explicit Execution**: YES - `--output` flag extends build → execute → exit model. No background processes.
- [x] **Testing Discipline**: YES - Formatters are pure functions testable with mock DiagnosticResult data, no DI required.
- [x] **Determinism**: YES - Same DiagnosticResult collection → same output. No time-based or random behavior.
- [x] **Extensibility**: YES - IReportFormatter abstraction enables third-party formatters (e.g., XML, HTML) without breaking changes.

*Violations MUST be documented in Complexity Tracking section with justification.*

**Result**: ✅ ALL CHECKS PASSED - No constitutional violations

## Project Structure

### Documentation (this feature)

```text
specs/006-structured-output-formats/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification (completed)
├── research.md          # Phase 0 output (SARIF schema analysis, JSON design patterns)
├── data-model.md        # Phase 1 output (IReportFormatter contract, OutputConfiguration)
├── quickstart.md        # Phase 1 output (Usage examples for all 3 formats)
├── contracts/           # Phase 1 output (JSON schema, SARIF validation examples)
│   ├── json-schema.md   # Inline JSON schema documentation
│   └── sarif-examples/  # Sample SARIF outputs for validation
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Lintelligent.Reporting/
│   ├── ReportGenerator.cs           # EXISTING - enhanced for --group-by
│   ├── Formatters/                  # NEW
│   │   ├── IReportFormatter.cs      # NEW - abstraction (FR-001)
│   │   ├── JsonFormatter.cs         # NEW - JSON implementation (FR-002, FR-007)
│   │   ├── SarifFormatter.cs        # NEW - SARIF implementation (FR-003, FR-008, FR-015)
│   │   └── MarkdownFormatter.cs     # NEW - enhanced markdown (FR-004, FR-013)
│   └── Lintelligent.Reporting.csproj # MODIFIED - add Microsoft.CodeAnalysis.Sarif dependency
│
└── Lintelligent.Cli/
    ├── Commands/
    │   └── ScanCommand.cs           # MODIFIED - add --format and --output flags (FR-005, FR-006)
    ├── Infrastructure/
    │   └── OutputWriter.cs          # NEW - file/stdout abstraction (FR-009, FR-010, FR-011)
    └── Lintelligent.Cli.csproj      # MODIFIED - register formatters in DI

tests/
├── Lintelligent.Reporting.Tests/
│   ├── Formatters/                  # NEW
│   │   ├── JsonFormatterTests.cs    # NEW - 15 tests (schema, edge cases, performance)
│   │   ├── SarifFormatterTests.cs   # NEW - 15 tests (SARIF 2.1.0 conformance, VS Code compat)
│   │   └── MarkdownFormatterTests.cs # NEW - 12 tests (summary, colors, grouping)
│   └── Lintelligent.Reporting.Tests.csproj
│
└── Lintelligent.Cli.Tests/
    ├── Commands/
    │   └── ScanCommandTests.cs      # MODIFIED - add tests for --format/--output integration
    └── Lintelligent.Cli.Tests.csproj
```

**Structure Decision**: Single-project structure maintained. Formatters belong in `Lintelligent.Reporting` (pure transformation layer per Constitution Principle I). CLI layer orchestrates formatter selection and output destination. No new projects needed.

## Complexity Tracking

> **No constitutional violations to justify** - all checks passed ✅

## Phase Breakdown

### Phase 0: Research & Discovery ✅

**Objective**: Resolve all technical unknowns before implementation

**Deliverables**:
- `research.md` - SARIF 2.1.0 schema analysis, JSON design patterns, color detection strategies
- Decision on Microsoft.CodeAnalysis.Sarif vs custom SARIF serialization
- Decision on streaming vs buffering for large datasets
- ANSI color code detection strategy (Console.IsOutputRedirected + environment variables)

**Exit Criteria**: All "NEEDS CLARIFICATION" items resolved, technology stack finalized

---

### Phase 1: Design & Contracts ✅

**Objective**: Define interfaces, data models, and API contracts

**Deliverables**:
- `data-model.md` - IReportFormatter interface, OutputConfiguration model, SARIF mapping design
- `contracts/json-schema.md` - JSON output schema documentation
- `contracts/sarif-examples/` - Sample SARIF outputs for validation testing
- `quickstart.md` - Usage examples for all three output formats

**Tasks**:
1. Design IReportFormatter interface (single Format method)
2. Define OutputConfiguration (Format enum, OutputPath, ColorSupport)
3. Document JSON schema (status, summary, violations array structure)
4. Map DiagnosticResult → SARIF result object (line-only region, no columns)
5. Design enhanced Markdown structure (summary table + grouped violations)

**Exit Criteria**: All interfaces defined, contracts documented, no ambiguities in data mapping

---

### Phase 2: Foundation - Abstraction & Infrastructure

**Objective**: Implement core abstractions and CLI infrastructure

**Deliverables**:
- `IReportFormatter.cs` - Interface with Format method
- `OutputWriter.cs` - File/stdout abstraction (FR-009, FR-010, FR-011)
- Updated `ScanCommand.cs` - Parse --format and --output flags
- Unit tests for OutputWriter (path validation, file creation, stdout handling)

**Tasks**:
1. Create IReportFormatter interface in Lintelligent.Reporting
2. Implement OutputWriter with file validation (writable check, not path traversal prevention)
3. Add --format flag parsing to ScanCommand (json|sarif|markdown, default markdown)
4. Add --output flag parsing to ScanCommand (file path or `-` for stdout)
5. Write 8 unit tests for OutputWriter edge cases (FR-009 validation)

**Dependencies**: None (foundational phase)

**Exit Criteria**: Abstraction compiles, CLI flags parsed, OutputWriter tested

---

### Phase 3: JSON Formatter Implementation

**Objective**: Implement and validate JSON formatter

**Deliverables**:
- `JsonFormatter.cs` - IReportFormatter implementation
- 15 unit tests covering schema conformance, edge cases, performance
- JSON schema validation tests (jq, PowerShell ConvertFrom-Json)

**Tasks**:
1. Implement JsonFormatter.Format() using System.Text.Json
2. Generate JSON structure: { status, summary: { total, bySeverity }, violations: [...] }
3. Handle empty result sets (FR-014)
4. Escape special characters (FR-012)
5. Write tests: valid schema, 0 violations, 10K violations <10s (SC-008), severity filtering
6. Validate output with jq and PowerShell (SC-004)

**Dependencies**: Phase 2 (IReportFormatter abstraction)

**Exit Criteria**: JSON validates against schema, SC-001 and SC-004 passing, performance SC-008 met

---

### Phase 4: SARIF Formatter Implementation

**Objective**: Implement SARIF 2.1.0 formatter with IDE compatibility

**Deliverables**:
- `SarifFormatter.cs` - IReportFormatter implementation
- 15 unit tests covering SARIF schema conformance, VS Code import, GitHub Code Scanning
- Microsoft.CodeAnalysis.Sarif NuGet package integrated

**Tasks**:
1. Add Microsoft.CodeAnalysis.Sarif to Lintelligent.Reporting.csproj
2. Implement SarifFormatter.Format() using SARIF object model
3. Map DiagnosticResult → SarifResult (physicalLocation with line-only region)
4. Generate tool metadata (driver, rules array with helpUri per FR-015)
5. Handle empty result sets (FR-014)
6. Write tests: SARIF schema validation, VS Code import simulation, 10K violations <10s
7. Validate with official SARIF validator (SC-002)

**Dependencies**: Phase 2 (IReportFormatter abstraction)

**Exit Criteria**: SARIF validates against 2.1.0 schema, SC-002 and SC-003 passing

---

### Phase 5: Enhanced Markdown Formatter

**Objective**: Enhance existing markdown with summary table, colors, grouping

**Deliverables**:
- `MarkdownFormatter.cs` - IReportFormatter implementation (refactored from ReportGenerator)
- Updated `ReportGenerator.cs` - delegated to MarkdownFormatter
- 12 unit tests covering summary table, color detection, file/category grouping

**Tasks**:
1. Extract markdown generation from ReportGenerator into MarkdownFormatter
2. Add summary statistics table (total, by severity) - SC-005
3. Implement ANSI color codes with auto-detection (Console.IsOutputRedirected, NO_COLOR env var)
4. Add file grouping (violations grouped by FilePath)
5. Support --group-by category from existing ScanCommand logic
6. Write tests: summary table format, color detection, color suppression on redirect, grouping

**Dependencies**: Phase 2 (IReportFormatter abstraction)

**Exit Criteria**: Markdown includes summary table (SC-005), colors auto-detect, grouping works

---

### Phase 6: CLI Integration & Orchestration

**Objective**: Wire formatters into ScanCommand with DI

**Deliverables**:
- Updated `ScanCommand.cs` - formatter selection based on --format flag
- Updated `Bootstrapper.cs` - register all formatters in DI container
- OutputWriter integration for file/stdout separation (FR-011)
- 8 integration tests in ScanCommandTests

**Tasks**:
1. Register IReportFormatter implementations in DI (JsonFormatter, SarifFormatter, MarkdownFormatter)
2. Inject IEnumerable<IReportFormatter> or factory into ScanCommand
3. Select formatter based on --format flag value
4. Use OutputWriter to write formatted output (file or stdout)
5. Ensure stdout clean when --output file used (SC-006)
6. Write integration tests: each format end-to-end, file output, stdout output, invalid format error

**Dependencies**: Phase 3 (JSON), Phase 4 (SARIF), Phase 5 (Markdown)

**Exit Criteria**: All 3 formats selectable via CLI, file output works (SC-006), integration tests passing

---

### Phase 7: Performance Validation & Edge Cases

**Objective**: Validate performance targets and edge case handling

**Deliverables**:
- Performance tests for 10,000 violations (SC-008)
- Edge case tests (disk full, concurrent writes, invalid paths)
- Updated `.editorconfig` if needed for test suppressions

**Tasks**:
1. Write performance test: 10K DiagnosticResult → all 3 formats <10s (SC-008)
2. Test edge case: read-only output path → clear error message
3. Test edge case: invalid --format value → list valid formats
4. Test edge case: disk full simulation → error reported, partial file cleanup
5. Document concurrent write behavior (no special handling, OS behavior wins)
6. Verify all formatters handle empty results (FR-014)

**Dependencies**: Phase 6 (full integration)

**Exit Criteria**: SC-008 performance met, all edge cases documented/tested

---

### Phase 8: Documentation & Examples

**Objective**: Create user-facing documentation and examples

**Deliverables**:
- Updated README.md with --format and --output examples
- `specs/006-structured-output-formats/USAGE_GUIDE.md` - comprehensive format guide
- Sample JSON/SARIF outputs in `contracts/` directory
- CI/CD integration examples (GitHub Actions, Azure Pipelines)

**Tasks**:
1. Document --format flag usage in README
2. Document --output flag usage (file paths, stdout with `-`)
3. Create USAGE_GUIDE.md with examples for each format
4. Add CI/CD integration examples (parsing JSON in bash/PowerShell, SARIF upload to GitHub)
5. Document SARIF VS Code import workflow
6. Add troubleshooting section (common errors, schema validation failures)

**Dependencies**: Phase 7 (feature complete)

**Exit Criteria**: Comprehensive documentation, copy-paste ready examples, CI/CD integration guide

---

### Phase 9: Schema Validation & Compliance

**Objective**: Final validation against published schemas

**Deliverables**:
- JSON schema validation report
- SARIF 2.1.0 official validator results
- VS Code SARIF import test results
- GitHub Code Scanning compatibility test

**Tasks**:
1. Validate JSON output with published schema (SC-001)
2. Validate SARIF output with official SARIF 2.1.0 validator (SC-002)
3. Import SARIF into VS Code, verify Problems panel display (SC-003)
4. Test SARIF upload to GitHub Code Scanning (mock repository)
5. Verify jq and PowerShell parsing (SC-004)
6. Document validation results in RELEASE_NOTES

**Dependencies**: Phase 8 (documentation)

**Exit Criteria**: All schema validation success criteria (SC-001 through SC-004) passing

---

### Phase 10: Release Preparation

**Objective**: Finalize release artifacts and merge to main

**Deliverables**:
- `specs/006-structured-output-formats/RELEASE_NOTES.md`
- Updated CHANGELOG.md in repo root
- Feature merged to main branch
- Git tag for feature release

**Tasks**:
1. Write RELEASE_NOTES.md summarizing all 3 formats
2. Update CHANGELOG.md with feature entry
3. Verify all 170+ tests still passing (existing + new)
4. Final constitution check (re-verify Phase 1 design)
5. Create PR from 006-structured-output-formats → main
6. Merge PR after review
7. Tag release (v2.0.0 if breaking CLI changes, else v1.1.0)

**Dependencies**: Phase 9 (validation complete)

**Exit Criteria**: Feature merged, released, documented

---

## Phase Summary

| Phase | Focus | Estimated Tasks | Key Deliverables |
|-------|-------|-----------------|------------------|
| 0 | Research | ~5 | research.md, tech decisions |
| 1 | Design | ~8 | data-model.md, contracts/, quickstart.md |
| 2 | Foundation | ~10 | IReportFormatter, OutputWriter, CLI flags |
| 3 | JSON | ~12 | JsonFormatter, 15 tests, schema validation |
| 4 | SARIF | ~15 | SarifFormatter, 15 tests, SARIF validation |
| 5 | Markdown | ~10 | MarkdownFormatter, 12 tests, color/grouping |
| 6 | Integration | ~12 | ScanCommand wiring, DI setup, 8 integration tests |
| 7 | Performance | ~8 | 10K perf tests, edge case tests |
| 8 | Documentation | ~10 | USAGE_GUIDE, README updates, CI examples |
| 9 | Validation | ~8 | Schema validation, VS Code/GitHub tests |
| 10 | Release | ~6 | RELEASE_NOTES, CHANGELOG, merge |
| **Total** | **10 phases** | **~104 tasks** | **3 formatters, 50+ tests, full docs** |

---

## Next Steps

1. ✅ **Phase 0 Complete** - Run research agents for SARIF schema, JSON patterns, color detection
2. ✅ **Phase 1 Complete** - Generate data-model.md, contracts/, quickstart.md
3. **Phase 2-10 Pending** - Execute via `/speckit.tasks` command to generate tasks.md

**Command to proceed**: `/speckit.tasks` (generates actionable task list from this plan)

