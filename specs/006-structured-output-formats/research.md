# Research: Structured Output Formats

**Feature**: 006-structured-output-formats  
**Date**: 2025-12-25  
**Phase**: 0 (Research & Discovery)

## Overview

This document consolidates research findings for implementing three output formatters: JSON, SARIF 2.1.0, and enhanced Markdown with ANSI colors. All research completed to resolve technical unknowns before implementation.

---

## 1. SARIF 2.1.0 Implementation

### Decision: Use Microsoft.CodeAnalysis.Sarif NuGet Package

**Chosen**: `Microsoft.CodeAnalysis.Sarif` v4.x (latest stable)

**Rationale**:
- Official Microsoft package with full SARIF 2.1.0 object model
- Type-safe API prevents schema violations
- Built-in validation and serialization
- Actively maintained (used by Microsoft security tooling)

**Alternative Rejected**: Manual JSON serialization - too error-prone, no schema validation

### SARIF 2.1.0 Schema Reference

- **Official Spec**: https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html
- **JSON Schema**: https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json
- **Microsoft Docs**: https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.sarif

### Data Mapping: DiagnosticResult → SARIF

```csharp
// DiagnosticResult (source)
{
    FilePath: "src/Foo.cs",
    LineNumber: 42,
    RuleId: "LNT001",
    Severity: Severity.Warning,
    Message: "Method too long",
    Category: "Maintainability"
}

// SARIF Result (target)
{
    "ruleId": "LNT001",
    "level": "warning",  // Severity.Warning → "warning"
    "message": {
        "text": "Method too long"
    },
    "locations": [{
        "physicalLocation": {
            "artifactLocation": {
                "uri": "file:///C:/path/to/src/Foo.cs"
            },
            "region": {
                "startLine": 42
                // NO startColumn/endColumn - DiagnosticResult has line-only precision
            }
        }
    }]
}
```

**Severity Mapping**:
- `Severity.Error` → `"error"`
- `Severity.Warning` → `"warning"`
- `Severity.Info` → `"note"`

### Tool Metadata Requirements

**Minimal (required for validation)**:
```json
{
    "tool": {
        "driver": {
            "name": "Lintelligent",
            "version": "1.0.0",
            "informationUri": "https://github.com/your-org/lintelligent"
        }
    }
}
```

**Complete (for IDE integration)**:
```json
{
    "tool": {
        "driver": {
            "name": "Lintelligent",
            "version": "1.0.0",
            "informationUri": "https://github.com/your-org/lintelligent",
            "rules": [
                {
                    "id": "LNT001",
                    "shortDescription": { "text": "Method too long" },
                    "helpUri": "https://lintelligent.dev/rules/LNT001",
                    "defaultConfiguration": { "level": "warning" },
                    "properties": {
                        "category": "Maintainability"
                    }
                }
                // ... one entry per unique RuleId in results
            ]
        }
    }
}
```

### IDE Integration Notes

**VS Code**:
- Strictly requires SARIF 2.1.0 (older versions rejected)
- Imports via `SARIF Viewer` extension or native Problems panel
- Auto-resolves `file://` URIs to workspace-relative paths
- Displays `message.text` and `level` in Problems panel
- Clicking navigates to `physicalLocation.region.startLine`
- **Critical**: Must include `locations[0].physicalLocation` for navigation

**GitHub Code Scanning**:
- Accepts SARIF uploads via `github/codeql-action/upload-sarif@v2`
- **Recommended**: Include `partialFingerprints` for deduplication
- **Recommended**: Include `startColumn/endColumn` for precise highlighting (we'll omit, acceptable)
- Max 10MB per file, 25,000 results per run
- Requires `tool.driver.name` and `results[].ruleId`

### Code Generation Approach

**Chosen**: Object model (Microsoft.CodeAnalysis.Sarif)

**Example**:
```csharp
using Microsoft.CodeAnalysis.Sarif;

var run = new Run
{
    Tool = new Tool
    {
        Driver = new ToolComponent
        {
            Name = "Lintelligent",
            Version = "1.0.0",
            Rules = /* generated from unique RuleIds */
        }
    },
    Results = diagnosticResults.Select(d => new Result
    {
        RuleId = d.RuleId,
        Level = MapSeverity(d.Severity),
        Message = new Message { Text = d.Message },
        Locations = new[]
        {
            new Location
            {
                PhysicalLocation = new PhysicalLocation
                {
                    ArtifactLocation = new ArtifactLocation
                    {
                        Uri = new Uri(d.FilePath, UriKind.Absolute)
                    },
                    Region = new Region { StartLine = d.LineNumber }
                }
            }
        }
    }).ToList()
};

var sarifLog = new SarifLog
{
    Version = SarifVersion.Current, // 2.1.0
    Runs = new[] { run }
};

return JsonConvert.SerializeObject(sarifLog, Formatting.Indented);
```

**Alternative Rejected**: Manual JSON - too verbose, error-prone, no compile-time safety

---

## 2. JSON Output Design

### Decision: Flat Array with Status Wrapper

**Chosen Schema**:
```json
{
    "status": "success",
    "summary": {
        "total": 42,
        "bySeverity": {
            "error": 5,
            "warning": 30,
            "info": 7
        }
    },
    "violations": [
        {
            "filePath": "src/Foo.cs",
            "lineNumber": 42,
            "ruleId": "LNT001",
            "severity": "warning",
            "category": "Maintainability",
            "message": "Method too long"
        }
    ]
}
```

**Rationale**:
- **Flat array**: Enables streaming for 10,000+ violations (vs nested file structure)
- **Status field**: Enables CI/CD exit code decisions (success/failure)
- **Summary statistics**: Quick filtering without parsing full violations array
- **camelCase**: Matches .NET ecosystem, SARIF, and jq conventions

**Alternatives Surveyed**:
- **ESLint format** (file-centric): Good for small codebases, poor for streaming
- **RuboCop format** (nested files): Same issue, harder to parse with jq
- **golangci-lint format** (flat array): ✅ Closest match, streaming-friendly
- **Clippy NDJSON**: Best for 100K+ violations, overkill for Phase 1

### Field Naming Conventions

**Chosen**: camelCase

**Rationale**:
- Consistent with SARIF schema (industryStandard)
- Matches .NET serialization defaults (System.Text.Json)
- jq query compatibility: `.violations[0].lineNumber` (vs `.violations[0].line_number`)

**Category Values**: Fixed set from Feature 019 rules
- "Maintainability", "Performance", "Security", "Reliability", "Design", "Documentation", "Naming", "Testing"

### Severity Representation

**Chosen**: String values (`"error"`, `"warning"`, `"info"`)

**Rationale**:
- Self-documenting (no need to lookup numeric codes)
- Industry standard (ESLint, RuboCop, SARIF all use strings)
- Easy jq filtering: `jq '.violations[] | select(.severity == "error")'`

**Alternative Rejected**: Numeric codes (0=Info, 1=Warning, 2=Error) - less readable

### Performance Strategy

**Phase 1**: Buffered JSON (materialize all results in memory, serialize once)
- ✅ Simple implementation: `JsonSerializer.Serialize(result)`
- ✅ Complete object graph (summary stats require full collection)
- ⚠️ Memory: ~1KB per violation × 10K violations = ~10MB (acceptable)

**Phase 2** (Future): NDJSON streaming for 100K+ violations
- Newline-delimited JSON (one violation per line)
- Stream results as analysis progresses
- Out of scope for Feature 006

### Edge Case Handling

| Case | Behavior |
|------|----------|
| Empty results (0 violations) | `{ "status": "success", "summary": { "total": 0, "bySeverity": {} }, "violations": [] }` |
| Special characters in message | Escape per JSON spec (`\"`, `\n`, `\t`, Unicode) |
| File path with backslashes (Windows) | Normalize to forward slashes or escape: `C:\\path\\to\\file.cs` |
| Null Category field (defensive) | Replace with `"Unknown"` or omit field |
| Analysis error (exception) | `{ "status": "error", "message": "Analysis failed: ...", "violations": [] }` |

### CLI Tool Compatibility

**jq Examples**:
```bash
# Count errors
jq '.summary.bySeverity.error' results.json

# Filter by severity
jq '.violations[] | select(.severity == "error")' results.json

# Group by file
jq 'group_by(.filePath) | map({file: .[0].filePath, count: length})' results.json
```

**PowerShell Examples**:
```powershell
# Parse JSON
$results = Get-Content results.json | ConvertFrom-Json

# Count warnings
$results.summary.bySeverity.warning

# Filter by category
$results.violations | Where-Object { $_.category -eq "Security" }
```

### JSON Schema Definition

```json
{
    "$schema": "http://json-schema.org/draft-07/schema#",
    "type": "object",
    "required": ["status", "summary", "violations"],
    "properties": {
        "status": {
            "type": "string",
            "enum": ["success", "error"]
        },
        "summary": {
            "type": "object",
            "required": ["total", "bySeverity"],
            "properties": {
                "total": { "type": "integer", "minimum": 0 },
                "bySeverity": {
                    "type": "object",
                    "properties": {
                        "error": { "type": "integer", "minimum": 0 },
                        "warning": { "type": "integer", "minimum": 0 },
                        "info": { "type": "integer", "minimum": 0 }
                    }
                }
            }
        },
        "violations": {
            "type": "array",
            "items": {
                "type": "object",
                "required": ["filePath", "lineNumber", "ruleId", "severity", "category", "message"],
                "properties": {
                    "filePath": { "type": "string", "minLength": 1 },
                    "lineNumber": { "type": "integer", "minimum": 1 },
                    "ruleId": { "type": "string", "pattern": "^LNT\\d{3}$" },
                    "severity": { "type": "string", "enum": ["error", "warning", "info"] },
                    "category": { "type": "string", "enum": ["Maintainability", "Performance", "Security", "Reliability", "Design", "Documentation", "Naming", "Testing"] },
                    "message": { "type": "string", "minLength": 1 }
                }
            }
        }
    }
}
```

---

## 3. ANSI Color Detection

### Decision: Auto-Detect with Environment Variable Overrides

**Chosen Algorithm** (precedence order):
1. **User flag override**: `--color=never` → disable, `--color=always` → enable
2. **NO_COLOR env var**: If set (any value) → disable color
3. **FORCE_COLOR env var**: If set → enable color (overrides redirection)
4. **Console.IsOutputRedirected**: If redirected to file/pipe → disable color
5. **TERM env var**: If `"dumb"` or unset → disable color
6. **Default**: Enable color (terminals support ANSI by default in 2025)

**Rationale**: Follows emerging standards (NO_COLOR, FORCE_COLOR), respects redirection, allows user control

### .NET Color Detection APIs

**Available in .NET 6+ (including .NET 10.0)**:
```csharp
// Detects if stdout is redirected to file/pipe
bool isRedirected = Console.IsOutputRedirected;

// Check environment variables
string noColor = Environment.GetEnvironmentVariable("NO_COLOR");
string forceColor = Environment.GetEnvironmentVariable("FORCE_COLOR");
string term = Environment.GetEnvironmentVariable("TERM");
```

**No additional NuGet packages required** - all built-in to .NET 10.0

### Platform Compatibility

| Platform | ANSI Support | Notes |
|----------|--------------|-------|
| Windows 10+ (1607+) | ✅ | .NET 6+ enables automatically via VT100 mode |
| Windows Terminal | ✅ | Full support, default in Windows 11 |
| macOS Terminal | ✅ | Universal support |
| Linux terminals | ✅ | xterm, gnome-terminal, konsole all support |
| PowerShell 7+ (pwsh) | ✅ | Full ANSI support |
| PowerShell ISE | ❌ | No ANSI support (legacy) |
| cmd.exe (Windows 10+) | ✅ | Supported since Anniversary Update |

**Edge Case**: PowerShell ISE detection
```csharp
// Detect PowerShell ISE (no ANSI support)
bool isPowerShellISE = Environment.GetEnvironmentVariable("PSISE") != null;
if (isPowerShellISE) return false; // disable color
```

### Environment Variable Standards

**NO_COLOR** (2017 informal standard, 300+ tools support):
- URL: https://no-color.org/
- Behavior: If set to ANY value → disable color
- Example: `NO_COLOR=1 lintelligent scan` → no ANSI codes

**FORCE_COLOR** (2023 emerging standard):
- Behavior: If set → enable color even if output redirected
- Example: `FORCE_COLOR=1 lintelligent scan > output.txt` → includes ANSI codes
- Use case: Generating colorized logs for HTML viewers

**TERM** (Unix standard):
- Values: `xterm-256color`, `screen`, `dumb`, etc.
- `dumb` or unset → no color support
- Example: `TERM=dumb lintelligent scan` → no ANSI codes

**COLORTERM** (modern terminals):
- Values: `truecolor`, `24bit`
- Presence indicates advanced color support (not used for detection, just FYI)

### ANSI Escape Code Reference

**Severity Colors** (recommended bright variants for visibility):
```csharp
public static class AnsiColors
{
    public const string Red = "\x1b[91m";        // Bright Red (Error)
    public const string Yellow = "\x1b[93m";     // Bright Yellow (Warning)
    public const string Cyan = "\x1b[96m";       // Bright Cyan (Info)
    public const string Reset = "\x1b[0m";       // Reset all formatting
    
    // Alternative: Standard colors (less bright)
    // public const string Red = "\x1b[31m";
    // public const string Yellow = "\x1b[33m";
    // public const string Blue = "\x1b[34m";
}
```

**Usage**:
```csharp
string formatted = $"{AnsiColors.Red}Error:{AnsiColors.Reset} Message text";
// Output: "\x1b[91mError:\x1b[0mMessage text"
// Renders: Error: Message text (red "Error", normal "Message text")
```

### CI/CD Environment Detection

**Color-supporting CI environments**:
- ✅ GitHub Actions: `TERM=xterm`, supports ANSI
- ✅ Azure Pipelines: Supports ANSI colors in logs
- ✅ GitLab CI: Supports ANSI
- ✅ CircleCI: Supports ANSI
- ❌ Jenkins (without plugins): Limited color support
- ❌ AppVeyor: No ANSI support

**Detection**:
```csharp
// GitHub Actions
bool isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

// Azure Pipelines
bool isAzurePipelines = Environment.GetEnvironmentVariable("TF_BUILD") == "True";
```

### Recommended Implementation Strategy

```csharp
public static bool ShouldUseColor(string? colorFlag)
{
    // 1. User override
    if (colorFlag == "never") return false;
    if (colorFlag == "always") return true;
    
    // 2. NO_COLOR standard
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
        return false;
    
    // 3. FORCE_COLOR overrides redirection
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FORCE_COLOR")))
        return true;
    
    // 4. PowerShell ISE doesn't support ANSI
    if (Environment.GetEnvironmentVariable("PSISE") != null)
        return false;
    
    // 5. Redirection check
    if (Console.IsOutputRedirected)
        return false;
    
    // 6. TERM check
    string? term = Environment.GetEnvironmentVariable("TERM");
    if (term == "dumb" || string.IsNullOrEmpty(term))
        return false;
    
    // 7. Default: enable (modern terminals support ANSI)
    return true;
}
```

**CLI Flag Design**: Add `--color=<auto|always|never>` (default: `auto`)
- `auto`: Use detection algorithm above
- `always`: Force color (ignores all detection)
- `never`: Disable color (plain text)

---

## Decisions Summary

| Technology | Decision | Rationale |
|------------|----------|-----------|
| **SARIF Package** | Microsoft.CodeAnalysis.Sarif v4.x | Official Microsoft package, type-safe, validation |
| **SARIF Column Info** | Omit (line-only regions) | DiagnosticResult has no column data, acceptable per spec |
| **JSON Schema** | Flat array with status wrapper | Streaming-friendly, jq-compatible, industry standard |
| **JSON Naming** | camelCase | Matches .NET, SARIF, jq conventions |
| **Severity Format** | String (`"error"`, `"warning"`, `"info"`) | Self-documenting, industry standard |
| **Performance** | Buffered JSON (Phase 1) | Simple, 10K violations = ~10MB (acceptable) |
| **Color Detection** | Auto-detect with env var overrides | Respects NO_COLOR/FORCE_COLOR standards, user control |
| **Color Codes** | Bright ANSI (91/93/96) | Better visibility on dark terminals |
| **Color CLI Flag** | `--color=auto\|always\|never` | Industry standard pattern (git, ripgrep, etc.) |

---

## Next Steps (Phase 1: Design & Contracts)

1. ✅ Research complete (this document)
2. **Generate data-model.md**: Define IReportFormatter, OutputConfiguration, SARIF mappings
3. **Generate contracts/**: JSON schema file, sample SARIF outputs
4. **Generate quickstart.md**: Usage examples for all 3 formats
5. **Update agent context**: Add SARIF package, JSON patterns to `.copilot.md`

**Ready for Phase 1** ✅
