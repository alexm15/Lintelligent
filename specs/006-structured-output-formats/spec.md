# Feature Specification: Structured Output Formats

**Feature Branch**: `006-structured-output-formats`  
**Created**: 2025-12-24  
**Status**: Draft  
**Priority**: P1  
**Constitutional Principle**: VI (Extensibility)

## User Scenarios & Testing

### User Story 1 - CI/CD Pipeline Integration with JSON Output (Priority: P1)

DevOps engineers need to integrate Lintelligent into CI/CD pipelines where structured machine-readable output is required for automated quality gates, trend analysis, and dashboards.

**Why this priority**: This is the most critical use case - enabling automated workflows is essential for modern development practices. Without structured output, the tool cannot be integrated into existing DevOps infrastructure.

**Independent Test**: Run `lintelligent scan /path/to/project --format json` and verify valid JSON output is produced with all diagnostic information. Parse the JSON programmatically and extract violation counts by severity.

**Acceptance Scenarios**:

1. **Given** a project with 5 code quality violations, **When** user runs `lintelligent scan --format json`, **Then** output is valid JSON containing all 5 violations with complete metadata (file, line, rule, severity, message)
2. **Given** JSON output mode, **When** analysis completes successfully, **Then** exit code is 0 and JSON includes `"status": "success"` field
3. **Given** JSON output with errors, **When** analysis finds critical violations, **Then** JSON includes `"violations"` array with structured diagnostic objects
4. **Given** CI pipeline parsing JSON output, **When** extracting violation count by severity, **Then** JSON schema allows filtering by `severity` field (`Error`, `Warning`, `Info`)

---

### User Story 2 - SARIF for IDE and Security Tool Integration (Priority: P2)

Security teams and IDE developers need standardized SARIF (Static Analysis Results Interchange Format) output to integrate Lintelligent with security dashboards, VS Code Problem panel, GitHub Code Scanning, and other SARIF-consuming tools.

**Why this priority**: SARIF is the industry standard for interoperability. This enables integration with Microsoft security tooling, GitHub Advanced Security, and any IDE supporting SARIF.

**Independent Test**: Run `lintelligent scan --format sarif` and validate output against SARIF schema v2.1.0. Import SARIF file into VS Code and verify violations appear in Problems panel.

**Acceptance Scenarios**:

1. **Given** a project with violations, **When** user runs `lintelligent scan --format sarif`, **Then** output is valid SARIF 2.1.0 JSON conforming to the official schema
2. **Given** SARIF output, **When** imported into VS Code, **Then** violations appear in Problems panel with correct file locations and severities
3. **Given** SARIF file, **When** uploaded to GitHub Code Scanning, **Then** results appear in Security tab with clickable file references
4. **Given** multiple rules triggered, **When** SARIF is generated, **Then** `tool.driver.rules` array contains metadata for all triggered rules (ID, name, helpUri, defaultConfiguration)

---

### User Story 3 - Output to File for Report Archival (Priority: P2)

Build engineers need to save analysis results to files for archival, trend tracking over time, and artifact storage in CI systems.

**Why this priority**: Essential for compliance, historical analysis, and debugging. Teams need to compare reports across builds to track quality trends.

**Independent Test**: Run `lintelligent scan --format json --output results.json` and verify file is created with correct content. Verify stdout remains clean (no diagnostic output mixed with JSON).

**Acceptance Scenarios**:

1. **Given** `--output report.json` flag, **When** analysis completes, **Then** JSON is written to `report.json` and stdout contains only a success message
2. **Given** non-existent directory path `--output /path/to/nonexistent/report.json`, **When** command runs, **Then** error message explains directory doesn't exist
3. **Given** `--output -` (stdout), **When** `--format json` is used, **Then** JSON is written to stdout with no other text
4. **Given** existing file at output path, **When** command runs, **Then** file is overwritten with new results (with warning message)

---

### User Story 4 - Enhanced Markdown for Human Review (Priority: P3)

Developers reviewing reports manually need improved markdown formatting with better organization, color-coded severity, file grouping, and summary statistics.

**Why this priority**: Improves the existing experience but is not blocking for new use cases. The current markdown output is functional.

**Independent Test**: Run `lintelligent scan --format markdown` and verify output includes summary table with counts by severity and category, followed by violations grouped by file.

**Acceptance Scenarios**:

1. **Given** violations across multiple files, **When** markdown output is generated, **Then** output includes summary section with total counts by severity (Error, Warning, Info)
2. **Given** markdown output, **When** displayed in terminal, **Then** severity levels use color coding (red for Error, yellow for Warning, blue for Info)
3. **Given** violations in 3 files, **When** markdown output is generated, **Then** violations are grouped by file path with clear section headers
4. **Given** `--group-by category` with markdown, **When** output is generated, **Then** violations are grouped by category within each file

---

### Edge Cases

- What happens when `--output` path points to a read-only location? → Error message with clear explanation
- How does system handle binary or non-text output formats with `--output -` (stdout redirect)? → Binary formats should warn against stdout usage
- What if `--format` is invalid (e.g., `--format xml`)? → Error message listing valid formats
- What happens when output file write fails mid-stream (disk full)? → Error reported, partial file cleaned up or marked incomplete
- How to handle very large result sets (10,000+ violations) in JSON/SARIF? → Stream output, no in-memory buffering
- What if SARIF results include unsupported characters (e.g., null bytes in source code)? → Escape properly according to SARIF spec

## Requirements

### Functional Requirements

- **FR-001**: System MUST provide abstraction `IReportFormatter` with method `Format(IEnumerable<DiagnosticResult> results) : string`
- **FR-002**: System MUST implement `JsonFormatter` producing valid JSON with schema: `{ "status": string, "summary": {...}, "violations": [...] }`
- **FR-003**: System MUST implement `SarifFormatter` producing valid SARIF v2.1.0 conforming to official schema at https://docs.oasis-open.org/sarif/sarif/v2.1.0/
- **FR-004**: System MUST enhance existing `MarkdownFormatter` to include summary statistics table and file grouping
- **FR-005**: CLI MUST accept `--format <json|sarif|markdown>` flag with `markdown` as default
- **FR-006**: CLI MUST accept `--output <path>` flag to write results to file instead of stdout
- **FR-007**: JSON output MUST include: status (success/failure), total violation count, counts by severity, violations array with file/line/rule/severity/message
- **FR-008**: SARIF output MUST include: `tool` metadata, `rules` array with all rule definitions, `results` array with violation locations
- **FR-009**: System MUST validate output file path is writable before running analysis
- **FR-010**: System MUST support `--output -` to explicitly write to stdout (useful for shell pipes)
- **FR-011**: When `--output` is used, stdout MUST contain only progress/summary messages, not the full report
- **FR-012**: JSON and SARIF outputs MUST escape special characters correctly (quotes, newlines, Unicode)
- **FR-013**: Markdown output MUST use ANSI color codes when terminal supports color (auto-detected)
- **FR-014**: All formatters MUST handle empty result sets gracefully (no violations found)
- **FR-015**: SARIF output MUST include `helpUri` for each rule pointing to rule documentation

### Key Entities

- **ReportFormatter**: Abstract interface defining contract for all output formatters
  - Input: Collection of DiagnosticResult objects
  - Output: Formatted string representation
  - Variants: JsonFormatter, SarifFormatter, MarkdownFormatter

- **DiagnosticResult**: Core data structure containing violation details
  - Properties: FilePath, LineNumber, RuleId, Severity, Message, Category
  - Used as input to all formatters

- **SarifReport**: SARIF-specific structure conforming to SARIF 2.1.0 schema
  - Components: Run metadata, Tool info, Rules array, Results array
  - Must include physical locations with region (line/column) information

- **OutputConfiguration**: CLI configuration for output options
  - Properties: Format (enum), OutputPath (string or stdout), ColorSupport (bool)

## Success Criteria

### Measurable Outcomes

- **SC-001**: JSON output validates against a published JSON schema (100% schema conformance)
- **SC-002**: SARIF output validates against SARIF 2.1.0 schema using official validator (100% conformance)
- **SC-003**: VS Code can import SARIF output and display violations in Problems panel without errors
- **SC-004**: JSON output can be parsed by `jq` and `ConvertFrom-Json` PowerShell cmdlet without errors
- **SC-005**: Markdown output includes summary table showing violation count by severity (Error/Warning/Info)
- **SC-006**: File output mode (`--output file.json`) completes without writing diagnostics to stdout (clean separation)
- **SC-007**: Formatters handle 10,000+ violations without memory exhaustion (streaming or batching required)
- **SC-008**: All three formats represent the same diagnostic data with 100% fidelity (no information loss)

## Assumptions

- Output formatters operate on in-memory `DiagnosticResult` collections (no streaming of analysis results yet - that's a future performance optimization)
- SARIF version 2.1.0 is the target (latest stable version as of 2024)
- Markdown output targets terminals supporting ANSI color codes (Windows Terminal, iTerm2, modern Linux terminals)
- `--output -` convention for stdout is familiar to CLI users (common in Unix tools like `tar`, `cat`)
- Users have write permissions to output file paths they specify
- JSON schema is defined inline in code documentation (no separate .json schema file published initially)
- Color detection uses `Console.IsOutputRedirected` and environment variable checks (`NO_COLOR`, `TERM`)

## Out of Scope

- XML output format (not requested, can be added later if needed)
- HTML output with interactive filtering (future enhancement)
- Real-time streaming output during analysis (current design processes results after analysis completes)
- Custom output templates or user-defined formats (extensibility for future)
- Diff output comparing two analysis runs (separate feature)
- Integration with specific CI platforms (GitHub Actions, Azure Pipelines) - users integrate via JSON/SARIF
- Localization of output messages to non-English languages
- Binary formats (Protobuf, MessagePack) for performance - use JSON for now
- Output compression (gzip, zip) - users can pipe to compression tools if needed

## Dependencies

- **Microsoft.CodeAnalysis.Sarif** NuGet package (v4.x) for SARIF object model and validation
- **System.Text.Json** (built-in .NET) for JSON serialization
- Existing `DiagnosticResult` model from `Lintelligent.AnalyzerEngine`
- Existing `MarkdownFormatter` in `Lintelligent.Reporting` (to be enhanced)
- CLI command infrastructure (`ScanCommand`) for new flags

## Technical Constraints

- JSON output must be UTF-8 encoded
- SARIF must conform to https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html specification
- File paths in SARIF must use URI format (`file:///C:/path/to/file.cs`)
- Markdown ANSI codes must not appear when output is redirected to file (color detection required)
- Output file writes must be atomic where possible (write to temp file, then rename)

## Constitutional Compliance

**Principle VI (Extensibility)**:
- ✅ `IReportFormatter` abstraction allows third-party formatters to be added via plugin architecture
- ✅ New formats (XML, HTML) can be added without modifying existing formatters
- ✅ Formatters are decoupled from analysis engine - clean separation of concerns
- ✅ Output system can evolve independently (e.g., add streaming later) without breaking contracts

**Principle IV (Composition over Inheritance)**:
- ✅ Formatters compose `DiagnosticResult` data rather than inheriting from analysis components
- ✅ Output configuration is passed as data, not through inheritance hierarchies

**Principle VII (Testability)**:
- ✅ Each formatter can be unit tested independently with mock diagnostic data
- ✅ Output validation can test schema conformance without running analysis
- ✅ File I/O is abstracted to allow testing without filesystem dependencies

## Open Questions

None - all requirements are clear from the feature description.

## Notes

- SARIF support will significantly improve GitHub Advanced Security integration
- JSON output enables custom dashboard creation (Grafana, Power BI)
- This feature unblocks enterprise adoption where standardized reporting is mandatory
- Markdown enhancements improve developer experience without breaking existing workflows

- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]

*Example of marking unclear requirements:*

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]
