# Data Model: Structured Output Formats

**Feature**: 006-structured-output-formats  
**Date**: 2025-12-25  
**Phase**: 1 (Design & Contracts)

## Overview

This document defines the data models, interfaces, and mappings for the three output formatters: JSON, SARIF 2.1.0, and enhanced Markdown. All models are designed for testability, determinism, and constitutional compliance.

---

## Core Abstractions

### IReportFormatter Interface

**Purpose**: Abstract contract for all output formatters, enabling extensibility (Constitutional Principle VI)

**Location**: `src/Lintelligent.Reporting/Formatters/IReportFormatter.cs`

```csharp
namespace Lintelligent.Reporting.Formatters;

/// <summary>
/// Defines the contract for report formatters that transform diagnostic results into output formats.
/// </summary>
/// <remarks>
/// Constitutional Compliance:
/// - Principle VI (Extensibility): Allows third-party formatters without modifying existing code
/// - Principle VII (Testability): Pure transformation, testable with mock DiagnosticResult data
/// - Principle III (Determinism): Same input → same output (no side effects)
/// </remarks>
public interface IReportFormatter
{
    /// <summary>
    /// Formats a collection of diagnostic results into a string representation.
    /// </summary>
    /// <param name="results">Diagnostic results to format (may be empty collection).</param>
    /// <returns>Formatted output string (JSON, SARIF, Markdown, etc.).</returns>
    /// <remarks>
    /// Implementation Requirements:
    /// - MUST be stateless and thread-safe
    /// - MUST handle empty collections gracefully (FR-014)
    /// - MUST NOT perform I/O operations (pure transformation)
    /// - SHOULD complete in &lt;10 seconds for 10,000 results (SC-008)
    /// </remarks>
    string Format(IEnumerable<DiagnosticResult> results);
    
    /// <summary>
    /// Gets the format name (e.g., "json", "sarif", "markdown") for CLI selection.
    /// </summary>
    string FormatName { get; }
}
```

**Design Rationale**:
- Single method interface (simplicity, YAGNI)
- Accepts `IEnumerable<T>` for deferred execution (future streaming support)
- Returns `string` (not `void`) to decouple formatting from I/O
- `FormatName` property enables CLI `--format` flag matching

---

### OutputConfiguration Model

**Purpose**: Encapsulates output destination and formatting options

**Location**: `src/Lintelligent.Cli/Models/OutputConfiguration.cs`

```csharp
namespace Lintelligent.Cli.Models;

/// <summary>
/// Configuration for output destination and formatting options.
/// </summary>
/// <remarks>
/// This model is immutable (record type) for thread-safety and determinism.
/// </remarks>
public record OutputConfiguration
{
    /// <summary>
    /// Output format (json, sarif, markdown).
    /// </summary>
    public required string Format { get; init; }
    
    /// <summary>
    /// Output path (file path or "-" for stdout). Null means stdout (default).
    /// </summary>
    public string? OutputPath { get; init; }
    
    /// <summary>
    /// Whether to use ANSI color codes (auto-detected or user-specified).
    /// Only applicable to Markdown format.
    /// </summary>
    public bool EnableColor { get; init; } = true;
    
    /// <summary>
    /// Validates configuration rules (e.g., markdown format with color support).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Format))
            throw new ArgumentException("Format cannot be null or whitespace", nameof(Format));
        
        var validFormats = new[] { "json", "sarif", "markdown" };
        if (!validFormats.Contains(Format.ToLowerInvariant()))
            throw new ArgumentException(
                $"Invalid format '{Format}'. Valid formats: {string.Join(", ", validFormats)}", 
                nameof(Format));
    }
}
```

**Usage Example**:
```csharp
var config = new OutputConfiguration
{
    Format = "json",
    OutputPath = "results.json",
    EnableColor = false // not applicable for JSON, but harmless
};
config.Validate(); // throws if invalid
```

---

## JSON Formatter

### JsonOutputModel

**Purpose**: Strongly-typed model for JSON serialization (ensures schema conformance)

**Location**: `src/Lintelligent.Reporting/Formatters/Models/JsonOutputModel.cs`

```csharp
namespace Lintelligent.Reporting.Formatters.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Root JSON output model conforming to Lintelligent JSON schema.
/// </summary>
public record JsonOutputModel
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }
    
    [JsonPropertyName("summary")]
    public required SummaryModel Summary { get; init; }
    
    [JsonPropertyName("violations")]
    public required List<ViolationModel> Violations { get; init; }
}

public record SummaryModel
{
    [JsonPropertyName("total")]
    public required int Total { get; init; }
    
    [JsonPropertyName("bySeverity")]
    public required Dictionary<string, int> BySeverity { get; init; }
}

public record ViolationModel
{
    [JsonPropertyName("filePath")]
    public required string FilePath { get; init; }
    
    [JsonPropertyName("lineNumber")]
    public required int LineNumber { get; init; }
    
    [JsonPropertyName("ruleId")]
    public required string RuleId { get; init; }
    
    [JsonPropertyName("severity")]
    public required string Severity { get; init; }
    
    [JsonPropertyName("category")]
    public required string Category { get; init; }
    
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
```

### DiagnosticResult → JSON Mapping

```csharp
// Mapping logic (implemented in JsonFormatter)
var violations = results.Select(r => new ViolationModel
{
    FilePath = r.FilePath,
    LineNumber = r.LineNumber,
    RuleId = r.RuleId,
    Severity = r.Severity.ToString().ToLowerInvariant(), // Severity.Warning → "warning"
    Category = r.Category,
    Message = r.Message
}).ToList();

var summary = new SummaryModel
{
    Total = violations.Count,
    BySeverity = violations
        .GroupBy(v => v.Severity)
        .ToDictionary(g => g.Key, g => g.Count())
};

var output = new JsonOutputModel
{
    Status = "success", // or "error" if analysis failed
    Summary = summary,
    Violations = violations
};

string json = JsonSerializer.Serialize(output, new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
});
```

**Severity Mapping**:
- `Severity.Error` → `"error"`
- `Severity.Warning` → `"warning"`
- `Severity.Info` → `"info"`

**Empty Results**:
```json
{
    "status": "success",
    "summary": {
        "total": 0,
        "bySeverity": {}
    },
    "violations": []
}
```

---

## SARIF Formatter

### DiagnosticResult → SARIF Mapping

**SARIF Object Model** (using Microsoft.CodeAnalysis.Sarif):

```csharp
using Microsoft.CodeAnalysis.Sarif;

// 1. Generate unique rules from results
var uniqueRules = results
    .GroupBy(r => r.RuleId)
    .Select(g =>
    {
        var first = g.First();
        return new ReportingDescriptor
        {
            Id = first.RuleId,
            ShortDescription = new MultiformatMessageString
            {
                Text = $"{first.Category}: {first.RuleId}"
            },
            HelpUri = new Uri($"https://lintelligent.dev/rules/{first.RuleId}"),
            DefaultConfiguration = new ReportingConfiguration
            {
                Level = MapSeverityToFailureLevel(first.Severity)
            },
            Properties = new Dictionary<string, object>
            {
                { "category", first.Category }
            }
        };
    })
    .ToList();

// 2. Map results to SARIF Result objects
var sarifResults = results.Select(r => new Result
{
    RuleId = r.RuleId,
    Level = MapSeverityToFailureLevel(r.Severity),
    Message = new Message { Text = r.Message },
    Locations = new[]
    {
        new Location
        {
            PhysicalLocation = new PhysicalLocation
            {
                ArtifactLocation = new ArtifactLocation
                {
                    Uri = new Uri(Path.GetFullPath(r.FilePath), UriKind.Absolute)
                },
                Region = new Region
                {
                    StartLine = r.LineNumber
                    // NO StartColumn/EndColumn - line-only precision per Clarification #2
                }
            }
        }
    }
}).ToList();

// 3. Build SARIF log
var run = new Run
{
    Tool = new Tool
    {
        Driver = new ToolComponent
        {
            Name = "Lintelligent",
            Version = "1.0.0", // TODO: Get from assembly version
            InformationUri = new Uri("https://github.com/your-org/lintelligent"),
            Rules = uniqueRules
        }
    },
    Results = sarifResults
};

var sarifLog = new SarifLog
{
    Version = SarifVersion.Current, // 2.1.0
    Runs = new[] { run }
};

// 4. Serialize
string json = JsonConvert.SerializeObject(sarifLog, Formatting.Indented);
```

**Severity Mapping**:
```csharp
private static FailureLevel MapSeverityToFailureLevel(Severity severity)
{
    return severity switch
    {
        Severity.Error => FailureLevel.Error,
        Severity.Warning => FailureLevel.Warning,
        Severity.Info => FailureLevel.Note,
        _ => throw new ArgumentOutOfRangeException(nameof(severity))
    };
}
```

**File Path Handling**:
- DiagnosticResult stores relative or absolute paths
- SARIF requires absolute `file://` URIs
- Use `Path.GetFullPath()` to normalize, then `new Uri(path, UriKind.Absolute)`
- Example: `src/Foo.cs` → `file:///C:/path/to/repo/src/Foo.cs`

**Empty Results**:
```json
{
    "version": "2.1.0",
    "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
    "runs": [{
        "tool": {
            "driver": {
                "name": "Lintelligent",
                "version": "1.0.0",
                "informationUri": "https://github.com/your-org/lintelligent",
                "rules": []
            }
        },
        "results": []
    }]
}
```

---

## Enhanced Markdown Formatter

### Markdown Structure

**Output Format**:
```markdown
# Lintelligent Report

## Summary

| Severity | Count |
|----------|-------|
| Error    | 5     |
| Warning  | 30    |
| Info     | 7     |
| **Total**| **42**|

## Violations by File

### src/Foo.cs (12 violations)

- **[ERROR] LNT001** (Line 42): Method too long (Maintainability)
- **[WARNING] LNT003** (Line 55): Parameter count exceeds limit (Design)
...

### src/Bar.cs (8 violations)

- **[INFO] LNT007** (Line 10): Consider extracting method (Maintainability)
...
```

### ANSI Color Codes

**When Enabled** (based on OutputConfiguration.EnableColor):
```markdown
\x1b[91m[ERROR]\x1b[0m LNT001 (Line 42): Method too long
\x1b[93m[WARNING]\x1b[0m LNT003 (Line 55): Parameter count exceeds limit
\x1b[96m[INFO]\x1b[0m LNT007 (Line 10): Consider extracting method
```

**Color Constants**:
```csharp
public static class AnsiColors
{
    public const string BrightRed = "\x1b[91m";
    public const string BrightYellow = "\x1b[93m";
    public const string BrightCyan = "\x1b[96m";
    public const string Reset = "\x1b[0m";
    
    public static string Colorize(string text, Severity severity)
    {
        if (!Console.IsOutputRedirected && ShouldUseColor())
        {
            var color = severity switch
            {
                Severity.Error => BrightRed,
                Severity.Warning => BrightYellow,
                Severity.Info => BrightCyan,
                _ => ""
            };
            return $"{color}{text}{Reset}";
        }
        return text;
    }
}
```

### Grouping Strategy

**By File** (default):
```csharp
var grouped = results
    .GroupBy(r => r.FilePath)
    .OrderBy(g => g.Key);

foreach (var group in grouped)
{
    output += $"### {group.Key} ({group.Count()} violations)\n\n";
    foreach (var result in group)
    {
        output += FormatViolation(result);
    }
}
```

**By Category** (when `--group-by category`):
```csharp
var grouped = results
    .GroupBy(r => r.Category)
    .OrderBy(g => g.Key);

foreach (var group in grouped)
{
    output += $"## {group.Key}\n\n";
    foreach (var result in group)
    {
        output += FormatViolation(result);
    }
}
```

---

## CLI Integration Models

### OutputWriter Abstraction

**Purpose**: Decouple formatting from I/O (Constitutional Principle I - Layered Architecture)

**Location**: `src/Lintelligent.Cli/Infrastructure/OutputWriter.cs`

```csharp
namespace Lintelligent.Cli.Infrastructure;

/// <summary>
/// Handles writing formatted output to files or stdout.
/// </summary>
/// <remarks>
/// Constitutional Compliance:
/// - I/O operations confined to CLI layer
/// - Formatters remain pure (no side effects)
/// - Testable via in-memory streams
/// </remarks>
public class OutputWriter
{
    /// <summary>
    /// Writes content to the specified output destination.
    /// </summary>
    /// <param name="content">Formatted content to write.</param>
    /// <param name="outputPath">File path or "-" for stdout. Null means stdout.</param>
    /// <exception cref="IOException">Thrown if file write fails.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if path not writable.</exception>
    public void Write(string content, string? outputPath)
    {
        if (outputPath == null || outputPath == "-")
        {
            // Write to stdout (FR-010)
            Console.WriteLine(content);
            return;
        }
        
        // Validate path is writable (FR-009)
        ValidateOutputPath(outputPath);
        
        // Write to file (atomic: temp file → rename)
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            File.Move(tempPath, outputPath, overwrite: true);
        }
        catch
        {
            // Cleanup temp file on failure
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }
    
    private static void ValidateOutputPath(string path)
    {
        // Check if directory exists (not path traversal validation per Clarification #4)
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!Directory.Exists(directory))
        {
            throw new IOException(
                $"Output directory does not exist: {directory}. " +
                $"Create the directory or specify a valid path.");
        }
        
        // Check if path is writable (best-effort, actual write may still fail)
        try
        {
            using var fs = File.OpenWrite(path);
        }
        catch (UnauthorizedAccessException)
        {
            throw new IOException($"Output path is read-only or not writable: {path}");
        }
    }
}
```

---

## Dependency Injection Wiring

**Location**: `src/Lintelligent.Cli/Bootstrapper.cs`

```csharp
// Register formatters
services.AddTransient<IReportFormatter, JsonFormatter>();
services.AddTransient<IReportFormatter, SarifFormatter>();
services.AddTransient<IReportFormatter, MarkdownFormatter>();

// Register output writer
services.AddSingleton<OutputWriter>();

// ScanCommand receives IEnumerable<IReportFormatter> to select by --format flag
```

**ScanCommand Selection Logic**:
```csharp
public sealed class ScanCommand(
    IEnumerable<IReportFormatter> formatters,
    OutputWriter outputWriter)
{
    public Task<CommandResult> ExecuteAsync(string[] args)
    {
        var config = ParseOutputConfiguration(args);
        config.Validate();
        
        // Select formatter by name
        var formatter = formatters.FirstOrDefault(f => 
            f.FormatName.Equals(config.Format, StringComparison.OrdinalIgnoreCase));
        
        if (formatter == null)
        {
            var validFormats = string.Join(", ", formatters.Select(f => f.FormatName));
            return Task.FromResult(CommandResult.Failure(2, 
                $"Invalid format '{config.Format}'. Valid formats: {validFormats}"));
        }
        
        // ... analyze, format, write
        var results = engine.Analyze(syntaxTrees);
        var output = formatter.Format(results);
        outputWriter.Write(output, config.OutputPath);
        
        // Only write progress to stdout when output goes to file (FR-011)
        if (config.OutputPath != null && config.OutputPath != "-")
        {
            Console.WriteLine($"Analysis complete. Results written to {config.OutputPath}");
        }
    }
}
```

---

## Validation & Testing Strategy

### Unit Tests (Per Formatter)

**JsonFormatterTests.cs**:
- Valid schema with 0 violations
- Valid schema with 1 violation
- Valid schema with 10,000 violations
- Severity mapping (Error/Warning/Info → error/warning/info)
- Special character escaping (quotes, newlines, Unicode)
- Empty bySeverity when 0 results
- Performance: 10,000 violations < 10 seconds

**SarifFormatterTests.cs**:
- SARIF 2.1.0 schema validation (using Microsoft.CodeAnalysis.Sarif.Validation)
- Tool metadata presence (driver.name, driver.version)
- Rules array populated (unique RuleIds)
- HelpUri included per rule (FR-015)
- Region has startLine but no startColumn (line-only precision)
- File URI format (`file:///` prefix)
- Empty results produces valid SARIF

**MarkdownFormatterTests.cs**:
- Summary table includes all severity counts
- Color codes present when EnableColor = true
- Color codes absent when EnableColor = false
- Grouping by file (default)
- Grouping by category (--group-by flag)
- Empty results produces minimal markdown

### Integration Tests (CLI)

**ScanCommandTests.cs**:
- `--format json` produces valid JSON
- `--format sarif` produces valid SARIF
- `--format markdown` produces markdown
- `--output file.json` writes to file
- `--output -` writes to stdout
- Invalid --format value → error with valid formats listed
- Read-only output path → clear error message
- Non-existent directory → clear error message

---

## Summary

### Files Created (Phase 1)

| File | Purpose |
|------|---------|
| `IReportFormatter.cs` | Core abstraction |
| `OutputConfiguration.cs` | CLI configuration model |
| `JsonOutputModel.cs` | JSON schema models |
| `JsonFormatter.cs` | JSON implementation |
| `SarifFormatter.cs` | SARIF implementation |
| `MarkdownFormatter.cs` | Enhanced markdown implementation |
| `OutputWriter.cs` | File/stdout I/O abstraction |
| `AnsiColors.cs` | Color code constants |

### Constitutional Compliance

- ✅ **Principle I (Layered Architecture)**: Formatters in Reporting (pure), I/O in CLI
- ✅ **Principle II (DI Boundaries)**: DI only in CLI layer
- ✅ **Principle III (Determinism)**: Same DiagnosticResult → same output (no randomness)
- ✅ **Principle V (Testing)**: Formatters testable without DI or full app
- ✅ **Principle VI (Extensibility)**: IReportFormatter enables third-party formats
- ✅ **Principle VII (Testability)**: Pure transformation, mock-friendly

**Ready for Phase 2 Implementation** ✅
