# Data Model: Explicit CLI Execution Model

**Feature**: 003-explicit-cli-execution  
**Phase**: 1 (Design & Contracts)  
**Date**: 2025-12-23

## Core Entities

### CliApplicationBuilder

**Purpose**: Fluent builder for constructing CLI applications with explicit command and service registration.

**Fields**:
- `_services`: IServiceCollection - DI container for service registration
- `_commands`: Dictionary<string, Type> - Maps command names to command types

**Relationships**:
- **Produces**: CliApplication (via Build() method)
- **Registers**: ICommand implementations (via AddCommand<T>)
- **Configures**: IServiceCollection (internal, exposed via Build)

**State Transitions**:
1. **Created** → AddCommand/ConfigureServices called (mutable configuration phase)
2. **Build called** → Produces CliApplication instance (builder remains reusable)

**Validation Rules**:
- At least one command must be registered before Build()
- Command types must implement ICommand interface
- Duplicate command registrations are allowed (last registration wins)

---

### CliApplication

**Purpose**: Configured CLI application that executes commands and returns results synchronously.

**Fields**:
- `_serviceProvider`: IServiceProvider - Resolved DI container
- `_commands`: IReadOnlyDictionary<string, Type> - Registered command mappings

**Relationships**:
- **Created by**: CliApplicationBuilder.Build()
- **Executes**: ICommand instances (resolved from service provider)
- **Returns**: CommandResult (execution outcome)

**State Transitions**:
1. **Built** → Immutable; ready for Execute() calls
2. **Execute() called** → Resolves command, runs it, returns CommandResult
3. **Disposed** (if IDisposable) → Cleans up service provider

**Validation Rules**:
- Must be created via CliApplicationBuilder (no public constructor)
- Execute() can be called multiple times (stateless execution)
- Immutable after Build() - configuration changes require new builder

---

### CommandResult

**Purpose**: Immutable value object representing command execution outcome.

**Fields**:
- `ExitCode`: int - Exit code (0=success, 1=general error, 2=invalid args, 3+=command-specific)
- `Output`: string - Standard output content (stdout)
- `Error`: string - Error output content (stderr)

**Relationships**:
- **Returned by**: CliApplication.Execute()
- **Consumed by**: Program.Main() (extracts ExitCode for process exit)
- **Tested by**: Unit/integration tests (assert on all properties)

**State Transitions**:
- **Immutable** - Created once per command execution, never modified

**Validation Rules**:
- ExitCode must be 0-255 (valid process exit code range)
- Output and Error can be empty strings but never null
- Success scenario: ExitCode=0, Error=string.Empty
- Failure scenario: ExitCode>0, Output typically empty

**Factory Methods**:
- `CommandResult.Success(string output)` → (0, output, "")
- `CommandResult.Failure(int exitCode, string error)` → (exitCode, "", error)

---

### ICommand Interface (Existing - Updated)

**Purpose**: Contract for executable CLI commands.

**Current State** (inferred from ScanCommand):
```csharp
// ScanCommand currently has ExecuteAsync(string[] args) : Task
// No formal ICommand interface exists yet
```

**Proposed Update**:
```csharp
public interface ICommand
{
    CommandResult Execute(string[] args);
}

public interface IAsyncCommand  // Optional: for commands needing async I/O
{
    Task<CommandResult> ExecuteAsync(string[] args);
}
```

**Relationships**:
- **Implemented by**: ScanCommand (and future commands)
- **Resolved by**: CliApplication (via service provider)
- **Registered in**: CliApplicationBuilder.AddCommand<T>()

**Validation Rules**:
- Commands must return CommandResult (not void or Task)
- Commands must accept string[] args (no other parameters in Execute signature)
- Commands should be registered as services in DI container

---

## Entity Diagram

```text
┌─────────────────────────┐
│ CliApplicationBuilder   │
│                         │
│ + AddCommand<T>()       │
│ + ConfigureServices()   │
│ + Build()               │
└────────────┬────────────┘
             │ Build()
             ▼
┌─────────────────────────┐           ┌─────────────────────┐
│ CliApplication          │           │ CommandResult       │
│                         │ Execute() │                     │
│ + Execute(string[])     │──────────▶│ + ExitCode: int     │
│                         │  returns  │ + Output: string    │
└────────────┬────────────┘           │ + Error: string     │
             │ resolves                └─────────────────────┘
             ▼
┌─────────────────────────┐
│ ICommand / ScanCommand  │
│                         │
│ + Execute(string[])     │
│   returns CommandResult │
└─────────────────────────┘
```

---

## Migration Impact

### Entities Modified
- **ScanCommand**: 
  - Change from `Task ExecuteAsync(string[])` to `CommandResult Execute(string[])` or `Task<CommandResult> ExecuteAsync(string[])`
  - Replace Console.WriteLine with output capturing (return CommandResult with Output property)
  - Remove await Task.CompletedTask (no longer needed)

### Entities Created
- **CliApplicationBuilder** (new class in Lintelligent.Cli/Infrastructure/)
- **CliApplication** (new class in Lintelligent.Cli/Infrastructure/)
- **CommandResult** (new record in Lintelligent.Cli/Infrastructure/)
- **ICommand** (new interface in Lintelligent.Cli/Commands/)
- **IAsyncCommand** (new interface in Lintelligent.Cli/Commands/, optional)

### Entities Removed
- None (but hosting framework usage in Program.cs is removed)

---

## Data Flow

### Initialization Flow
```text
1. Program.Main(args) starts
2. new CliApplicationBuilder() creates builder
3. builder.ConfigureServices(...) registers AnalyzerEngine, ReportGenerator, ScanCommand
4. builder.AddCommand<ScanCommand>() maps "scan" → ScanCommand type
5. builder.Build() creates IServiceProvider, constructs CliApplication
```

### Execution Flow
```text
1. app.Execute(args) receives ["scan", "path/to/code", "--severity", "error"]
2. CliApplication parses command name ("scan") from args[0]
3. Resolves ScanCommand from service provider
4. Calls command.Execute(args) or awaits command.ExecuteAsync(args) internally
5. ScanCommand:
   a. Parses args (path, --severity filter)
   b. Creates FileSystemCodeProvider
   c. Calls AnalyzerEngine.Analyze()
   d. Generates report via ReportGenerator
   e. Returns CommandResult(exitCode: 0, output: report, error: "")
6. CliApplication returns CommandResult to caller
7. Program.Main returns result.ExitCode
```

### Exception Handling Flow
```text
1. CliApplication.Execute() wraps command execution in try-catch
2. If ArgumentException → CommandResult(exitCode: 2, error: ex.Message)
3. If any other Exception → CommandResult(exitCode: 1, error: ex.Message)
4. CommandResult propagates up to Program.Main
5. Exit code returned to shell/OS
```

---

## Validation & Invariants

### CliApplicationBuilder
- **Invariant**: Must have at least one registered command before Build()
- **Validation**: Build() throws InvalidOperationException if _commands.Count == 0

### CliApplication
- **Invariant**: Service provider must be non-null and contain all registered commands
- **Validation**: Constructor asserts serviceProvider != null

### CommandResult
- **Invariant**: ExitCode must be in range 0-255 (valid process exit codes)
- **Validation**: Constructor validates exitCode range, throws ArgumentOutOfRangeException if invalid
- **Invariant**: Output and Error strings must never be null (can be empty)
- **Validation**: Constructor replaces null with string.Empty

### ICommand Implementations
- **Invariant**: Must return CommandResult (not throw for expected failures)
- **Validation**: Enforced by interface contract; unexpected exceptions caught by CliApplication.Execute()

---

## Notes

- **Immutability**: CliApplication and CommandResult are immutable after creation
- **Testability**: All entities can be constructed and tested in isolation
- **No IO in entities**: File system access remains in ScanCommand via FileSystemCodeProvider (preserves layering)
- **DI scoping**: Service provider uses singleton lifetime for all services (no scoped services in CLI context)
