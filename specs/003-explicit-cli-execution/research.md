# Research: Explicit CLI Execution Model

**Feature**: 003-explicit-cli-execution  
**Phase**: 0 (Outline & Research)  
**Date**: 2025-12-23

## Research Tasks

Based on Technical Context analysis, the following areas required investigation:

1. **Builder pattern for CLI applications in .NET** - How to structure CliApplicationBuilder
2. **CommandResult design** - What properties and patterns work best for testable results
3. **Removing Microsoft.Extensions.Hosting** - Impact and migration path
4. **Async-to-sync command execution** - How CliApplication.Execute() should handle async commands
5. **Exit code conventions** - Industry standards for CLI exit codes

---

## Finding 1: Builder Pattern for CLI Applications

**Decision**: Use fluent builder pattern similar to ASP.NET Core's WebApplication.CreateBuilder but simpler.

**Rationale**: 
- **Discoverability**: IntelliSense guides usage through method chaining
- **Familiarity**: .NET developers already know this pattern from WebApplicationBuilder, HostBuilder
- **Testability**: Builder can be constructed and configured in tests without side effects
- **Simplicity**: No need for complex options patterns - direct method calls suffice

**API Shape**:
```csharp
var builder = new CliApplicationBuilder();
builder.AddCommand<ScanCommand>();
// Future: builder.ConfigureServices(...), builder.ConfigureLogging(...), etc.
var app = builder.Build();
```

**Alternatives Considered**:
- **Static factory methods** (`CliApplication.Create()`) - Rejected: Less discoverable, harder to extend
- **Options pattern** (`new CliApplication(new CliOptions { ... })`) - Rejected: Verbose, no IntelliSense guidance
- **Direct constructor** (`new CliApplication(commands)`) - Rejected: No room for future configuration extension

**References**:
- ASP.NET Core minimal hosting model: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis
- Builder pattern (Gang of Four): Separate construction from representation

---

## Finding 2: CommandResult Design

**Decision**: Immutable value object with ExitCode (int), Output (string), Error (string) properties.

**Rationale**:
- **Testability**: Tests can assert on all three properties without parsing console output
- **Separation of concerns**: Output (stdout) vs Error (stderr) mirrors process model
- **Simplicity**: No streams or TextWriters to manage - just strings
- **Immutability**: Value object pattern prevents accidental mutation after command execution

**API Shape**:
```csharp
public sealed record CommandResult(int ExitCode, string Output, string Error)
{
    public static CommandResult Success(string output) => new(0, output, string.Empty);
    public static CommandResult Failure(int exitCode, string error) => new(exitCode, string.Empty, error);
}
```

**Alternatives Considered**:
- **Single Output property** (stdout + stderr combined) - Rejected: Loses diagnostic separation
- **TextWriter properties** - Rejected: Over-engineered, complicates testing
- **Exception property** - Rejected: Exit codes + error message sufficient for CLI context

**References**:
- Process.StandardOutput/StandardError separation
- C# record types for immutable value objects (C# 9.0+)

---

## Finding 3: Removing Microsoft.Extensions.Hosting

**Decision**: Remove Microsoft.Extensions.Hosting package, keep Microsoft.Extensions.DependencyInjection.

**Rationale**:
- **Constitutional compliance**: Hosting framework violates Principle IV (Explicit Execution)
- **Minimal impact**: Only Program.cs and Bootstrapper.cs currently use hosting APIs
- **DI still needed**: Services still need registration (AnalyzerEngine, ReportGenerator, etc.)
- **Simpler model**: ServiceCollection → ServiceProvider is sufficient without IHost wrapper

**Migration Path**:
```csharp
// BEFORE (hosting pattern):
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(Bootstrapper.Configure)
    .Build();
await host.RunAsync();

// AFTER (explicit pattern):
var builder = new CliApplicationBuilder();
builder.ConfigureServices(services => {
    services.AddSingleton<AnalyzerEngine>();
    services.AddSingleton<ReportGenerator>();
    services.AddSingleton<ScanCommand>();
});
builder.AddCommand<ScanCommand>();
var app = builder.Build();
var result = app.Execute(args);
return result.ExitCode;
```

**Impact**:
- **Breaking**: Program.Main() signature changes from implicit async to `static int Main(string[] args)`
- **Dependencies**: Remove `Microsoft.Extensions.Hosting` package reference from Lintelligent.Cli.csproj
- **Benefits**: ~500KB reduction in deployment size, faster startup, simpler mental model

**Alternatives Considered**:
- **Keep hosting but wrap it** - Rejected: Doesn't solve constitutional violation
- **Remove DI entirely** - Rejected: DI is allowed in CLI layer, provides value for service composition

---

## Finding 4: Async-to-Sync Command Execution

**Decision**: CliApplication.Execute() handles async internally; commands can implement async or sync versions of their execution logic.

**Rationale**:
- **Caller simplicity**: Program.Main() is synchronous, no async/await at entry point
- **Command flexibility**: Commands can use async I/O (file reading, HTTP) without Task.Wait() in their code
- **Blocking acceptable**: CLI commands are finite operations where blocking until completion is expected behavior
- **No deadlock risk**: No SynchronizationContext in console apps (unlike UI frameworks)

**API Shape**:
```csharp
public sealed class CliApplication
{
    public CommandResult Execute(string[] args)
    {
        var command = ResolveCommand(args);
        
        // If command is async, await it synchronously
        if (command is IAsyncCommand asyncCmd)
        {
            return asyncCmd.ExecuteAsync(args).GetAwaiter().GetResult();
        }
        
        // Synchronous commands execute directly
        return ((ICommand)command).Execute(args);
    }
}
```

**Alternatives Considered**:
- **Force commands to be synchronous** - Rejected: Unrealistic for I/O-bound operations
- **Make Execute() async** - Rejected: Leaks async to Program.Main(), violates explicit execution principle
- **Use Task.Run()** - Rejected: Unnecessary overhead, doesn't change blocking behavior

**References**:
- Stephen Cleary's "Don't Block on Async Code" (doesn't apply to console apps without SyncContext)
- .NET console app behavior: No SynchronizationContext by default

---

## Finding 5: Exit Code Conventions

**Decision**: Base contract with 0=success, 1=general error, 2=invalid arguments, 3+ for command-specific errors.

**Rationale**:
- **POSIX compatibility**: 0=success is universal; 1-2 are common standards
- **Shell scripting**: Scripts check `$?` (exit code) to determine success/failure
- **Extensibility**: Commands can define domain-specific codes (e.g., 3=file not found, 4=parse error)
- **Simplicity**: Small fixed base, infinite extension space

**Mapping**:
- **0**: Success (operation completed without errors)
- **1**: General error (unhandled exception, unexpected failure)
- **2**: Invalid arguments (ArgumentException, missing required args)
- **3+**: Command-specific (defined per command as needed)

**Exception Handling**:
```csharp
public CommandResult Execute(string[] args)
{
    try
    {
        var command = ResolveCommand(args);
        return command.Execute(args);
    }
    catch (ArgumentException ex)
    {
        return CommandResult.Failure(2, ex.Message); // Invalid arguments
    }
    catch (Exception ex)
    {
        return CommandResult.Failure(1, ex.Message); // General error
    }
}
```

**Alternatives Considered**:
- **Only 0 and 1** - Rejected: Loses valuable diagnostic information (invalid args vs runtime error)
- **HTTP-style codes** (200, 400, 500) - Rejected: Confusing in CLI context, not shell-script friendly
- **No exception mapping** (let exceptions propagate) - Rejected: Violates explicit execution model

**References**:
- POSIX exit codes: https://tldp.org/LDP/abs/html/exitcodes.html
- Advanced Bash-Scripting Guide: Exit codes with special meanings (0-2 are standard)

---

## Summary

All NEEDS CLARIFICATION items from Technical Context have been resolved:

| Unknown | Resolution |
|---------|------------|
| Builder API design | Fluent builder pattern (AddCommand<T>, Build) |
| CommandResult structure | Record with ExitCode, Output, Error properties |
| Hosting removal impact | Remove Microsoft.Extensions.Hosting, keep DI; minimal migration effort |
| Async command handling | CliApplication.Execute() awaits async commands internally using GetAwaiter().GetResult() |
| Exit code scheme | 0=success, 1=error, 2=invalid args, 3+=command-specific (clarified in spec) |

**Next Phase**: Phase 1 - Design (data-model.md, contracts/, quickstart.md)
