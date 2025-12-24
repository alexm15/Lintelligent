# Feature Specification: Explicit CLI Execution Model

**Feature Branch**: `003-explicit-cli-execution`  
**Created**: 2025-12-23  
**Status**: Draft  
**Input**: User description: "Explicit CLI Execution Model - Implement CliApplicationBuilder pattern with explicit build → execute → exit flow for predictable, testable CLI behavior without hosting framework overhead."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Explicit Build-Execute Flow (Priority: P1)

As a CLI application developer, I want an explicit build → execute → exit pattern so that the application lifecycle is predictable, synchronous, and testable without hidden async hosting magic.

**Why this priority**: This is the core constitutional requirement (Principle IV). Without explicit execution control, the CLI remains coupled to hosting frameworks, making testing difficult and behavior unpredictable. This is foundational for all other CLI improvements.

**Independent Test**: Can be fully tested by creating a CliApplicationBuilder, building the application, executing a command, and verifying the exit code is returned synchronously without any background tasks or implicit async flows.

**Acceptance Scenarios**:

1. **Given** a CLI application entry point, **When** Program.Main() executes, **Then** the flow must be: (1) Build application, (2) Execute command, (3) Return exit code - all synchronously
2. **Given** a command execution completes, **When** the result is returned, **Then** an integer exit code (0 for success, non-zero for failure) is immediately available
3. **Given** no hosting framework is present, **When** building the application, **Then** all dependencies are explicitly configured without reliance on DI containers or host builders

---

### User Story 2 - Testable Command Execution (Priority: P1)

As a test engineer, I want to execute CLI commands programmatically and capture their results so that I can write fast, isolated tests without spawning processes or parsing console output.

**Why this priority**: Testability is the primary benefit of explicit execution. Current CLI tests likely spawn processes or rely on host mocking, making them slow and fragile. Explicit execution enables in-memory testing.

**Independent Test**: Can be tested by creating a CliApplication in a test, executing a command with test inputs, and asserting on the CommandResult object (exit code, output, errors) - all in-memory, no process spawning.

**Acceptance Scenarios**:

1. **Given** a CliApplication instance in a test, **When** executing a command with specific arguments, **Then** the test can capture the CommandResult synchronously
2. **Given** a command execution fails, **When** the result is returned, **Then** the exit code is non-zero and error details are accessible
3. **Given** a test needs to verify command output, **When** the command executes, **Then** output is captured in the CommandResult without writing to the actual console

---

### User Story 3 - No Implicit Async Hosting (Priority: P2)

As a CLI developer, I want to eliminate all implicit async hosting patterns so that the application has a clear, synchronous execution path and avoids the complexity of async/await for simple command execution.

**Why this priority**: While important for simplicity and maintainability, this is secondary to establishing the explicit execution pattern. It's a cleanup step that ensures constitutional compliance.

**Independent Test**: Can be tested by verifying Program.Main() signature is `int Main(string[] args)` (not async Task<int>), and no hosting framework types (IHost, HostBuilder) appear in dependency references.

**Acceptance Scenarios**:

1. **Given** the Program.cs entry point, **When** reviewing the Main signature, **Then** it must be `static int Main(string[] args)` (synchronous)
2. **Given** the CLI project dependencies, **When** checking package references, **Then** no Microsoft.Extensions.Hosting packages are present
3. **Given** command execution completes, **When** returning to the caller, **Then** no async continuations or background tasks remain running

---

### User Story 3 - [Brief Title] (Priority: P3)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### Edge Cases

1. **Builder Reuse**: What if CliApplicationBuilder.Build() is called multiple times on the same instance? → Throws InvalidOperationException after first Build() call (builder transitions to Built state)
2. **Duplicate Commands**: What if AddCommand<T>() is called multiple times for the same command type? → Last registration wins (standard ServiceCollection behavior)
3. **ConfigureServices Exceptions**: What if the ConfigureServices delegate throws an exception? → Exception propagates to caller (no try-catch in builder)
4. **Unrecognized Command**: What if Execute(args) is called with args[0] not matching any registered command? → Returns CommandResult with exit code 2 (invalid arguments)
5. **Zero Commands Registered**: What if Build() is called without any AddCommand<T>() calls? → Allowed; Execute() returns exit code 2 for any args
6. **Empty Arguments**: What if Execute() is called with empty array (Execute([]))? → Returns CommandResult with exit code 2 (invalid arguments - no command specified)
7. **Null Arguments**: What if Execute(null) is called? → Throws ArgumentNullException immediately
8. **Command Execution Exceptions**: What happens when a command throws an unhandled exception? → Caught and converted to exit code per FR-009 (ArgumentException/derived → 2, others → 1), exception.Message stored in CommandResult.Error
9. **DI Resolution Failures**: What if command constructor dependencies cannot be resolved? → Exception caught during Execute(), maps to exit code 1, error details in CommandResult.Error
10. **Build() Exceptions**: What if CliApplicationBuilder.Build() throws during service provider construction? → Exception propagates to caller (validation errors, service registration issues)
11. **Exit Code Validation**: What if command attempts to return exit code >255? → CommandResult constructor validates 0-255 range, throws ArgumentOutOfRangeException
12. **Async Command Execution**: How does the system handle commands that use async I/O internally? → IAsyncCommand.ExecuteAsync() is awaited using GetAwaiter().GetResult(); AggregateException automatically unwrapped to first inner exception

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a CliApplicationBuilder class that follows the builder pattern for constructing CLI applications
- **FR-002**: CliApplicationBuilder MUST have an explicit Build() method that returns a CliApplication instance
- **FR-003**: CliApplication MUST have an Execute(string[] args) method that runs the appropriate command and returns a CommandResult
- **FR-004**: CommandResult MUST contain an integer exit code following the base contract: 0=success, 1=general error, 2=invalid arguments, with codes 3+ available for command-specific errors
- **FR-005**: Program.Main() entry point MUST be synchronous (signature: `static int Main(string[] args)`)
- **FR-006**: Command execution MUST be synchronous from the caller's perspective (Execute() returns immediately with result, awaiting any async command execution internally)
- **FR-007**: System MUST NOT depend on Microsoft.Extensions.Hosting or any hosting framework
- **FR-008**: CliApplicationBuilder MUST support registering commands via AddCommand<TCommand>() where TCommand : ICommand (generic method, no auto-discovery)
- **FR-009**: Unhandled exceptions during command execution MUST be caught and converted to exit codes (ArgumentException and all derived types → 2, all other exceptions → 1) with exception.Message stored in CommandResult.Error property (stack traces excluded)
- **FR-010**: CommandResult MUST contain string Output and Error properties for in-memory test verification (separate stdout/stderr streams, no process spawning required)

### Key Entities

- **CliApplicationBuilder**: Builder for constructing CLI applications with explicit command registration and configuration
- **CliApplication**: Represents a configured CLI application with an Execute method for running commands
- **CommandResult**: Value object containing exit code (int), Output (string), and Error (string) properties from command execution
- **ICommand**: Interface that commands implement (existing - may need updates for explicit execution)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Program.Main() signature is synchronous (`int Main(string[] args)`) with no async/await at entry point
- **SC-002**: CLI tests execute in-memory without spawning processes (test execution time <50ms per command)
- **SC-003**: Zero dependencies on Microsoft.Extensions.Hosting or related hosting packages
- **SC-004**: 100% of commands return explicit exit codes (0 for success, 1+ for various failure types)
- **SC-005**: CliApplication instances are created via explicit builder pattern (no implicit factory/DI container)
- **SC-006**: All command registrations are explicit in Program.cs (no reflection-based auto-discovery)

## Clarifications

### Session 2025-12-23

- Q: What exit code scheme should be used - fixed set of codes or extensible contract? → A: Base contract with 0=success, 1=general error, 2=invalid args, then allow commands to define 3+ for domain errors
- Q: Who is responsible for synchronizing async commands - CliApplication.Execute() or individual commands? → A: CliApplication.Execute() is responsible - commands can be async, but Execute() awaits completion before returning CommandResult
- Q: What output capture mechanism should CommandResult provide for testing? → A: CommandResult contains separate Output and Error properties (stdout/stderr streams)
- Q: How should unhandled exceptions map to exit codes? → A: ArgumentException → code 2, all others → code 1, store exception message in CommandResult.Error
- Q: What API should be used for command registration in the builder? → A: builder.AddCommand<TCommand>() where TCommand : ICommand (generic method with type parameter)

## Assumptions *(optional)*

### Defaults and Design Decisions

- **Exit Code Contract**: Base codes are 0=success, 1=general error, 2=invalid arguments. Commands may define exit codes 3+ for domain-specific errors (e.g., 3=file not found, 4=parse error). The contract guarantees 0 means success and non-zero means failure.
- **Command Interface**: Existing ICommand interface can be adapted to return CommandResult (or status code)
- **Output Handling**: Commands write to abstractions (TextWriter) not Console directly, enabling test capturing
- **Builder Immutability**: CliApplicationBuilder is mutable during configuration, CliApplication is immutable after Build()
- **Single Command Execution**: Execute() runs one command per invocation (no command chaining initially)

### Out of Scope

- **Async Command Support**: Commands may use async internally; CliApplication.Execute() ensures completion before returning (no Task.Wait needed in commands)
- **Middleware/Pipeline**: No middleware pattern initially - commands execute directly
- **Dependency Injection Container**: No IoC container - dependencies are constructor-injected explicitly
- **Configuration System**: No complex configuration loading - commands receive parsed arguments only
- **Logging Framework**: No logging initially (can be added later via constructor injection)

## Dependencies

- **Existing Code**: Current ScanCommand implementation will need refactoring to fit new execution model
- **Breaking Change**: This changes the CLI entry point and command execution pattern (not backward compatible)
- **Constitutional Alignment**: Directly implements Principle IV (Explicit Execution Model) from project constitution

## Risks & Mitigations

- **Risk**: Commands may have async I/O that's difficult to synchronize
  - **Mitigation**: Commands can use async internally; Execute() ensures completion before returning
  
- **Risk**: Loss of hosting framework benefits (logging, config, DI)
  - **Mitigation**: Benefits weren't being used; explicit patterns provide same capabilities with more control
  
- **Risk**: Breaking change disrupts existing workflows
  - **Mitigation**: Single command (ScanCommand) to migrate; no external consumers affected

## Notes

- This feature directly implements **Constitutional Principle IV**: "Explicit Execution Model - CLI applications must follow explicit build → execute → exit pattern without hosting framework magic"
- The builder pattern provides discoverability (IntelliSense guides usage) while maintaining explicit control
- Synchronous execution doesn't prevent commands from using async I/O internally - it just ensures the caller gets results synchronously
- This pattern is similar to ASP.NET Core's WebApplication.CreateBuilder but without the hosting complexity

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]
