# Lintelligent

[![NuGet](https://img.shields.io/nuget/v/Lintelligent.Analyzers?label=Analyzers)](https://www.nuget.org/packages/Lintelligent.Analyzers)
[![NuGet](https://img.shields.io/nuget/v/Lintelligent.Cli?label=CLI)](https://www.nuget.org/packages/Lintelligent.Cli)
[![NuGet](https://img.shields.io/nuget/v/Lintelligent.Reporting?label=Reporting)](https://www.nuget.org/packages/Lintelligent.Reporting)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build Status](https://github.com/alexm15/Lintelligent/actions/workflows/ci.yml/badge.svg)](https://github.com/alexm15/Lintelligent/actions)

Lintelligent is a modular static analysis toolkit for C#/.NET projects, featuring analyzers, code-fixes, a powerful CLI, and flexible reporting. All major components are open-source under MIT, with optional commercial licensing for advanced code-fix features.

---

## Major Components

### 1. Lintelligent.Analyzers

Roslyn-based analyzers for code quality, maintainability, and duplication. Rules are grouped by category:

The Roslyn analyzer automatically integrates with your IDE (Visual Studio, Rider, VS Code) and provides:
- Real-time diagnostics as you type
- Code navigation (F8 to jump between issues)
- EditorConfig support for per-project rule configuration
- Zero build overhead (<2s for 100-file solutions)
- **Workspace-level analysis**: Cross-file duplication detection (requires .NET 6+)

See the [Analyzer Guide](./specs/019-roslyn-analyzer-bridge/ANALYZER_GUIDE.md) for configuration options.

**Rule Categories & Examples:**

| Category           | Rule Name                | ID      | Description                                 |
|--------------------|-------------------------|---------|---------------------------------------------|
| Maintainability    | LongMethodRule          | LNT001  | Detects methods exceeding length threshold  |
| Duplication        | CodeDuplicationRule     | LNT100  | Finds whole-file and sub-block duplications |
| Design             | NoPublicFieldsRule      | LNT002  | Flags classes with public fields            |
| Performance        | InefficientLinqRule     | PERF001 | Detects inefficient LINQ usage              |
| Security           | (Planned)               |         |                                            |
| Style              | (Planned)               |         |                                            |

Rules are extensible—implement `IAnalyzerRule` to add your own.

---

### 2. Lintelligent.Cli

Cross-platform CLI for scanning projects, solutions, or files. Key features:

- **Scan**: Analyze codebase for diagnostics
- **Severity Filtering**: `--severity Error,Warning,Info`
- **Grouping**: `--group-by category` for organized reports
- **Output Formats**: Console, JSON (via Reporting)
- **Performance**: Streaming, memory-efficient for large repos
- **Integration**: Easily embed via `CliApplicationBuilder`

Example usage:
```bash
dotnet run -- scan /path/to/project --severity Error,Warning --group-by category
```

---

### 3. Lintelligent.CodeFixes

Roslyn CodeFixProviders for automated code fixes. Features:

- **Automated Fixes**: Suggest and apply fixes for supported diagnostics
- **Fix-All Support**: Document/project/solution-wide fixes
- **Rule Coverage**:
    - `LongMethodRule`: No code-fix (informational)
    - `NullableToOptionRule`: Code-fix available (convert nullable to Option<T>)
    - (Add more as rules evolve)
- **License**: Commercial license required for advanced code-fix features

---

### 4. Lintelligent.Reporting

Flexible reporting for CLI and integrations. Features:

- **Supported Formats**:
    - Console (default)
    - JSON (`--output json`)
- **Extensible**: Add custom formatters via `IReportFormatter`
- **Output Configuration**: Control verbosity, grouping, and output destination

---

## Repository Structure

```
src/
    Lintelligent.Analyzers/      # Roslyn analyzers
    Lintelligent.Cli/            # CLI application
    Lintelligent.CodeFixes/      # Code-fix providers
    Lintelligent.Reporting/      # Reporting/formatters
tests/
    Lintelligent.AnalyzerEngine.Tests/
    Lintelligent.Cli.Tests/
LICENSE
LICENSE-PRO.md
README.md
.github/
    workflows/
```

---

## Installation

### CLI Tool
```bash
dotnet build
```

### Roslyn Analyzer (NuGet)
```bash
dotnet add package Lintelligent.Analyzers
```

### CodeFixes (Pro)
```bash
dotnet add package Lintelligent.CodeFixes
# Requires license for advanced features
```

---

## Quick Start

Analyze a project:
```bash
cd src/Lintelligent.Cli
dotnet run -- scan /path/to/project
```

Filter by severity:
```bash
dotnet run -- scan /path/to/project --severity Error,Warning
```

Group by category:
```bash
dotnet run -- scan /path/to/project --group-by category
```

Output as JSON:
```bash
dotnet run -- scan /path/to/project --output json
```

---

## Extending & Contributing

Lintelligent is designed for extensibility. Add new rules, formatters, or CLI commands by following the architecture in each component. See CONTRIBUTING.md for guidelines.

---

## License

MIT License for analyzers, CLI, and reporting. Commercial license required for advanced code-fix features (see LICENSE-PRO.md).

---

## Support

- Community: [GitHub Discussions](https://github.com/alexm15/Lintelligent/discussions)


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

## Performance

Designed for large codebases:

- ✅ **Streaming architecture**: Uses `yield return` to avoid loading all files into memory
- ✅ **Lazy evaluation**: Syntax trees parsed on-demand during enumeration
- ✅ **Memory efficient**: Tested with 10,000+ file projects, no memory exhaustion
- ✅ **Fast execution**: ±5% performance vs pre-refactor implementation

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
For issues, questions, or contributions, please [open an issue](https://github.com/alexm15/lintelligent/issues).
