# Feature Specification: Enhanced Rule Contract

**Feature Branch**: `002-rule-contract-enhancement`  
**Created**: 2025-12-23  
**Status**: Draft  
**Input**: User description: "Enhanced Rule Contract - Align IAnalyzerRule with constitutional requirements for metadata and multiple findings"

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Severity-Based Filtering (Priority: P1)

As a CLI user, I want to filter analysis results by severity level (Error, Warning, Info), so I can focus on critical issues first and reduce noise in my reports.

**Why this priority**: This is the core value proposition - enabling users to prioritize their work based on issue severity. Without severity metadata, all diagnostics are treated equally, making it impossible to distinguish critical bugs from style suggestions.

**Independent Test**: Can be fully tested by running analysis with rules of different severities and verifying that filtering by severity level returns only matching diagnostics.

**Acceptance Scenarios**:

1. **Given** rules with different severity levels (Error, Warning, Info), **When** I analyze code and filter by "Error" severity, **Then** only diagnostics from Error-severity rules are returned
2. **Given** a rule configured with "Warning" severity, **When** I analyze code, **Then** the diagnostic result includes the severity metadata
3. **Given** multiple rules with mixed severities, **When** I filter results by severity, **Then** I can distinguish critical issues from informational ones

---

### User Story 2 - Multiple Findings Per File (Priority: P1)

As a rule developer, I want my rule to emit multiple findings when analyzing a single file, so I can report all violations instead of stopping at the first one.

**Why this priority**: This is a constitutional requirement (Principle III) and addresses a critical limitation - rules currently return only a single finding (or null), making them incomplete. A file with multiple long methods should report all of them, not just the first.

**Independent Test**: Can be tested by creating a rule that detects multiple violations in a single file and verifying that all violations are reported in the results.

**Acceptance Scenarios**:

1. **Given** a file with three long methods, **When** LongMethodRule analyzes it, **Then** three separate diagnostic results are returned (one for each method)
2. **Given** a file with no violations, **When** any rule analyzes it, **Then** an empty collection is returned (not null)
3. **Given** a rule that finds 10 violations in a file, **When** analyzing that file, **Then** all 10 violations appear in the final results

---

### User Story 3 - Categorization for Reporting (Priority: P2)

As a developer reviewing analysis reports, I want rules categorized by type (e.g., "Maintainability", "Performance", "Security"), so I can understand the nature of issues and prioritize fixes by category.

**Why this priority**: Categorization enables better reporting, metrics, and team workflows. While less critical than severity, it provides essential context for understanding and acting on findings.

**Independent Test**: Can be tested by creating rules with different categories and verifying that results include category metadata, allowing grouping in reports.

**Acceptance Scenarios**:

1. **Given** rules categorized as "Maintainability" and "Performance", **When** I generate a report, **Then** issues are grouped by category
2. **Given** a rule with category "Security", **When** it emits a finding, **Then** the diagnostic result includes the "Security" category
3. **Given** multiple categories defined, **When** filtering results, **Then** I can retrieve all findings for a specific category

---

### Edge Cases

- What happens when a rule emits an extremely large number of findings (e.g., 1000+ in a single file)? - Resolved: rely on streaming architecture (no hard limit)
- How does the system handle rules with invalid or missing metadata (empty string IDs, undefined severity)? - Resolved: validate at registration time (fail-fast)
- Can severity be "Unknown" or must it always be one of the defined enum values? - Resolved: fixed enum (Error/Warning/Info only)

## Clarifications

### Session 2025-12-23

- Q: What if a rule's Analyze method throws an exception - should it fail the entire analysis or just skip that rule? → A: Continue analysis, skip the failing rule, collect exceptions for reporting at end
- Q: How does severity and category metadata flow from rule to DiagnosticResult? → A: DiagnosticResult constructor requires severity and category as parameters (explicit)
- Q: What happens when a rule emits an extremely large number of findings (e.g., 1000+ in a single file)? → A: No limit, rely on streaming (IEnumerable) to handle large result sets efficiently
- Q: How does the system handle rules with invalid or missing metadata (empty string IDs, undefined severity)? → A: Validate at registration time - throw exception if rule has invalid metadata (fail-fast)
- Q: How should the Severity enum handle extensibility - should we allow for future expansion or unknown values? → A: Fixed enum (Error/Warning/Info only, no Unknown/Other)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: IAnalyzerRule interface MUST define a `Severity` property of type `Severity` enum
- **FR-002**: IAnalyzerRule interface MUST define a `Category` property of type `string`
- **FR-003**: IAnalyzerRule.Analyze() MUST return `IEnumerable<DiagnosticResult>` instead of `DiagnosticResult?`
- **FR-004**: Rules MUST be able to emit zero, one, or multiple findings per analyzed syntax tree
- **FR-005**: Severity enum MUST include values: Error, Warning, Info
- **FR-006**: DiagnosticResult MUST include severity and category from the rule that created it
- **FR-007**: LongMethodRule MUST be migrated to return all long methods in a file, not just the first one
- **FR-008**: LongMethodRule MUST define appropriate severity (Warning) and category ("Maintainability")
- **FR-009**: Rule metadata (Id, Severity, Category) MUST be immutable once the rule is instantiated
- **FR-010**: Empty result collections MUST be represented as empty enumerables, not null
- **FR-011**: AnalyzerEngine MUST continue analysis when a rule throws an exception, skipping that rule and collecting exceptions for end-of-analysis reporting
- **FR-012**: DiagnosticResult constructor MUST require severity and category as parameters to ensure metadata is explicitly provided at creation time
- **FR-013**: AnalyzerManager MUST validate rule metadata at registration time, throwing an exception if Id is null/empty or Severity is undefined (fail-fast validation)
- **FR-014**: Severity enum MUST contain only Error, Warning, and Info values (no Unknown or extensibility mechanisms)

### Key Entities

- **Severity Enum**: Represents the importance/impact level of a diagnostic finding
  - Values: Error (critical issues blocking release), Warning (should fix), Info (suggestions)
  - Used for filtering and prioritization in reports
  
- **Category**: String-based classification of rule type
  - Examples: "Maintainability", "Performance", "Security", "Style", "Design"
  - Enables grouping and filtering in reports
  - Not an enum to allow extensibility for custom categories

- **DiagnosticResult**: Enhanced to include severity and category
  - Constructor requires severity and category parameters (explicit metadata passing)
  - Inherits metadata from the rule that produced it
  - Represents a single finding at a specific location

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can filter analysis results by severity level, retrieving only Error, Warning, or Info diagnostics
- **SC-002**: Rules can emit multiple findings per file - a file with N violations produces N diagnostic results
- **SC-003**: 100% of existing rules (LongMethodRule) successfully migrated to new contract with severity and category
- **SC-004**: All diagnostic results include severity and category metadata (no null/missing values)
- **SC-005**: Analysis performance remains within ±10% when emitting multiple findings vs single finding
- **SC-006**: Zero breaking changes for code that only uses AnalyzerEngine (not directly instantiating rules)
- **SC-007**: Test coverage for rule contract ≥95% (rule metadata, multiple findings, severity filtering)

## Assumptions *(include if relevant)*

1. **Backward Compatibility**: Existing tests and code using AnalyzerEngine will need updates, but the engine's public API (Analyze method) remains unchanged
2. **Severity Defaults**: All existing rules will be assigned "Warning" severity and "General" category during migration
3. **Performance**: Emitting multiple findings uses lazy evaluation (IEnumerable) to avoid memory overhead; no hard limit on findings per file - streaming architecture handles large result sets
4. **Error Handling**: Rule exceptions are handled by AnalyzerEngine (continue analysis, skip failing rule, collect exceptions for reporting at end)
5. **Category Extensibility**: Categories are strings (not enums) to allow custom rules to define domain-specific categories

## Out of Scope *(include if relevant)*

- **Severity Configuration**: Users cannot override a rule's severity at runtime (future feature)
- **Custom Metadata**: No support for arbitrary key-value metadata beyond Id, Severity, Category
- **Rule Dependencies**: Rules still cannot depend on each other (constitutional constraint)
- **Diagnostic Suppression**: No mechanism to suppress specific findings (future feature)
- **Performance Optimization**: No specific tuning for rules that emit 100+ findings per file

## Dependencies *(include if relevant)*

- **Feature 001 (IO Boundary Refactor)**: Must be complete - AnalyzerEngine must accept IEnumerable<SyntaxTree>
- **Microsoft.CodeAnalysis**: No additional dependencies; uses existing Roslyn APIs
- **Breaking Change Migration**: Teams using custom IAnalyzerRule implementations will need to update their code
