# Lintelligent

A static code analysis CLI tool for C# projects that detects code quality issues and provides actionable insights.

## Features

- 🔍 **Configurable Rules**: Analyze C# code with extensible analyzer rules
- 🔄 **Code Duplication Detection**: Identify exact duplicated code blocks (whole files and sub-blocks within methods)
- 🧬 **Monad Detection**: Suggest functional patterns (Option<T>, Either<L,R>, Validation<T>) for safer error handling
- ⚡ **Severity Filtering**: Filter analysis results by Error/Warning/Info levels
- 📊 **Category Grouping**: Organize findings by category (Maintainability, Security, Performance, etc.)
- 🎯 **Multiple Findings**: Rules report all violations in a file, not just the first
- 🚀 **High Performance**: Handles large codebases (10,000+ files) with streaming architecture
- 🧪 **Testable Core**: Framework-agnostic engine with no file system dependencies
- 🔌 **Extensible**: Plugin-friendly architecture for IDE integration and custom frontends
- 🛡️ **Resilient**: Exception handling ensures one faulty rule doesn't crash entire analysis
- 🔬 **Roslyn Analyzer**: Build-time analysis with instant IDE feedback (zero additional tools)
- 🌐 **Workspace Analysis**: Cross-file analysis in Roslyn analyzer (duplication detection in IDE)

## Installation

### CLI Tool

```bash
dotnet build
```

### Roslyn Analyzer (NuGet Package)

For instant IDE feedback and build-time analysis:

```bash
dotnet add package Lintelligent.Analyzers
```

The Roslyn analyzer automatically integrates with your IDE (Visual Studio, Rider, VS Code) and provides:
- Real-time diagnostics as you type
- Code navigation (F8 to jump between issues)
- EditorConfig support for per-project rule configuration
- Zero build overhead (<2s for 100-file solutions)
- **Workspace-level analysis**: Cross-file duplication detection (requires .NET 6+)

See the [Analyzer Guide](./specs/019-roslyn-analyzer-bridge/ANALYZER_GUIDE.md) for configuration options.

## Quick Start

### Basic Usage

Analyze a C# project or file:

```bash
cd src/Lintelligent.Cli
dotnet run -- scan /path/to/project
```

Analyze with severity filtering (show only errors):

```bash
dotnet run -- scan /path/to/project --severity Error
```

Analyze with multiple severity levels:

```bash
dotnet run -- scan /path/to/project --severity Error,Warning
```

Group results by category for organized reports:

```bash
dotnet run -- scan /path/to/project --group-by category
```

Combine filtering and grouping:

```bash
dotnet run -- scan /path/to/project --severity Error,Warning --group-by category
```

Analyze a single file:

```bash
dotnet run -- scan /path/to/file.cs
```

Detect code duplication (whole files and sub-blocks within methods):

```bash
dotnet run -- scan /path/to/project  # LNT100 warnings show duplicated code
```

### Code Duplication Detection

Lintelligent detects exact code duplication at two levels:

1. **Whole-file duplication**: Identical files across your codebase
2. **Sub-block duplication**: Duplicated statement sequences (3+ statements) within methods

This helps identify:
- Copy-pasted validation logic
- Repeated error handling patterns
- Duplicated transaction/database code
- Repeated setup/teardown code in tests

**Configuration** (via `.lintelligent.json` or `.editorconfig`):
- `MinLines`: Minimum lines for whole-file duplication (default: 5)
- `MinTokens`: Minimum tokens for sub-block duplication (default: 50)

For detailed documentation, see [Code Duplication Detection Guide](./specs/020-code-duplication/README.md).

### CLI Execution Model

Lintelligent uses an explicit build → execute → exit pattern based on Constitutional Principle IV:

```csharp
using Lintelligent.Cli.Infrastructure;
using Lintelligent.Cli.Commands;

// 1. Create builder
var builder = new CliApplicationBuilder();

// 2. Configure services (dependency injection)
builder.ConfigureServices(services =>
{
    services.AddSingleton<AnalyzerManager>();
    services.AddTransient<ScanCommand>();  // Commands are transient
});

// 3. Register commands
builder.AddCommand<ScanCommand>();

// 4. Build application
using var app = builder.Build();

// 5. Execute command synchronously
var result = app.Execute(args);

// 6. Output results
if (!string.IsNullOrEmpty(result.Output))
    Console.WriteLine(result.Output);

if (!string.IsNullOrEmpty(result.Error))
    Console.Error.WriteLine(result.Error);

// 7. Exit with result code
return result.ExitCode;
```

**Key Benefits:**
- ✅ **Explicit Execution**: No hidden background tasks or async hosting
- ✅ **Testable**: CommandResult enables in-memory testing without process spawning
- ✅ **Synchronous**: Main() exits immediately after command completes
- ✅ **Clear Error Handling**: Exceptions map to standard exit codes (2 = invalid args, 1 = error)

### Programmatic Usage

#### CLI Integration (File System)

```csharp
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.Cli.Providers;

// 1. Set up analyzer with rules
var manager = new AnalyzerManager();
manager.RegisterRule(new LongMethodRule());

var engine = new AnalyzerEngine(manager);

// 2. Create file system code provider
var provider = new FileSystemCodeProvider("/path/to/project");

// 3. Analyze and get results
var results = engine.Analyze(provider.GetSyntaxTrees());

foreach (var diagnostic in results)
{
    Console.WriteLine($"{diagnostic.FilePath}:{diagnostic.LineNumber} - {diagnostic.Message}");
}
```

#### In-Memory Testing

```csharp
using Microsoft.CodeAnalysis.CSharp;

// Parse source code directly - no file system required
var sourceCode = @"
public class Example
{
    public void LongMethod()
    {
        // 100+ lines of code...
    }
}";

var tree = CSharpSyntaxTree.ParseText(sourceCode, path: "Example.cs");

var manager = new AnalyzerManager();
manager.RegisterRule(new LongMethodRule());
var engine = new AnalyzerEngine(manager);

var results = engine.Analyze(new[] { tree });
// Fast, isolated, deterministic testing
```

#### IDE Integration (In-Memory Buffers)

```csharp
using Lintelligent.AnalyzerEngine.Tests.TestUtilities;

// Analyze unsaved editor buffers
var editorBuffers = new Dictionary<string, string>
{
    ["Controller.cs"] = editorApi.GetBufferContent("Controller.cs"),
    ["Service.cs"] = editorApi.GetBufferContent("Service.cs")
};

var provider = new InMemoryCodeProvider(editorBuffers);

var manager = new AnalyzerManager();
manager.RegisterRule(new LongMethodRule());
var engine = new AnalyzerEngine(manager);

var results = engine.Analyze(provider.GetSyntaxTrees());
// Analyze code as you type, before saving
```

#### Selective Analysis (Filtering)

```csharp
using Lintelligent.AnalyzerEngine.Tests.TestUtilities;

// Analyze only modified files in a project
var baseProvider = new FileSystemCodeProvider("/path/to/project");

var modifiedFilesProvider = new FilteringCodeProvider(
    baseProvider,
    tree => modifiedFiles.Contains(tree.FilePath));

var results = engine.Analyze(modifiedFilesProvider.GetSyntaxTrees());
// Only analyze what changed - faster incremental analysis
```

## Architecture

### ICodeProvider Abstraction

Lintelligent uses the **Strategy Pattern** to decouple code analysis from code sources:

```
┌─────────────────────────────────────────────────────────────┐
│                        Your Application                      │
└─────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                      ICodeProvider Interface                 │
│  (Provides: IEnumerable<SyntaxTree> GetSyntaxTrees())       │
└─────────────────────────────────────────────────────────────┘
                               │
          ┌────────────────────┼────────────────────┐
          ▼                    ▼                    ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ FileSystemCode   │  │ InMemoryCode     │  │ FilteringCode    │
│ Provider         │  │ Provider         │  │ Provider         │
│ (File system)    │  │ (Dictionary)     │  │ (Decorator)      │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

### Core Components

- **AnalyzerEngine**: Stateless, deterministic analysis orchestrator (no IO dependencies)
- **ICodeProvider**: Abstraction for providing syntax trees from any source
- **FileSystemCodeProvider**: Discovers .cs files from disk (used by CLI)
- **InMemoryCodeProvider**: Analyzes in-memory source code (used for testing/IDE)
- **FilteringCodeProvider**: Decorator for selective analysis (predicate-based filtering)
- **AnalyzerManager**: Manages registered analyzer rules
- **IAnalyzerRule**: Interface for implementing custom analysis rules

## Migration Guide

### From v1.x to v2.0 (Enhanced Rule Contract)

**Version 2.0 introduces breaking changes to the IAnalyzerRule interface.** This migration guide helps you update your custom rules and usage code.

#### Breaking Change 1: Analyze() Return Type

**Before (v1.x)**:
```csharp
public DiagnosticResult? Analyze(SyntaxTree tree)
{
    var violation = FindFirstViolation(tree);
    return violation != null ? CreateResult(violation) : null;
}
```

**After (v2.0)**:
```csharp
public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)
{
    var violations = FindAllViolations(tree); // Find ALL, not just first
    
    foreach (var violation in violations)
    {
        yield return CreateResult(violation);
    }
    
    // Return empty collection instead of null
    // No explicit return needed - method naturally returns empty if no violations
}
```

**Migration Steps**:
1. Change return type from `DiagnosticResult?` to `IEnumerable<DiagnosticResult>`
2. Use `yield return` for each finding (enables lazy evaluation)
3. Find **all** violations, not just the first one
4. Remove null returns - empty collection is implicit

#### Breaking Change 2: New Required Properties

**Before (v1.x)**:
```csharp
public class MyRule : IAnalyzerRule
{
    public string Id => "MY001";
    public string Description => "My custom rule";
    
    public DiagnosticResult? Analyze(SyntaxTree tree) { /* ... */ }
}
```

**After (v2.0)**:
```csharp
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;

public class MyRule : IAnalyzerRule
{
    public string Id => "MY001";
    public string Description => "My custom rule";
    public Severity Severity => Severity.Warning;  // NEW - Required
    public string Category => DiagnosticCategories.Maintainability;  // NEW - Required
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree) { /* ... */ }
}
```

**Migration Steps**:
1. Add `Severity` property - choose from `Error`, `Warning`, or `Info`
2. Add `Category` property - use `DiagnosticCategories` constants or custom string
3. Update `Analyze()` signature per Breaking Change 1

**Severity Guidelines**:
- `Severity.Error`: Critical bugs, security vulnerabilities, correctness issues that **must** be fixed
- `Severity.Warning`: Code smells, maintainability problems, **should** be fixed but non-blocking
- `Severity.Info`: Style suggestions, optional improvements, informational only

**Category Options**:
- `DiagnosticCategories.Maintainability` - Code structure, readability, complexity
- `DiagnosticCategories.Performance` - Performance issues, inefficiencies
- `DiagnosticCategories.Security` - Security vulnerabilities, risks
- `DiagnosticCategories.Style` - Formatting, naming conventions
- `DiagnosticCategories.Design` - Architecture, design patterns
- `DiagnosticCategories.General` - General code quality
- Custom string: `"MyCustomCategory"` for domain-specific categorization

#### Breaking Change 3: DiagnosticResult Constructor

**Before (v1.x)**:
```csharp
yield return new DiagnosticResult(
    tree.FilePath,
    Id,
    "Method is too long",
    lineNumber
);
```

**After (v2.0)**:
```csharp
yield return new DiagnosticResult(
    tree.FilePath,
    Id,
    "Method is too long",
    lineNumber,
    Severity,      // NEW - Pass rule's severity
    Category       // NEW - Pass rule's category
);
```

**Migration Steps**:
1. Add `Severity` parameter (pass `this.Severity`)
2. Add `Category` parameter (pass `this.Category`)

#### Complete Migration Example

**Before (v1.x)**:
```csharp
public class LongMethodRule : IAnalyzerRule
{
    public string Id => "LNT001";
    public string Description => "Method exceeds recommended length";

    public DiagnosticResult? Analyze(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var longMethod = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Body?.Statements.Count > 20);

        if (longMethod != null)
        {
            var line = longMethod.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            return new DiagnosticResult(tree.FilePath, Id, "Method is too long", line);
        }

        return null;
    }
}
```

**After (v2.0)**:
```csharp
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;

public class LongMethodRule : IAnalyzerRule
{
    public string Id => "LNT001";
    public string Description => "Method exceeds recommended length";
    public Severity Severity => Severity.Warning;                    // ADDED
    public string Category => DiagnosticCategories.Maintainability;  // ADDED

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)    // CHANGED return type
    {
        var root = tree.GetRoot();
        var longMethods = root.DescendantNodes()                     // CHANGED to find ALL
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Body?.Statements.Count > 20);              // CHANGED to Where()

        foreach (var method in longMethods)                          // ADDED loop
        {
            var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new DiagnosticResult(                       // CHANGED to yield return
                tree.FilePath,
                Id,
                "Method is too long",
                line,
                Severity,                                            // ADDED
                Category                                             // ADDED
            );
        }
        // No explicit return - empty collection is implicit
    }
}
```

#### New CLI Features

**Severity Filtering**:
```bash
# Show only errors
dotnet run -- scan /path/to/project --severity Error

# Show errors and warnings
dotnet run -- scan /path/to/project --severity Error,Warning

# Show all (default behavior)
dotnet run -- scan /path/to/project --severity Error,Warning,Info
```

**Category Grouping**:
```bash
# Group findings by category in reports
dotnet run -- scan /path/to/project --group-by category

# Output organized with headers like:
# ## Maintainability
# LNT001: Method is too long (Controller.cs:45)
# 
# ## Performance
# PERF001: Inefficient LINQ usage (Service.cs:102)
```

**Combined**:
```bash
# Show only errors, grouped by category
dotnet run -- scan /path/to/project --severity Error --group-by category
```

#### Why This Change?

1. **Multiple Findings**: Old API (`DiagnosticResult?`) could only return one finding or null, missing subsequent violations in the same file
2. **Severity Filtering**: Users needed a way to focus on critical issues (errors) vs informational messages
3. **Better Reporting**: Categories enable organized reports, metrics by type, team-specific workflows
4. **Constitutional Alignment**: Principle III requires comprehensive findings and metadata

#### Migration Checklist

- [ ] Update all custom IAnalyzerRule implementations:
  - [ ] Change `Analyze()` return type to `IEnumerable<DiagnosticResult>`
  - [ ] Add `Severity` property
  - [ ] Add `Category` property
  - [ ] Use `yield return` for findings
  - [ ] Find ALL violations, not just first
  - [ ] Pass severity and category to DiagnosticResult constructor
- [ ] Update tests that expect single results to handle collections
- [ ] Update code that calls `Analyze()` to enumerate results
- [ ] Test with `--severity` and `--group-by` CLI options
- [ ] Review [CHANGELOG.md](CHANGELOG.md) for full list of changes

### From v1.x (Direct File System Access)

**Old API** (before IO boundary refactor):
```csharp
var engine = new AnalyzerEngine(manager);
var results = engine.Analyze("/path/to/project"); // REMOVED
```

**New API** (after IO boundary refactor):
```csharp
var provider = new FileSystemCodeProvider("/path/to/project");
var engine = new AnalyzerEngine(manager);
var results = engine.Analyze(provider.GetSyntaxTrees()); // NEW
```

### Key Changes

1. **AnalyzerEngine.Analyze() signature changed**:
   - **Before**: `Analyze(string path)` - accepted file path
   - **After**: `Analyze(IEnumerable<SyntaxTree> syntaxTrees)` - accepts parsed trees

2. **File system access moved to CLI layer**:
   - **Before**: AnalyzerEngine directly read files
   - **After**: FileSystemCodeProvider handles IO, AnalyzerEngine processes trees

3. **Testing is now in-memory**:
   - **Before**: Tests created temporary files on disk
   - **After**: Tests use `CSharpSyntaxTree.ParseText()` directly

### Benefits of Migration

✅ **50x faster tests** - no disk IO overhead  
✅ **Isolated tests** - no file cleanup, no race conditions  
✅ **IDE integration ready** - analyze unsaved buffers  
✅ **Constitutional compliance** - layered architecture enforced  
✅ **Same performance** - CLI usage has ±5% execution time  

## Creating Custom Code Providers

Implement `ICodeProvider` to analyze code from any source:

```csharp
using Lintelligent.AnalyzerEngine.Abstractions;
using Microsoft.CodeAnalysis;

public class DatabaseCodeProvider : ICodeProvider
{
    private readonly IDbConnection _connection;

    public DatabaseCodeProvider(IDbConnection connection)
    {
        _connection = connection;
    }

    public IEnumerable<SyntaxTree> GetSyntaxTrees()
    {
        // Fetch source code from database
        var sources = _connection.Query<CodeFile>("SELECT Path, Content FROM SourceFiles");
        
        foreach (var source in sources)
        {
            yield return CSharpSyntaxTree.ParseText(source.Content, path: source.Path);
        }
    }
}

// Usage
var provider = new DatabaseCodeProvider(dbConnection);
var results = engine.Analyze(provider.GetSyntaxTrees());
```

## Creating Custom Analyzer Rules

Implement `IAnalyzerRule` to add custom analysis logic:

```csharp
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class NoPublicFieldsRule : IAnalyzerRule
{
    public string Id => "LINT002";
    public string Description => "Classes should not expose public fields";
    public Severity Severity => Severity.Warning;                    // Required: Error/Warning/Info
    public string Category => DiagnosticCategories.Design;           // Required: Categorization

    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree)    // Returns IEnumerable
    {
        var root = tree.GetRoot();
        var publicFields = root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(m => m.Text == "public"));

        foreach (var field in publicFields)                          // Report ALL violations
        {
            var lineNumber = tree.GetLineSpan(field.Span).StartLinePosition.Line + 1;
            yield return new DiagnosticResult(
                tree.FilePath,
                Id,
                Description,
                lineNumber,
                Severity,                                            // Pass severity
                Category                                             // Pass category
            );
        }
        // Empty collection returned if no violations (implicit)
    }
}

// Register and use
manager.RegisterRule(new NoPublicFieldsRule());
```

**Key Points**:
- Use `yield return` for lazy evaluation (better performance)
- Find **all** violations in the file, not just the first
- Choose appropriate `Severity` (Error for critical issues, Warning for code smells, Info for suggestions)
- Use `DiagnosticCategories` constants for standard categories or define custom ones

## Monad Detection (Functional Programming)

Lintelligent can suggest safer functional patterns from [language-ext](https://github.com/louthy/language-ext) to improve error handling and eliminate null reference exceptions.

### Enable Monad Detection

Add to your `.editorconfig`:

```ini
[*.cs]
# Enable monad detection
language_ext_monad_detection = true

# Optional: Set complexity thresholds
language_ext_min_complexity = 3
```

### Available Monad Detections

#### LNT200: Nullable → Option<T>

Detects nullable return types with multiple null checks and suggests `Option<T>` to eliminate null reference exceptions.

**Before:**
```csharp
public User? FindUser(int id)
{
    var user = database.Find(id);
    if (user == null) return null;
    if (user.IsDeleted) return null;
    if (!user.IsActive) return null;
    return user;
}

// Usage requires null checks everywhere
var user = FindUser(123);
if (user != null) 
{
    Console.WriteLine(user.Name);
}
```

**After:**
```csharp
using LanguageExt;

public Option<User> FindUser(int id)
{
    var user = database.Find(id);
    return user switch
    {
        null => Option<User>.None,
        { IsDeleted: true } => Option<User>.None,
        { IsActive: false } => Option<User>.None,
        _ => Option<User>.Some(user)
    };
}

// Type-safe usage
FindUser(123).Match(
    Some: user => Console.WriteLine(user.Name),
    None: () => Console.WriteLine("User not found")
);
```

**Diagnostic**: Triggered when a method has 3+ null operations (configurable via `language_ext_min_complexity`).

#### LNT201: Try/Catch → Either<L, R>

Detects try/catch blocks used for control flow and suggests `Either<L, R>` for railway-oriented programming.

**Before:**
```csharp
public decimal CalculatePrice(int quantity, decimal unitPrice)
{
    try
    {
        if (quantity <= 0) throw new ArgumentException("Invalid quantity");
        return quantity * unitPrice;
    }
    catch (ArgumentException ex)
    {
        return 0m; // Error hidden in return value
    }
}
```

**After:**
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

public Either<string, decimal> CalculatePrice(int quantity, decimal unitPrice)
{
    if (quantity <= 0) return Left<string, decimal>("Invalid quantity");
    return Right<string, decimal>(quantity * unitPrice);
}

// Usage
var result = CalculatePrice(5, 10.50m);
result.Match(
    Right: price => Console.WriteLine($"Total: {price:C}"),
    Left: error => Console.WriteLine($"Error: {error}")
);
```

**Diagnostic**: Triggered when try and catch blocks both contain return statements (control flow pattern).

#### LNT202: Sequential Validation → Validation<T>

Detects sequential validation checks that fail fast and suggests `Validation<T>` to accumulate all errors.

**Before:**
```csharp
public User CreateUser(string name, string email, int age)
{
    if (string.IsNullOrEmpty(name)) 
        throw new ValidationException("Name required");
    
    if (!email.Contains("@")) 
        throw new ValidationException("Invalid email");
    
    if (age < 18) 
        throw new ValidationException("Must be 18+");
    
    return new User(name, email, age);
}

// User only sees first error
```

**After:**
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

public Validation<string, User> CreateUser(string name, string email, int age)
{
    var nameValidation = string.IsNullOrEmpty(name)
        ? Fail<string, string>("Name required")
        : Success<string, string>(name);
    
    var emailValidation = !email.Contains("@")
        ? Fail<string, string>("Invalid email")
        : Success<string, string>(email);
    
    var ageValidation = age < 18
        ? Fail<string, int>("Must be 18+")
        : Success<string, int>(age);
    
    return (nameValidation, emailValidation, ageValidation)
        .Apply((n, e, a) => new User(n, e, a));
}

// Usage - shows ALL errors at once
var result = CreateUser("", "invalid", 15);
result.Match(
    Succ: user => Console.WriteLine($"Created: {user.Name}"),
    Fail: errors => Console.WriteLine($"Errors: {string.Join(", ", errors)}")
);
// Output: "Errors: Name required, Invalid email, Must be 18+"
```

**Diagnostic**: Triggered when a method has 2+ sequential validation checks with immediate returns (configurable via `language_ext_min_complexity`).

### Configuration Options

In `.editorconfig`:

```ini
[*.cs]
# Enable/disable monad detection
language_ext_monad_detection = true|false

# Minimum complexity for Option<T> detection (default: 3)
# Only report methods with N+ null operations
language_ext_min_complexity = 3

# Minimum validations for Validation<T> detection (default: 2)
# Only report methods with N+ sequential validations
language_ext_min_complexity = 2
```

### Benefits

- **Type Safety**: Errors become part of the type signature
- **Null Safety**: `Option<T>` eliminates null reference exceptions at compile time
- **Better UX**: `Validation<T>` shows all errors at once instead of one at a time
- **Railway-Oriented Programming**: `Either<L, R>` makes error paths explicit
- **Functional Composition**: Monads compose elegantly with LINQ and language-ext combinators

For more details, see the [language-ext documentation](https://github.com/louthy/language-ext).

## Testing

Run all tests:

```bash
dotnet test
```

Run specific test project:

```bash
dotnet test tests/Lintelligent.AnalyzerEngine.Tests
dotnet test tests/Lintelligent.Cli.Tests
```

### Current Test Coverage

- **Total Tests**: 84 (100% passing)
- **AnalyzerEngine.Tests**: 62 tests (core engine, providers, integration, performance, validation)
- **Cli.Tests**: 22 tests (CLI commands, file system integration, filtering, grouping)
- **Code Coverage**: ≥95% for rule contract, ≥90% for AnalyzerEngine core

## Performance

Designed for large codebases:

- ✅ **Streaming architecture**: Uses `yield return` to avoid loading all files into memory
- ✅ **Lazy evaluation**: Syntax trees parsed on-demand during enumeration
- ✅ **Memory efficient**: Tested with 10,000+ file projects, no memory exhaustion
- ✅ **Fast execution**: ±5% performance vs pre-refactor implementation

## Project Structure

```
Lintelligent/
├── src/
│   ├── Lintelligent.AnalyzerEngine/    # Core analysis engine (no IO dependencies)
│   │   ├── Abstractions/               # ICodeProvider interface
│   │   ├── Analysis/                   # AnalyzerEngine, AnalyzerManager
│   │   ├── Rules/                      # IAnalyzerRule, LongMethodRule
│   │   └── Results/                    # DiagnosticResult
│   ├── Lintelligent.Cli/               # CLI application (handles IO)
│   │   ├── Commands/                   # ScanCommand
│   │   └── Providers/                  # FileSystemCodeProvider
│   └── Lintelligent.Reporting/         # Report generation
└── tests/
    ├── Lintelligent.AnalyzerEngine.Tests/
    │   ├── TestUtilities/              # InMemoryCodeProvider, FilteringCodeProvider
    │   ├── AnalyzerEngineTests.cs
    │   ├── InMemoryCodeProviderTests.cs
    │   ├── FilteringCodeProviderTests.cs
    │   └── CodeProviderIntegrationTests.cs
    └── Lintelligent.Cli.Tests/
        ├── Providers/                  # FileSystemCodeProviderTests
        └── ScanCommandTests.cs
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Implement your changes
5. Ensure all tests pass (`dotnet test`)
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## License

[Add license information]

## Support

For issues, questions, or contributions, please [open an issue](https://github.com/yourorg/lintelligent/issues).
