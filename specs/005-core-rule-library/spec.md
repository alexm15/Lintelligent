# Feature Specification: Core Rule Library

**Feature Branch**: `005-core-rule-library`  
**Created**: December 24, 2025  
**Status**: Draft  
**Priority**: P1  
**Constitutional Principles**: III (Rule Contract), VII (Determinism)  
**Input**: User description: "Implement essential C# code smell detection rules: Long Parameter List (>5 params), Complex Conditional (nested if >3), Magic Numbers (hardcoded literals), God Class (>500 LOC or >15 methods), Dead Code (unused private members), Exception Swallowing (empty catch), Missing XML Documentation (public APIs). Enhance existing Long Method rule. Each rule independently testable with documentation (what, why, how to fix). Categorized: Code Smell, Design, Maintainability, Documentation."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Long Parameter List Detection (Priority: P1)

Developers want to identify methods with excessive parameters (more than 5) that make code difficult to understand and test. When analyzing C# code, the system detects methods with more than 5 parameters and reports them as code smells with actionable guidance.

**Why this priority**: Parameter lists are visible in every method signature and directly impact code readability and testability. This is a universally recognized code smell with clear, objective detection criteria.

**Independent Test**: Analyze a C# file containing a method with 6 parameters. The rule returns a diagnostic indicating "Method has 6 parameters (max: 5)" with severity Warning, category "Code Smell", and suggests refactoring to a parameter object or builder pattern.

**Acceptance Scenarios**:

1. **Given** a method with exactly 5 parameters, **When** the rule analyzes it, **Then** no diagnostic is returned
2. **Given** a method with 6 parameters, **When** the rule analyzes it, **Then** a diagnostic is returned with message "Method 'MethodName' has 6 parameters (max: 5)"
3. **Given** a constructor with 8 parameters, **When** the rule analyzes it, **Then** a diagnostic is returned (rule applies to all method declarations including constructors)
4. **Given** a method with 0 parameters, **When** the rule analyzes it, **Then** no diagnostic is returned

---

### User Story 2 - Complex Conditional Detection (Priority: P1)

Developers want to identify deeply nested conditionals (depth >3) that reduce code maintainability and increase cyclomatic complexity. The system detects if statements nested more than 3 levels deep and recommends extraction to separate methods or guard clauses.

**Why this priority**: Deeply nested conditionals are a primary source of bugs and comprehension difficulties. Clear, objective detection criteria (count nesting depth) with high developer value.

**Independent Test**: Analyze a method with if statements nested 4 levels deep. The rule returns a diagnostic with severity Warning, category "Code Smell", and location pointing to the deepest nested if statement.

**Acceptance Scenarios**:

1. **Given** a method with if statements nested 3 levels deep, **When** the rule analyzes it, **Then** no diagnostic is returned
2. **Given** a method with if statements nested 4 levels deep, **When** the rule analyzes it, **Then** a diagnostic is returned indicating "Conditional nesting depth is 4 (max: 3)"
3. **Given** a method with if-else-if chains at the same level, **When** the rule analyzes it, **Then** no diagnostic is returned (only nesting depth matters, not chaining)
4. **Given** nested if statements with switch statements mixed in, **When** the rule counts nesting depth, **Then** both if and switch contribute to nesting depth

---

### User Story 3 - Magic Number Detection (Priority: P2)

Developers want to identify hardcoded numeric literals (magic numbers) that lack context and reduce code maintainability. The system detects numeric literals in code (excluding common constants like 0, 1, -1) and suggests replacing them with named constants.

**Why this priority**: Magic numbers are widespread and significantly impact code clarity, but detection requires filtering out acceptable cases (0, 1, array indices). Slightly more complex heuristics than parameter/nesting detection.

**Independent Test**: Analyze code containing `if (status == 3)` without a named constant. The rule returns a diagnostic indicating "Magic number '3' should be replaced with a named constant" with severity Info.

**Acceptance Scenarios**:

1. **Given** code using numeric literal `0`, **When** the rule analyzes it, **Then** no diagnostic is returned (0, 1, -1 are acceptable)
2. **Given** code with `const int MaxRetries = 3;` and later `if (retries < MaxRetries)`, **When** the rule analyzes it, **Then** no diagnostic is returned (already a named constant)
3. **Given** code with `Thread.Sleep(5000);`, **When** the rule analyzes it, **Then** a diagnostic is returned suggesting a named constant like `TimeoutMilliseconds`
4. **Given** code with `double pi = 3.14159;`, **When** the rule analyzes it, **Then** a diagnostic is returned (floating-point literals also qualify as magic numbers)
5. **Given** attribute arguments like `[MaxLength(255)]`, **When** the rule analyzes them, **Then** diagnostics are returned only if the number isn't a framework-standard value

---

### User Story 4 - God Class Detection (Priority: P2)

Developers want to identify classes that violate Single Responsibility Principle by being too large (>500 lines of code OR >15 methods). The system flags these classes and recommends splitting them into smaller, focused classes.

**Why this priority**: God classes are high-impact code smells but require counting LOC and methods accurately. Slightly lower priority because they're less frequent than method-level issues.

**Independent Test**: Analyze a class with 520 lines of code. The rule returns a diagnostic with severity Warning, category "Design", message "Class 'CustomerService' has 520 lines (max: 500)", and suggests splitting by responsibility.

**Acceptance Scenarios**:

1. **Given** a class with exactly 500 lines of code and 10 methods, **When** the rule analyzes it, **Then** no diagnostic is returned
2. **Given** a class with 501 lines of code, **When** the rule analyzes it, **Then** a diagnostic is returned
3. **Given** a class with 300 lines but 16 methods, **When** the rule analyzes it, **Then** a diagnostic is returned (either threshold triggers detection)
4. **Given** a class with 250 lines and 14 methods, **When** the rule analyzes it, **Then** no diagnostic is returned
5. **Given** a class with auto-properties and simple getters/setters, **When** counting methods, **Then** only count actual method declarations (not auto-property accessors)

---

### User Story 5 - Dead Code Detection (Priority: P3)

Developers want to identify unused private methods and fields that clutter the codebase and confuse maintainers. The system detects private members with no references within the containing class and recommends removal.

**Why this priority**: Dead code detection requires semantic analysis (finding references), which is more complex than syntax analysis. Lower priority because unused code doesn't cause bugs, just maintenance overhead.

**Independent Test**: Analyze a class with a private method that is never called. The rule returns a diagnostic with severity Info, category "Maintainability", indicating "Private method 'HelperMethod' is never used".

**Acceptance Scenarios**:

1. **Given** a private method called once within the same class, **When** the rule analyzes it, **Then** no diagnostic is returned
2. **Given** a private method with zero call sites, **When** the rule analyzes it, **Then** a diagnostic is returned
3. **Given** a private field assigned but never read, **When** the rule analyzes it, **Then** a diagnostic is returned
4. **Given** a private field used only in its own declaration (e.g., `private int _count = 0;` with no other usage), **When** the rule analyzes it, **Then** a diagnostic is returned
5. **Given** a public method with no call sites, **When** the rule analyzes it, **Then** no diagnostic is returned (only private members are checked)

---

### User Story 6 - Exception Swallowing Detection (Priority: P1)

Developers want to identify empty catch blocks that silently suppress exceptions and hide errors. The system detects catch blocks with no statements and flags them as dangerous practices.

**Why this priority**: Exception swallowing directly causes production bugs by hiding failures. High severity, clear detection criteria (catch block statement count == 0), and universally recognized anti-pattern.

**Independent Test**: Analyze a try-catch block with an empty catch clause. The rule returns a diagnostic with severity Warning, category "Code Smell", message "Empty catch block suppresses exceptions", and suggests logging or re-throwing.

**Acceptance Scenarios**:

1. **Given** a catch block containing a single throw statement, **When** the rule analyzes it, **Then** no diagnostic is returned (re-throw is acceptable)
2. **Given** a catch block containing only a comment but no executable code, **When** the rule analyzes it, **Then** a diagnostic is returned
3. **Given** a catch block with `catch (Exception) { }`, **When** the rule analyzes it, **Then** a diagnostic is returned
4. **Given** a catch block with logging code like `Logger.Error(ex)`, **When** the rule analyzes it, **Then** no diagnostic is returned
5. **Given** nested try-catch with outer catch empty but inner catch not empty, **When** the rule analyzes it, **Then** a diagnostic is returned only for the outer catch

---

### User Story 7 - Missing XML Documentation Detection (Priority: P3)

Developers want to ensure all public APIs have XML documentation comments for IntelliSense and API discoverability. The system detects public classes, methods, and properties without XML doc comments and flags them.

**Why this priority**: Documentation quality is important but doesn't affect runtime behavior. Lower priority because it's a quality-of-life improvement rather than a functional issue. Requires checking both syntax (doc comment presence) and accessibility (public modifier).

**Independent Test**: Analyze a public method without an XML doc comment (`/// <summary>`). The rule returns a diagnostic with severity Info, category "Documentation", indicating "Public method 'Calculate' is missing XML documentation".

**Acceptance Scenarios**:

1. **Given** a public method with a `/// <summary>` comment, **When** the rule analyzes it, **Then** no diagnostic is returned
2. **Given** a public method with no doc comment, **When** the rule analyzes it, **Then** a diagnostic is returned
3. **Given** a private method with no doc comment, **When** the rule analyzes it, **Then** no diagnostic is returned (only public APIs require documentation)
4. **Given** a public property with no doc comment, **When** the rule analyzes it, **Then** a diagnostic is returned
5. **Given** a public class with no doc comment, **When** the rule analyzes it, **Then** a diagnostic is returned
6. **Given** a public method with a regular comment (`// comment`) but no XML doc, **When** the rule analyzes it, **Then** a diagnostic is returned (must be XML doc format)

---

### User Story 8 - Long Method Enhancement (Priority: P2)

The existing LongMethodRule needs enhancement to provide better categorization and documentation. Enhance the rule to use the updated category system ("Code Smell" instead of generic) and add fix guidance to the diagnostic message.

**Why this priority**: Leverages existing implementation and testing, only requires metadata and message updates. Priority P2 because it's an enhancement, not a new rule.

**Independent Test**: Analyze a method with 22 executable statements using the enhanced rule. The diagnostic message includes actionable fix guidance like "Consider extracting logical blocks into separate methods" and uses category "Code Smell".

**Acceptance Scenarios**:

1. **Given** the existing LongMethodRule, **When** enhanced with new category metadata, **Then** diagnostics use category "Code Smell"
2. **Given** a long method detected by the rule, **When** the diagnostic is created, **Then** the message includes fix guidance text
3. **Given** existing unit tests for LongMethodRule, **When** re-run after enhancement, **Then** all tests continue to pass
4. **Given** the enhanced rule metadata, **When** queried for category, **Then** it returns "Code Smell" instead of previous generic category

---

### Edge Cases

- **Partial classes**: For God Class detection, how should we count LOC and methods when a class is split across multiple files? (Count aggregated across all partial declarations)
- **Generated code**: Should rules skip auto-generated code files (e.g., `*.Designer.cs`, files with `<auto-generated>` comment)? (Yes, skip generated code to avoid noise)
- **Interface implementations**: For Dead Code detection, should we consider private methods that implement explicit interface members? (Yes, exclude from dead code detection)
- **Conditional compilation**: How to handle magic numbers or long methods inside `#if DEBUG` blocks? (Analyze all code paths; conditional compilation doesn't exempt code from quality rules)
- **Extension methods**: For parameter count, should the first `this` parameter count toward the limit? (No, extension method's `this` parameter doesn't count toward limit of 5)
- **Anonymous functions**: Should complex conditional detection apply inside lambda expressions? (Yes, nested conditionals in lambdas count toward depth)
- **XML doc inheritance**: For documentation detection, should rules flag public members with `<inheritdoc/>` as missing documentation? (No, `<inheritdoc/>` is acceptable documentation)
- **Empty catch with specific exception**: Is `catch (OperationCanceledException) { }` acceptable for cancellation scenarios? (Edge case for configuration - default behavior flags it, but rule could be configurable)

## Requirements *(mandatory)*

### Functional Requirements

#### Rule Detection Requirements

- **FR-001**: System MUST detect methods (including constructors) with more than 5 parameters
- **FR-002**: System MUST detect if statements nested more than 3 levels deep
- **FR-003**: System MUST detect numeric literals (excluding 0, 1, -1) used directly in code
- **FR-004**: System MUST detect classes exceeding 500 lines of code OR 15 method declarations
- **FR-005**: System MUST detect private methods and fields with zero references within their declaring class
- **FR-006**: System MUST detect catch blocks containing zero executable statements
- **FR-007**: System MUST detect public classes, methods, and properties without XML documentation comments
- **FR-008**: LongMethodRule MUST be enhanced with "Code Smell" category and fix guidance in diagnostic messages

#### Rule Metadata Requirements

- **FR-009**: Each rule MUST implement IAnalyzerRule interface per Constitutional Principle III
- **FR-010**: Each rule MUST provide a unique RuleId (format: LNT###, where ### is sequential number)
- **FR-011**: Each rule MUST specify Title, Description, Category, and Severity
- **FR-012**: Rule categories MUST be one of: "Code Smell", "Design", "Maintainability", "Documentation"
- **FR-013**: Rule severity MUST be one of: Warning (default for code smells), Info (for documentation/suggestions)

#### Diagnostic Requirements

- **FR-014**: Each diagnostic MUST include the exact location (file path, line number, column) of the violation
- **FR-015**: Each diagnostic message MUST be clear and actionable (state the problem and suggest a fix)
- **FR-016**: Diagnostic messages MUST include concrete values (e.g., "has 6 parameters (max: 5)" not "too many parameters")
- **FR-017**: For each diagnostic, the system MUST report the rule ID, severity, category, and message

#### Rule Behavior Requirements

- **FR-018**: Rules MUST skip auto-generated code files (files with `<auto-generated>` header comment or `*.Designer.cs` pattern)
- **FR-019**: Complex Conditional rule MUST count both if and switch statements when calculating nesting depth
- **FR-020**: Magic Number rule MUST exclude numeric literals 0, 1, and -1 from detection
- **FR-021**: God Class rule MUST trigger if EITHER LOC threshold (>500) OR method count threshold (>15) is exceeded
- **FR-022**: Dead Code rule MUST exclude private methods that implement explicit interface members
- **FR-023**: Exception Swallowing rule MUST flag catch blocks with only comments (no executable statements) as violations
- **FR-024**: XML Documentation rule MUST accept `<inheritdoc/>` as valid documentation for public members
- **FR-025**: Parameter Count rule MUST exclude the `this` parameter from extension methods when counting parameters

#### Testing Requirements

- **FR-026**: Each rule MUST have comprehensive unit tests covering all acceptance scenarios
- **FR-027**: Each rule test suite MUST include tests for boundary conditions (e.g., exactly at threshold, one above threshold)
- **FR-028**: Each rule test suite MUST verify correct diagnostic metadata (rule ID, severity, category)
- **FR-029**: Test code MUST verify diagnostic location accuracy (line and column numbers)
- **FR-030**: All rule implementations MUST be deterministic per Constitutional Principle VII (same input always produces same output)

### Key Entities *(mandatory for this feature)*

- **AnalyzerRule**: Represents a code quality rule that implements IAnalyzerRule interface
  - Attributes: RuleId (string), Title (string), Description (string), Category (string), Severity (enum)
  - Behavior: Analyze(SyntaxNode) returns collection of DiagnosticResult
  
- **DiagnosticResult**: Represents a detected violation of a rule
  - Attributes: RuleId, Message, Severity, Category, FilePath, Line, Column
  - Relationships: Created by AnalyzerRule, consumed by ReportGenerator

- **RuleCategory**: Enumeration or constant defining valid rule categories
  - Values: "Code Smell", "Design", "Maintainability", "Documentation"
  - Purpose: Enables grouping and filtering of diagnostics in reports

- **SyntaxNode**: Roslyn syntax tree node representing C# code structure
  - Relationships: Input to AnalyzerRule.Analyze method
  - Note: Part of Roslyn API, not created by this feature

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 8 rules (7 new + 1 enhanced) are implemented and pass comprehensive test suites (minimum 5 test cases per rule)
- **SC-002**: Each rule correctly identifies violations with zero false positives in boundary test cases
- **SC-003**: Rule execution completes analysis of a 1000-line file in under 500ms per rule (maintains fast feedback loop)
- **SC-004**: Diagnostic messages are clear and actionable - 90% of developers understand how to fix the issue without additional documentation
- **SC-005**: All rule metadata (RuleId, Title, Description, Category, Severity) is complete and accurate
- **SC-006**: Rules integrate seamlessly with existing AnalyzerEngine - no changes required to engine infrastructure
- **SC-007**: Test coverage for all rules combined reaches at least 95% line coverage
- **SC-008**: Each rule's documentation includes: what the rule detects, why it matters, and how to fix violations (3-section format)
- **SC-009**: Rules handle edge cases correctly (auto-generated code, partial classes, interface implementations) with zero crashes or false positives
- **SC-010**: All diagnostics include precise location information (file, line, column) accurate to within 1 character

### Rule-Specific Success Criteria

- **Long Parameter List**: Detects 100% of methods with >5 parameters, zero false positives on methods with exactly 5 or fewer
- **Complex Conditional**: Accurately counts nesting depth for if/switch combinations, detects all cases >3 levels
- **Magic Number**: Identifies hardcoded literals while correctly excluding 0, 1, -1 and named constants
- **God Class**: Correctly counts lines and methods across partial class declarations, triggers on either threshold
- **Dead Code**: Identifies unused private members with 100% accuracy, zero false positives on private interface implementations
- **Exception Swallowing**: Detects all empty catch blocks, zero false positives on catch blocks with re-throw or logging
- **Missing XML Documentation**: Detects all undocumented public APIs, correctly accepts `<inheritdoc/>` as valid documentation
- **Long Method Enhancement**: Existing tests continue passing, new category and message format verified

## Assumptions

- **Roslyn Version**: The implementation assumes .NET 10.0 SDK with Roslyn compiler platform version compatible with current project setup
- **Threshold Values**: Default thresholds (5 parameters, 3 nesting levels, 500 LOC, 15 methods) are industry-standard best practices and not configurable in initial version
- **Code Style**: Rules assume standard C# formatting conventions (e.g., one statement per line for LOC counting)
- **Generated Code Detection**: Auto-generated files are identified by `<auto-generated>` comment or `*.Designer.cs` naming pattern
- **Dead Code Detection Approach**: Dead Code detection uses syntax-based reference finding within same file; full semantic model analysis deferred to future enhancement for cross-file reference checking
- **XML Doc Format**: Documentation detection assumes standard C# XML doc comment format (`///` prefix, `<summary>` tag)
- **Performance**: Analysis operates on individual files in isolation (no cross-file dependency analysis required)
- **Testing Infrastructure**: xUnit and FluentAssertions are available from existing test projects

## Constraints

- **No Configuration**: Rules use hardcoded thresholds (no user-configurable severity or thresholds in this iteration)
- **No Auto-Fix**: Rules only detect and report violations; they do not provide automated code fixes
- **Single-File Analysis**: Rules analyze one file at a time; cross-file analysis (e.g., checking if a public method is called from another file) is out of scope
- **No Custom Exceptions**: Exception Swallowing rule flags all empty catch blocks; no exception type whitelisting
- **Documentation Format**: Only XML doc comments are recognized; other documentation formats (e.g., Markdown, plain comments) are not accepted
- **No IDE Integration**: Rules output diagnostics to reports; real-time IDE integration (e.g., squiggles in Visual Studio) is out of scope
- **English Messages Only**: All diagnostic messages and documentation are in English

## Dependencies

- **Roslyn Compiler Platform**: Microsoft.CodeAnalysis.CSharp (already available in project)
- **Existing Infrastructure**: IAnalyzerRule interface, DiagnosticResult class, AnalyzerEngine
- **Testing Frameworks**: xUnit 2.9.3, FluentAssertions 6.8.0 (already available)
- **Feature 002**: Rule Contract Enhancement must be complete (provides IAnalyzerRule interface)

## Out of Scope

- **Configurable Thresholds**: User-defined thresholds for parameters, LOC, nesting depth (future enhancement)
- **Auto-Fix Capabilities**: Automated code refactoring to resolve violations (future enhancement)
- **IDE Integration**: Real-time analysis in Visual Studio or Rider (future enhancement)
- **Cross-File Analysis**: Detecting unused public methods across solution boundaries (requires semantic analysis beyond single file)
- **Custom Rule Configuration**: Per-project rule enablement/disablement (future enhancement)
- **Performance Profiling**: Detailed performance metrics and optimization (beyond basic <500ms requirement)
- **Localization**: Non-English diagnostic messages and documentation (future enhancement)
- **Rule Suppression**: Mechanisms to suppress specific rule violations via attributes or comments (future enhancement)
