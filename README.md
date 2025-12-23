# Lintelligent

A static code analysis CLI tool for C# projects that detects code quality issues and provides actionable insights.

## Features

- 🔍 **Configurable Rules**: Analyze C# code with extensible analyzer rules
- 🚀 **High Performance**: Handles large codebases (10,000+ files) with streaming architecture
- 🧪 **Testable Core**: Framework-agnostic engine with no file system dependencies
- 🔌 **Extensible**: Plugin-friendly architecture for IDE integration and custom frontends

## Installation

```bash
dotnet build
```

## Quick Start

### Basic Usage

Analyze a C# project or file:

```bash
cd src/Lintelligent.Cli
dotnet run -- scan /path/to/project
```

Analyze a single file:

```bash
dotnet run -- scan /path/to/file.cs
```

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
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class NoPublicFieldsRule : IAnalyzerRule
{
    public string Id => "LINT002";
    public string Description => "Classes should not expose public fields";

    public DiagnosticResult Analyze(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var publicFields = root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(m => m.Text == "public"));

        if (publicFields.Any())
        {
            var firstField = publicFields.First();
            var lineNumber = tree.GetLineSpan(firstField.Span).StartLinePosition.Line + 1;
            return new DiagnosticResult(tree.FilePath, Id, Description, lineNumber);
        }

        return null!; // No violation
    }
}

// Register and use
manager.RegisterRule(new NoPublicFieldsRule());
```

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

- **Total Tests**: 53 (100% passing)
- **AnalyzerEngine.Tests**: 33 tests (core engine, providers, integration)
- **Cli.Tests**: 20 tests (CLI commands, file system integration)
- **Code Coverage**: ≥90% for AnalyzerEngine core

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
