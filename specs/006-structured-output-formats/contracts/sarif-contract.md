# SARIF 2.1.0 Contract

**Feature**: 006-structured-output-formats  
**Format**: SARIF (Static Analysis Results Interchange Format)  
**Version**: 2.1.0  
**Specification**: https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html

## Overview

SARIF 2.1.0 is the industry standard format for static analysis results, designed for IDE integration (VS Code, Visual Studio), security tooling (GitHub Code Scanning), and cross-tool interoperability.

## Required NuGet Package

```xml
<PackageReference Include="Microsoft.CodeAnalysis.Sarif" Version="4.*" />
```

**Rationale**: Official Microsoft package provides type-safe SARIF object model, validation, and serialization. Prevents schema violations via compile-time checks.

## Schema Reference

- **Official Spec**: https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html
- **JSON Schema**: https://json.schemastore.org/sarif-2.1.0.json
- **Microsoft Docs**: https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.sarif

## Example Outputs

See `sarif-examples/` directory:
- `valid-output.sarif` - Standard output with 3 violations
- `empty-results.sarif` - No violations found (valid SARIF)

## Lintelligent SARIF Mapping

### Top-Level Structure

```json
{
  "version": "2.1.0",
  "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
  "runs": [/* single run per analysis */]
}
```

### Tool Metadata

**Minimal (required)**:
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

**Complete (recommended for IDE integration)**:
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
          "shortDescription": { "text": "Method exceeds maximum statement count" },
          "fullDescription": { "text": "Methods should not exceed 20 statements..." },
          "helpUri": "https://lintelligent.dev/rules/LNT001",
          "defaultConfiguration": { "level": "warning" },
          "properties": { "category": "Maintainability" }
        }
        /* One entry per unique ruleId in results */
      ]
    }
  }
}
```

**Rules Array**: Generated from unique `RuleId` values in DiagnosticResult collection
- `id`: RuleId (e.g., "LNT001")
- `helpUri`: Link to rule documentation (FR-015)
- `defaultConfiguration.level`: Mapped severity (error/warning/note)
- `properties.category`: Lintelligent category (Maintainability, Performance, etc.)

### Result Object

```json
{
  "ruleId": "LNT001",
  "level": "warning",
  "message": {
    "text": "Method 'CreateUser' exceeds 20 statements (found: 27)"
  },
  "locations": [
    {
      "physicalLocation": {
        "artifactLocation": {
          "uri": "file:///C:/Projects/MyApp/src/Services/UserService.cs",
          "uriBaseId": "SRCROOT"
        },
        "region": {
          "startLine": 42
        }
      }
    }
  ]
}
```

**Key Mappings**:
- `ruleId`: DiagnosticResult.RuleId
- `level`: Severity.Error → "error", Severity.Warning → "warning", Severity.Info → "note"
- `message.text`: DiagnosticResult.Message
- `locations[0].physicalLocation.artifactLocation.uri`: Absolute file:// URI
- `locations[0].physicalLocation.region.startLine`: DiagnosticResult.LineNumber
- **NO** `region.startColumn` or `region.endColumn` (line-only precision per Clarification #2)

### File URI Format

**Windows**: `file:///C:/Projects/MyApp/src/Foo.cs`  
**Unix**: `file:///home/user/projects/myapp/src/Foo.cs`

**Conversion**:
```csharp
var absolutePath = Path.GetFullPath(diagnosticResult.FilePath);
var uri = new Uri(absolutePath, UriKind.Absolute);
// uri.ToString() → "file:///C:/Projects/MyApp/src/Foo.cs"
```

### Region Object (Line-Only Precision)

```json
{
  "region": {
    "startLine": 42
  }
}
```

**Clarification #2**: DiagnosticResult has only line numbers (no column information). SARIF 2.1.0 allows optional `startColumn`/`endColumn` fields. We omit them, which is acceptable per specification.

**Optional Fields Not Used**:
- `startColumn` - DiagnosticResult doesn't have column info
- `endColumn` - DiagnosticResult doesn't have column info
- `endLine` - Single-line violations only
- `charOffset` - Not tracked
- `charLength` - Not tracked

## Severity Level Mapping

| Lintelligent Severity | SARIF Level | Description |
|----------------------|-------------|-------------|
| `Severity.Error` | `"error"` | Build-breaking violation |
| `Severity.Warning` | `"warning"` | Should be fixed but not critical |
| `Severity.Info` | `"note"` | Informational suggestion |

**Implementation**:
```csharp
private static FailureLevel MapSeverity(Severity severity)
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

## IDE Integration

### VS Code

**Requirements**:
- SARIF version MUST be exactly "2.1.0" (older versions rejected)
- `tool.driver.name` required
- `locations[0].physicalLocation` required for file navigation
- `region.startLine` required for line navigation
- File URIs can be workspace-relative or absolute (VS Code resolves both)

**Import Methods**:
1. **SARIF Viewer Extension**: Open SARIF file directly
2. **Native Problems Panel**: VS Code imports SARIF natively (2023+)
3. **Command Palette**: "SARIF: Open SARIF log file"

**Display**:
- Problems panel shows: `[LNT001] Method 'CreateUser' exceeds 20 statements (found: 27) (UserService.cs, Ln 42)`
- Clicking navigates to `UserService.cs:42`
- Severity icons: ❌ (error), ⚠️ (warning), ℹ️ (note)

### Visual Studio

**Requirements**:
- Same as VS Code (SARIF 2.1.0 strict)
- `helpUri` recommended (opens documentation when user clicks "Help" icon)
- `tool.driver.version` recommended for telemetry

**Display**:
- Error List window with filterable columns
- Right-click → "Show Help" opens `helpUri`

### GitHub Code Scanning

**Upload**:
```yaml
- name: Upload SARIF to GitHub
  uses: github/codeql-action/upload-sarif@v2
  with:
    sarif_file: results.sarif
```

**Requirements**:
- `tool.driver.name` required
- `results[].ruleId` required for deduplication
- **Recommended**: `partialFingerprints` for cross-run tracking (we'll omit in Phase 1)
- **Recommended**: `startColumn`/`endColumn` for precise highlighting (we omit, acceptable)
- Max 10MB per file, 25,000 results per run

**Display**:
- Security tab → Code Scanning alerts
- Grouped by rule
- Clickable file references navigate to code

## Validation

### Using Microsoft.CodeAnalysis.Sarif Validator

```bash
# Install validator
dotnet tool install --global Microsoft.CodeAnalysis.Sarif.Multitool

# Validate SARIF file
sarif validate results.sarif

# Expected output:
# results.sarif: Valid
```

### Using Online Validator

https://sarifweb.azurewebsites.net/Validation

Upload SARIF file → validates against 2.1.0 schema

### Programmatic Validation

```csharp
using Microsoft.CodeAnalysis.Sarif.Validation;

var validator = new SarifValidator();
var results = validator.Validate(sarifLog);

if (results.Any())
{
    foreach (var result in results)
    {
        Console.WriteLine($"{result.Level}: {result.Message}");
    }
}
else
{
    Console.WriteLine("SARIF is valid!");
}
```

## Edge Cases

| Case | Behavior |
|------|----------|
| Empty results (0 violations) | Valid SARIF with `results: []` and `rules: []` |
| File path with spaces | URI-encode spaces: `file:///C:/My%20Project/Foo.cs` |
| File path with Unicode | UTF-8 encode, URI format handles automatically |
| Very long message text | Truncate at 10,000 characters (SARIF limit) |
| Null or whitespace message | Replace with default: "Violation found (no message provided)" |
| Invalid URI characters | `Uri` constructor escapes automatically |

## Performance Considerations

**10,000 Violations** (SC-008 requirement):
- SARIF object model is in-memory (no streaming in Microsoft.CodeAnalysis.Sarif v4.x)
- Estimated size: ~500 bytes per result × 10,000 = ~5MB JSON
- Serialization time: ~2-3 seconds (well under 10-second target)
- Memory usage: ~50MB peak (acceptable)

**Optimization**: Reuse `ReportingDescriptor` objects for duplicate rules (done automatically by grouping unique RuleIds)

## Testing Strategy

### Unit Tests (SarifFormatterTests.cs)

1. **Schema Validation**: Output validates against SARIF 2.1.0 schema
2. **Tool Metadata**: `tool.driver.name`, `tool.driver.version` present
3. **Rules Array**: One `ReportingDescriptor` per unique RuleId
4. **HelpUri**: Each rule has `helpUri` field (FR-015)
5. **File URIs**: Absolute `file://` format
6. **Region**: `startLine` present, NO `startColumn`/`endColumn`
7. **Severity Mapping**: Error/Warning/Info → error/warning/note
8. **Empty Results**: Valid SARIF with empty arrays
9. **Special Characters**: Message with quotes/newlines/Unicode serializes correctly
10. **Performance**: 10,000 results format in <10 seconds

### Integration Tests (CLI)

1. `lintelligent scan --format sarif` produces valid SARIF
2. VS Code can import SARIF (manual test with real IDE)
3. GitHub Code Scanning accepts SARIF (test upload to GitHub API)
4. SARIF validator CLI reports "Valid"

## References

- OASIS SARIF Spec: https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html
- JSON Schema: https://json.schemastore.org/sarif-2.1.0.json
- Microsoft Docs: https://docs.microsoft.com/en-us/code-analysis/sarif-sdk/
- GitHub Code Scanning SARIF: https://docs.github.com/en/code-security/code-scanning/integrating-with-code-scanning/sarif-support-for-code-scanning

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.1.0 | 2025-12-25 | Initial SARIF contract for Feature 006 |
