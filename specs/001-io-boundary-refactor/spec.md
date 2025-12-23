# Feature Specification: Refactor AnalyzerEngine IO Boundary

**Feature Branch**: `001-io-boundary-refactor`  
**Created**: 2025-12-22  
**Status**: Draft  
**Input**: User description: "Refactor AnalyzerEngine IO Boundary - Remove IO operations from AnalyzerEngine, introduce abstraction for file system access"

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

### User Story 1 - In-Memory Testing of Analysis Rules (Priority: P1)

As a rule developer, I want to test my analyzer rules against in-memory syntax trees without requiring physical files on disk, so I can write fast, isolated unit tests that don't depend on file system state.

**Why this priority**: This is the core constitutional violation that must be fixed. Testing is a foundational requirement (Principle V), and the current IO coupling prevents proper unit testing of the AnalyzerEngine.

**Independent Test**: Can be fully tested by creating an in-memory collection of syntax trees, passing them to the refactored AnalyzerEngine, and verifying analysis results without any file system operations.

**Acceptance Scenarios**:

1. **Given** a collection of in-memory SyntaxTree objects, **When** I pass them to AnalyzerEngine.Analyze(), **Then** the engine analyzes all trees and returns diagnostic results without touching the file system
2. **Given** an empty collection of syntax trees, **When** I pass it to AnalyzerEngine.Analyze(), **Then** the engine returns an empty result set without errors
3. **Given** syntax trees with various rule violations, **When** I analyze them, **Then** results are identical regardless of whether trees came from files or memory

---

### User Story 2 - File System Code Provider for CLI (Priority: P2)

As a CLI user, I want the scan command to discover and analyze all C# files in my project directory, so I can run analysis on real codebases from the command line.

**Why this priority**: This enables the existing CLI functionality to continue working after the refactor, but it's built on top of the abstraction created in US1.

**Independent Test**: Can be tested by pointing the CLI at a directory with C# files and verifying that all files are discovered and analyzed, producing the expected diagnostic output.

**Acceptance Scenarios**:

1. **Given** a directory path containing .cs files, **When** FileSystemCodeProvider discovers files, **Then** it returns SyntaxTree objects for all .cs files recursively
2. **Given** a directory with no .cs files, **When** FileSystemCodeProvider discovers files, **Then** it returns an empty collection without errors
3. **Given** a directory with mixed file types, **When** FileSystemCodeProvider runs, **Then** it only processes .cs files and ignores others
4. **Given** a path to a single .cs file (not a directory), **When** FileSystemCodeProvider processes it, **Then** it returns a single SyntaxTree for that file

---

### User Story 3 - Alternative Code Providers for IDE Integration (Priority: P3)

As an IDE plugin developer, I want to provide syntax trees from the IDE's in-memory representation of open documents, so analysis can run on unsaved changes without requiring file system access.

**Why this priority**: This demonstrates extensibility for future IDE integrations (Principle VI), but it's not needed for the immediate refactor to work.

**Independent Test**: Can be tested by creating a mock IDE code provider that yields syntax trees from an in-memory editor buffer, verifying that AnalyzerEngine processes them identically to file-based trees.

**Acceptance Scenarios**:

1. **Given** an ICodeProvider implementation that yields in-memory editor buffers, **When** AnalyzerEngine processes them, **Then** analysis results reflect the in-memory content, not the saved file content
2. **Given** a code provider that filters files by criteria (e.g., only modified files), **When** AnalyzerEngine runs, **Then** only the filtered set is analyzed
3. **Given** multiple different ICodeProvider implementations, **When** they are swapped at runtime, **Then** AnalyzerEngine behavior remains consistent

---

### Edge Cases

- What happens when a code provider yields a null or invalid SyntaxTree? (Should be skipped with appropriate logging/error handling in CLI layer)
- What happens when analyzing a very large collection of syntax trees (100k+ files)? (Should handle gracefully with streaming/yield patterns, not load all into memory)
- What happens if a file is deleted between discovery and analysis in FileSystemCodeProvider? (Should handle FileNotFoundException gracefully)
- What happens when the same file path appears multiple times from a code provider? (Should analyze each instance independently - deduplication is provider's responsibility)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: AnalyzerEngine MUST NOT perform any file system IO operations (read, write, enumerate files/directories)
- **FR-002**: AnalyzerEngine MUST accept an enumerable collection of SyntaxTree objects as input for analysis
- **FR-003**: AnalyzerEngine MUST process syntax trees in a streaming fashion (yield results, don't require full collection in memory)
- **FR-004**: System MUST provide an ICodeProvider abstraction for discovering and providing syntax trees
- **FR-005**: FileSystemCodeProvider MUST be implemented in the CLI layer (not in AnalyzerEngine project)
- **FR-006**: FileSystemCodeProvider MUST support both directory paths and single file paths
- **FR-007**: FileSystemCodeProvider MUST recursively discover all .cs files in a given directory
- **FR-008**: FileSystemCodeProvider MUST handle common file system errors gracefully (access denied, file not found, path too long)
- **FR-009**: ICodeProvider interface MUST return IEnumerable<SyntaxTree> to support lazy evaluation
- **FR-010**: AnalyzerEngine constructor MUST accept only stateless dependencies (AnalyzerManager) - NO file paths or IO configuration
- **FR-011**: Refactored architecture MUST maintain 100% backward compatibility with existing CLI scan command behavior
- **FR-012**: All existing tests MUST continue to pass after refactor (or be updated to use new architecture)

### Key Entities

- **ICodeProvider**: Abstraction for discovering and providing code for analysis; implementations may read from file system, memory, IDE buffers, network, etc.; yields SyntaxTree objects with associated metadata (file path, encoding)
- **FileSystemCodeProvider**: Concrete implementation that discovers .cs files from disk; accepts a root path (file or directory); handles file enumeration, reading, and parsing to SyntaxTree
- **AnalyzerEngine**: Core analysis orchestrator; accepts IEnumerable<SyntaxTree> and AnalyzerManager; yields DiagnosticResult objects for each rule violation found
- **AnalyzerManager**: Existing component that manages rule execution; unchanged by this refactor
- **SyntaxTree**: Roslyn's representation of parsed code; used as the common unit of analysis

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: AnalyzerEngine can be unit tested with in-memory syntax trees without any file system dependencies (0 file IO operations during core engine tests)
- **SC-002**: All existing CLI integration tests pass after refactor with identical or improved performance (±5% execution time tolerance)
- **SC-003**: Code coverage for AnalyzerEngine increases to ≥90% due to improved testability
- **SC-004**: FileSystemCodeProvider successfully analyzes projects with 10,000+ files without memory exhaustion or errors
- **SC-005**: Refactored architecture enables future IDE plugin development where code providers yield in-memory editor buffers (proven by mock implementation in test suite)
- **SC-006**: Constitution Principle I violation is resolved - AnalyzerEngine project has zero dependencies on System.IO for file operations
- **SC-007**: Analysis results are deterministic - same syntax trees yield identical results regardless of code provider implementation

### Assumptions

- SyntaxTree parsing continues to be handled by Roslyn APIs (CSharpSyntaxTree.ParseText)
- File encoding detection uses default UTF-8 with BOM detection (no custom encoding configuration needed initially)
- File discovery patterns are limited to "*.cs" (no configurable glob patterns in this iteration)
- Error handling strategy: FileSystemCodeProvider logs errors and skips problematic files rather than failing entire analysis
- Performance baseline: Current implementation's analysis speed is acceptable; refactor should not degrade performance by more than 5%
