# Quickstart: Explicit CLI Execution Model

**Feature**: 003-explicit-cli-execution  
**Audience**: Developers implementing or testing the new CLI execution pattern  
**Time**: 5-10 minutes

## Overview

This feature replaces the implicit hosting framework pattern in `Program.cs` with an explicit build → execute → exit model, enabling in-memory testing and predictable CLI behavior.

**Before** (hosting pattern):
```csharp
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(Bootstrapper.Configure)
    .Build();
await host.RunAsync();
```

**After** (explicit pattern):
```csharp
var builder = new CliApplicationBuilder();
builder.ConfigureServices(/* ... */);
builder.AddCommand<ScanCommand>();
var app = builder.Build();
var result = app.Execute(args);
return result.ExitCode;
```

---

## Prerequisites

- .NET 10 SDK installed
- Lintelligent repository cloned on branch `003-explicit-cli-execution`
- Basic familiarity with dependency injection in .NET

---

## Step 1: Understand the New Execution Flow

The explicit execution model follows these steps:

```text
┌─────────────────────────────────────────────────────────┐
│ 1. CREATE BUILDER                                       │
│    var builder = new CliApplicationBuilder();          │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 2. CONFIGURE SERVICES (DI registration)                 │
│    builder.ConfigureServices(services => {             │
│        services.AddSingleton<AnalyzerEngine>();         │
│        services.AddSingleton<ScanCommand>();            │
│    });                                                  │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 3. REGISTER COMMANDS                                    │
│    builder.AddCommand<ScanCommand>();                   │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 4. BUILD APPLICATION                                    │
│    var app = builder.Build();                           │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 5. EXECUTE COMMAND (synchronous)                        │
│    var result = app.Execute(args);                      │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 6. RETURN EXIT CODE                                     │
│    return result.ExitCode;                              │
└─────────────────────────────────────────────────────────┘
```

---

## Step 2: Implement a Simple Command

Create a new command implementing `IAsyncCommand`:

```csharp
using Lintelligent.Cli.Commands;
using Lintelligent.Cli.Infrastructure;

namespace Lintelligent.Cli.Commands;

public class ScanCommand(AnalyzerEngine engine, ReportGenerator reporter) : IAsyncCommand
{
    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        try
        {
            // Parse arguments
            var path = args.Length > 1 ? args[1] : ".";
            
            // Perform analysis
            var provider = new FileSystemCodeProvider(path);
            var syntaxTrees = provider.GetSyntaxTrees();
            var results = engine.Analyze(syntaxTrees).ToList();
            
            // Generate report
            var report = reporter.GenerateMarkdown(results);
            
            // Return success with output
            return CommandResult.Success(report);
        }
        catch (ArgumentException ex)
        {
            // Invalid arguments → exit code 2
            return CommandResult.Failure(2, $"Invalid arguments: {ex.Message}");
        }
        catch (Exception ex)
        {
            // General error → exit code 1
            return CommandResult.Failure(1, $"Error: {ex.Message}");
        }
    }
}
```

**Key Changes from Old Pattern**:
- ❌ Remove `Task ExecuteAsync(string[])` → ✅ Use `Task<CommandResult> ExecuteAsync(string[])`
- ❌ Remove `Console.WriteLine(report)` → ✅ Return `CommandResult.Success(report)`
- ❌ Remove `await Task.CompletedTask` → ✅ Return actual CommandResult
- ✅ Wrap in try-catch to convert exceptions to exit codes

---

## Step 3: Update Program.cs

Replace hosting pattern with explicit execution:

```csharp
using Lintelligent.Cli.Infrastructure;
using Lintelligent.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;

// Build CLI application
var builder = new CliApplicationBuilder();

// Configure services (DI)
builder.ConfigureServices(services =>
{
    // Core services (singleton lifetime - shared state)
    services.AddSingleton<AnalyzerManager>();
    services.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
    services.AddSingleton<ReportGenerator>();
    
    // Commands (transient lifetime - new instance per execution)
    // Note: Commands should be registered as Transient to avoid state leakage between executions
    services.AddTransient<ScanCommand>();
    
    // Rules
    services.AddSingleton<IAnalyzerRule, LongMethodRule>();
});

// Register commands
builder.AddCommand<ScanCommand>();

// Build and execute
using var app = builder.Build();
var result = app.Execute(args);

// Output results to console (for user to see)
if (!string.IsNullOrEmpty(result.Output))
    Console.WriteLine(result.Output);

if (!string.IsNullOrEmpty(result.Error))
    Console.Error.WriteLine(result.Error);

// Return exit code to shell
return result.ExitCode;
```

**What Changed**:
1. Replaced `Host.CreateDefaultBuilder` with `CliApplicationBuilder`
2. Changed `async` entry point to synchronous `int Main(string[] args)`
3. Removed `await host.RunAsync()` → use `app.Execute(args)`
4. Explicitly output CommandResult.Output and Error to console
5. Return exit code directly
6. **Commands registered as Transient** (new instance per execution to avoid state issues)

---

## Step 4: Update Package References

Remove hosting framework dependency:

```xml
<!-- Lintelligent.Cli.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.1" />
  <!-- REMOVE: <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.1" /> -->
</ItemGroup>
```

---

## Step 5: Test In-Memory Execution

Write a test without spawning processes:

```csharp
using Lintelligent.Cli.Infrastructure;
using Lintelligent.Cli.Commands;
using Xunit;

public class ScanCommandTests
{
    [Fact]
    public void Execute_WithValidPath_ReturnsSuccessResult()
    {
        // Arrange: Build CLI app with test dependencies
        var builder = new CliApplicationBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<AnalyzerEngine>(/* mock or real */);
            services.AddSingleton<ReportGenerator>(/* mock or real */);
            services.AddSingleton<ScanCommand>();
        });
        builder.AddCommand<ScanCommand>();
        
        using var app = builder.Build();
        
        // Act: Execute in-memory (no process spawning!)
        var result = app.Execute(new[] { "scan", "./test-data" });
        
        // Assert: Verify result directly
        Assert.Equal(0, result.ExitCode);
        Assert.NotEmpty(result.Output);
        Assert.Empty(result.Error);
    }
    
    [Fact]
    public void Execute_WithInvalidArgs_ReturnsExitCode2()
    {
        // ... similar setup ...
        
        var result = app.Execute(new[] { "scan", "--invalid-flag" });
        
        Assert.Equal(2, result.ExitCode);  // Invalid arguments
        Assert.NotEmpty(result.Error);
    }
}
```

**Benefits**:
- ✅ No process spawning (tests run in <50ms)
- ✅ Direct assertion on exit codes, output, errors
- ✅ Easy to test edge cases (exceptions, invalid args)
- ✅ No parsing of console output required

---

## Step 6: Run and Verify

### Build the project
```bash
dotnet build
```

### Run the CLI
```bash
dotnet run --project src/Lintelligent.Cli -- scan ./sample-code
```

### Run tests
```bash
dotnet test
```

### Expected Output
```text
Lintelligent CLI (NET 10)

# Analysis Report
...
(report content)
...

Exit Code: 0
```

---

## Common Patterns

### Pattern 1: Command with Multiple Exit Codes

```csharp
public async Task<CommandResult> ExecuteAsync(string[] args)
{
    if (args.Length < 2)
        return CommandResult.Failure(2, "Usage: scan <path>");
    
    var path = args[1];
    if (!Directory.Exists(path))
        return CommandResult.Failure(3, $"Directory not found: {path}");
    
    // ... analysis logic ...
    
    return CommandResult.Success(report);
}
```

### Pattern 2: Command with Severity Filtering

```csharp
public async Task<CommandResult> ExecuteAsync(string[] args)
{
    var path = args[1];
    var severityFilter = ParseSeverityArg(args);  // --severity error
    
    var results = engine.Analyze(trees);
    if (severityFilter.HasValue)
        results = results.Where(r => r.Severity == severityFilter.Value);
    
    var filteredResults = results.ToList();
    var report = reporter.GenerateMarkdown(filteredResults);
    
    return CommandResult.Success(report);
}
```

### Pattern 3: Testing with Mocked Dependencies

```csharp
[Fact]
public void Execute_WithMockedEngine_ReturnsExpectedOutput()
{
    // Arrange: Mock AnalyzerEngine
    var mockEngine = new Mock<AnalyzerEngine>();
    mockEngine.Setup(e => e.Analyze(It.IsAny<IEnumerable<SyntaxTree>>()))
              .Returns(new[] { /* test diagnostic results */ });
    
    var builder = new CliApplicationBuilder();
    builder.ConfigureServices(services =>
    {
        services.AddSingleton(mockEngine.Object);
        services.AddSingleton<ReportGenerator>();
        services.AddSingleton<ScanCommand>();
    });
    builder.AddCommand<ScanCommand>();
    
    using var app = builder.Build();
    
    // Act
    var result = app.Execute(new[] { "scan", "./test" });
    
    // Assert
    Assert.Equal(0, result.ExitCode);
    mockEngine.Verify(e => e.Analyze(It.IsAny<IEnumerable<SyntaxTree>>()), Times.Once);
}
```

---

## Troubleshooting

### Error: "No command registered"
**Cause**: Forgot to call `builder.AddCommand<T>()`  
**Fix**: Add `builder.AddCommand<ScanCommand>()` before `Build()`

### Error: "Service not found"
**Cause**: Command has constructor dependency not registered in DI  
**Fix**: Register all dependencies in `ConfigureServices`

### Error: "Exit code must be between 0 and 255"
**Cause**: Attempted to return invalid exit code  
**Fix**: Use exit codes 0-255 only (0=success, 1-255=errors)

### Tests Fail: "Object reference not set to an instance"
**Cause**: Service provider not properly configured in tests  
**Fix**: Ensure all command dependencies are registered in test builder

---

## Next Steps

After implementing this feature:

1. **Verify all tests pass** (`dotnet test`)
2. **Run manual CLI smoke test** (`dotnet run -- scan ./src`)
3. **Check exit codes** in shell (`echo $?` on Linux/macOS, `echo %ERRORLEVEL%` on Windows)
4. **Review constitutional compliance** (no hosting framework, explicit execution)

For more details, see:
- [spec.md](spec.md) - Full feature specification
- [data-model.md](data-model.md) - Entity relationships and data flow
- [contracts/](contracts/) - API contracts for new types
