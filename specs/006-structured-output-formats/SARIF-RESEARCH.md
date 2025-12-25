# SARIF 2.1.0 Research for Lintelligent Structured Output

**Date:** December 25, 2025  
**Context:** Implementing SARIF formatter for C# static analysis CLI tool  
**Target:** Convert DiagnosticResult objects to valid SARIF 2.1.0 for VS Code and GitHub Code Scanning

---

## SARIF 2.1.0 Schema Reference

### Official Resources

- **Official Schema URL:** `https://docs.oasis-open.org/sarif/sarif/v2.1.0/errata01/os/schemas/sarif-schema-2.1.0.json`
- **Specification:** [OASIS SARIF v2.1.0 Complete Specification](https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html)
- **GitHub JSON Schema Store:** `https://json.schemastore.org/sarif-2.1.0.json` (alternative URL)

### Key Required Fields

**Top-Level (`sarifLog`):**
- `version` (string): MUST be `"2.1.0"`
- `runs` (array): Array of one or more `run` objects

**Run Object:**
- `tool` (object): MUST contain `tool.driver` with analysis tool metadata
- `results` (array): Array of zero or more result objects (empty array if no issues found)

**Tool Driver:**
- `name` (string): Required tool name
- `rules` (array): Recommended - array of `reportingDescriptor` objects defining rules

**Result Object:**
- `message.text` (string): Required description of the result
- `locations` (array): Required - at least one location where result was detected
- `ruleId` (string): Optional but recommended for filtering

**Location/PhysicalLocation:**
- `artifactLocation.uri` (string): Required - relative or absolute file path
- `region.startLine` (integer): Required - 1-based line number
- `region.startColumn` (integer): Required for GitHub - column number
- `region.endColumn` (integer): Required for GitHub - end column

### Key Optional Fields

**Result Object:**
- `level` (string): `"none"`, `"note"`, `"warning"`, `"error"` (GitHub uses for severity display)
- `partialFingerprints` (object): Strongly recommended for GitHub - enables result tracking across commits
- `ruleIndex` (integer): Index into `tool.driver.rules` array
- `kind` (string): `"fail"`, `"pass"`, `"open"`, `"review"`, `"notApplicable"`, `"informational"`

**Rule Descriptor (reportingDescriptor):**
- `id` (string): Required - unique rule identifier
- `shortDescription.text` (string): Required - concise rule description (max 1024 chars)
- `fullDescription.text` (string): Required - detailed description
- `helpUri` (string): Optional - URL to rule documentation
- `help.text` (string): Required - help text
- `help.markdown` (string): Recommended - Markdown formatted help
- `defaultConfiguration.level` (string): Default severity level
- `properties.tags[]` (array): Tags for filtering (e.g., `["security", "performance"]`)
- `properties.precision` (string): `"very-high"`, `"high"`, `"medium"`, `"low"`

---

## NuGet Package Recommendation

### Microsoft.CodeAnalysis.Sarif v4.x

**Recommended Package:**
- **Name:** `Microsoft.CodeAnalysis.Sarif`
- **Version:** 4.0.0+ (latest stable in v4.x line)
- **NuGet URL:** https://www.nuget.org/packages/Microsoft.CodeAnalysis.Sarif

**Rationale:**
1. **Official Microsoft Package:** Maintained by Microsoft's SARIF team, used by CodeQL and other Microsoft tools
2. **Full Object Model:** Provides complete C# object model for SARIF 2.1.0 - avoids manual JSON construction
3. **Mature v4.x Line:** Version 4.x targets .NET Standard 2.0+ and includes all SARIF 2.1.0 features
4. **Validation Built-In:** Includes schema validation to catch errors before output
5. **Used by Roslyn Compiler:** The C# compiler (`csc.exe`) uses this package for `/errorlog` SARIF output

**Alternative Considerations:**
- The Roslyn compiler includes `Microsoft.CodeAnalysis.SarifVersionFacts` for version handling
- VS Code SARIF Viewer extension strictly requires SARIF 2.1.0 (older versions rejected)
- Azure DevOps Advanced Security also requires SARIF 2.1.0

---

## Data Mapping Strategy (DiagnosticResult → SARIF)

### DiagnosticResult Properties
```
FilePath     → string (absolute or relative path)
LineNumber   → int (1-based line number)
RuleId       → string (e.g., "LI001", "CA2101")
Severity     → enum (Error, Warning, Info, etc.)
Message      → string (diagnostic message)
Category     → string (e.g., "Performance", "Security")
```

### SARIF Result Object Mapping

```json
{
  "ruleId": "<DiagnosticResult.RuleId>",
  "ruleIndex": <index in tool.driver.rules array>,
  "level": "<mapped from DiagnosticResult.Severity>",
  "message": {
    "text": "<DiagnosticResult.Message>"
  },
  "locations": [
    {
      "physicalLocation": {
        "artifactLocation": {
          "uri": "<DiagnosticResult.FilePath (relative)>",
          "uriBaseId": "%SRCROOT%" // Optional base ID for path resolution
        },
        "region": {
          "startLine": <DiagnosticResult.LineNumber>,
          "startColumn": 1,  // Default - no column info available
          "endColumn": 1000   // Arbitrary large value for whole-line highlight
        }
      }
    }
  ],
  "partialFingerprints": {
    "primaryLocationLineHash": "<computed hash>"  // GitHub requirement
  }
}
```

### Severity Mapping (DiagnosticResult → SARIF Level)

| DiagnosticResult.Severity | SARIF `level`    | Notes |
|---------------------------|------------------|-------|
| Error                     | `"error"`        | Critical issues |
| Warning                   | `"warning"`      | Default for most rules |
| Info                      | `"note"`         | Informational |
| Hidden/Suggestion         | `"note"`         | Low priority |

### Line-Only Regions (No Column Information)

**Challenge:** DiagnosticResult only has `LineNumber`, no column data.

**Solution:** SARIF supports line-only regions:
- Set `region.startLine` to the line number
- For GitHub: Also set `region.startColumn = 1` and `region.endColumn` to approximate end
- For VS Code: Can omit columns, but setting them provides better highlighting

**Best Practice:**
```csharp
region = new Region
{
    StartLine = diagnosticResult.LineNumber,
    StartColumn = 1,  // Start of line
    EndColumn = int.MaxValue  // Or read file to get actual line length
}
```

---

## Tool Metadata Requirements

### Minimal Tool Metadata (Required)

```json
{
  "tool": {
    "driver": {
      "name": "Lintelligent",
      "version": "1.0.0",  // Or semanticVersion
      "informationUri": "https://github.com/yourusername/Lintelligent"
    }
  }
}
```

### Complete Tool Metadata (Recommended)

```json
{
  "tool": {
    "driver": {
      "name": "Lintelligent",
      "fullName": "Lintelligent Static Analysis Tool",
      "version": "1.0.0",
      "semanticVersion": "1.0.0",
      "informationUri": "https://github.com/yourusername/Lintelligent",
      "rules": [
        {
          "id": "LI001",
          "name": "LongMethodRule",
          "shortDescription": {
            "text": "Method exceeds maximum line count"
          },
          "fullDescription": {
            "text": "Methods should not exceed the configured maximum line count to maintain readability and testability."
          },
          "defaultConfiguration": {
            "level": "warning"
          },
          "helpUri": "https://github.com/yourusername/Lintelligent/docs/rules/LI001.md",
          "help": {
            "text": "Refactor long methods into smaller, focused methods.",
            "markdown": "**Refactor** long methods into smaller, focused methods. See [documentation](https://github.com/yourusername/Lintelligent/docs/rules/LI001.md)."
          },
          "properties": {
            "tags": ["maintainability", "complexity"],
            "precision": "high"
          }
        }
      ]
    }
  }
}
```

### Rules Array Best Practices

1. **Pre-populate rules:** Include ALL rules that might be detected (even if no results for some)
2. **Use `ruleIndex`:** Reference rules by index in result objects for compact output
3. **Include help:** Provide `helpUri` and `help.markdown` for viewer integration
4. **Set default level:** Use `defaultConfiguration.level` to set rule severity

---

## IDE Integration Notes

### VS Code SARIF Viewer Extension

**Extension:** `MS-SarifVSCode.sarif-viewer` (v3.4.5+)

**Requirements:**
- **Version:** Strictly SARIF 2.1.0 (older versions rejected)
- **File Extension:** `.sarif` or `.sarif.json`
- **Display:** Shows results in Problems panel and dedicated SARIF Results Panel

**Critical Fields for VS Code:**
- `result.locations[0].physicalLocation.artifactLocation.uri` - Must resolve to workspace files
- `result.message.text` - Displayed as title in Problems panel
- `result.level` - Used for icon/severity (error, warning, info)
- `region.startLine` - Required for navigation
- `region.startColumn` - Recommended for precise highlighting (can default to 1)

**URI Resolution:**
- VS Code auto-reconciles URIs between SARIF and workspace in most cases
- Recommend using **relative paths** from workspace root
- Can use `run.originalUriBaseIds` to define base paths for resolution

**Viewer Features:**
- Keyboard accessible results list
- Resizable details panel
- Filtering by severity, rule, file
- Click result → navigate to code location
- Show squiggles in editor

### GitHub Code Scanning

**Platform:** GitHub Advanced Security / GitHub Code Security

**Requirements:**
- **Version:** SARIF 2.1.0 only
- **Max File Size:** 10 MB (gzip-compressed)
- **Upload Methods:**
  - GitHub Actions: `github/codeql-action/upload-sarif@v2`
  - REST API: `/code-scanning/sarifs` endpoint

**Critical Fields for GitHub:**

| Field | Requirement | Notes |
|-------|-------------|-------|
| `partialFingerprints.primaryLocationLineHash` | **STRONGLY RECOMMENDED** | GitHub uses this to track results across commits. If missing, GitHub attempts to compute it. |
| `result.locations[0].physicalLocation` | **REQUIRED** | Must have valid `artifactLocation.uri` and `region` |
| `region.startLine` | **REQUIRED** | 1-based line number |
| `region.startColumn` | **REQUIRED** | Column number (GitHub requirement) |
| `region.endColumn` | **REQUIRED** | End column for highlighting |
| `result.message.text` | **REQUIRED** | Alert title in UI |
| `tool.driver.name` | **REQUIRED** | Tool identification |

**Limits (GitHub):**

| Object | Limit | Notes |
|--------|-------|-------|
| Runs per file | 20 | Max 20 runs in one SARIF file |
| Results per run | 25,000 | Only top 5,000 shown (by severity) |
| Rules per run | 25,000 | - |
| Locations per result | 1,000 | Only 100 displayed |
| Tags per rule | 20 | Only 10 displayed |

**Fingerprint Generation:**
- GitHub computes `partialFingerprints` if missing (when using `upload-sarif` action)
- For API uploads, you MUST provide `partialFingerprints` to avoid duplicate alerts
- Algorithm: Hash of file path + line number + snippet
- See: https://github.com/github/codeql-action/blob/main/src/fingerprints.ts

**Category for Multiple Uploads:**
- Use `run.automationDetails.id` to distinguish multiple analyses
- Format: `"<category>/<run-id>"` (e.g., `"csharp-analysis/2024-12-25"`)
- Allows uploading multiple SARIF files for same commit without overwriting

---

## Code Generation Approach

### Recommended: Object Model (Microsoft.CodeAnalysis.Sarif)

**Advantages:**
- Type-safe C# objects
- Automatic schema validation
- Less error-prone than manual JSON
- Built-in serialization
- Easier to maintain

**Example Usage:**
```csharp
using Microsoft.CodeAnalysis.Sarif;
using Newtonsoft.Json;

var sarifLog = new SarifLog
{
    Version = SarifVersion.Current,  // "2.1.0"
    Runs = new[]
    {
        new Run
        {
            Tool = new Tool
            {
                Driver = new ToolComponent
                {
                    Name = "Lintelligent",
                    InformationUri = new Uri("https://github.com/user/lintelligent"),
                    Rules = new[]
                    {
                        new ReportingDescriptor
                        {
                            Id = "LI001",
                            ShortDescription = new MultiformatMessageString
                            {
                                Text = "Method is too long"
                            }
                        }
                    }
                }
            },
            Results = new[]
            {
                new Result
                {
                    RuleId = "LI001",
                    Message = new Message { Text = "Method exceeds 50 lines" },
                    Locations = new[]
                    {
                        new Location
                        {
                            PhysicalLocation = new PhysicalLocation
                            {
                                ArtifactLocation = new ArtifactLocation
                                {
                                    Uri = new Uri("src/MyClass.cs", UriKind.Relative)
                                },
                                Region = new Region
                                {
                                    StartLine = 42,
                                    StartColumn = 1
                                }
                            }
                        }
                    },
                    Level = FailureLevel.Warning
                }
            }
        }
    }
};

// Serialize
var json = JsonConvert.SerializeObject(sarifLog, Formatting.Indented);
File.WriteAllText("output.sarif", json);
```

### Alternative: Manual JSON Serialization

**Only if:**
- You want minimal dependencies
- You need extreme control over output format
- Package size is critical concern

**Disadvantages:**
- Manual string construction error-prone
- No compile-time validation
- Must handle all SARIF schema complexities manually
- Harder to maintain

**Recommendation:** Use object model approach unless you have specific constraints.

---

## Sample SARIF Structure (Minimal Valid Example)

```json
{
  "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
  "version": "2.1.0",
  "runs": [
    {
      "tool": {
        "driver": {
          "name": "Lintelligent",
          "version": "1.0.0",
          "informationUri": "https://github.com/yourusername/Lintelligent",
          "rules": [
            {
              "id": "LI001",
              "shortDescription": {
                "text": "Method is too long"
              },
              "fullDescription": {
                "text": "Methods should not exceed the configured maximum line count."
              },
              "help": {
                "text": "Refactor long methods into smaller methods."
              },
              "defaultConfiguration": {
                "level": "warning"
              },
              "properties": {
                "tags": ["maintainability"],
                "precision": "high"
              }
            }
          ]
        }
      },
      "results": [
        {
          "ruleId": "LI001",
          "ruleIndex": 0,
          "level": "warning",
          "message": {
            "text": "Method 'ProcessData' exceeds 50 lines (found 75 lines)"
          },
          "locations": [
            {
              "physicalLocation": {
                "artifactLocation": {
                  "uri": "src/DataProcessor.cs"
                },
                "region": {
                  "startLine": 42,
                  "startColumn": 1,
                  "endColumn": 100
                }
              }
            }
          ],
          "partialFingerprints": {
            "primaryLocationLineHash": "a1b2c3d4:1"
          }
        }
      ]
    }
  ]
}
```

### Minimal Example (Empty Results)

```json
{
  "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
  "version": "2.1.0",
  "runs": [
    {
      "tool": {
        "driver": {
          "name": "Lintelligent"
        }
      },
      "results": []
    }
  ]
}
```

---

## Implementation Checklist

### Phase 1: Basic SARIF Output
- [ ] Add `Microsoft.CodeAnalysis.Sarif` NuGet package (v4.x)
- [ ] Create `SarifFormatter` class implementing output format
- [ ] Map `DiagnosticResult` to SARIF `Result` objects
- [ ] Handle line-only regions (startLine, startColumn=1)
- [ ] Set proper severity levels (error/warning/note)
- [ ] Generate basic tool metadata

### Phase 2: Enhanced Metadata
- [ ] Populate `tool.driver.rules` array with all rule definitions
- [ ] Add rule descriptions (`shortDescription`, `fullDescription`)
- [ ] Include `helpUri` links to documentation
- [ ] Add rule tags and precision metadata
- [ ] Use `ruleIndex` to reference rules efficiently

### Phase 3: GitHub Integration
- [ ] Implement `partialFingerprints` computation (hash of location+code)
- [ ] Ensure `region.startColumn` and `endColumn` are set
- [ ] Add `run.automationDetails.id` for category support
- [ ] Test with GitHub SARIF validator (https://sarifweb.azurewebsites.net/)
- [ ] Verify file size stays under 10 MB limit

### Phase 4: VS Code Integration
- [ ] Use relative paths for `artifactLocation.uri`
- [ ] Test with VS Code SARIF Viewer extension
- [ ] Verify Problems panel integration
- [ ] Ensure navigation works (click → jump to code)
- [ ] Test squiggle/highlight display in editor

---

## References

1. **OASIS SARIF Specification:** https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html
2. **SARIF Schema JSON:** https://docs.oasis-open.org/sarif/sarif/v2.1.0/errata01/os/schemas/sarif-schema-2.1.0.json
3. **Microsoft SARIF Tutorials:** https://github.com/microsoft/sarif-tutorials
4. **GitHub SARIF Support:** https://docs.github.com/en/code-security/code-scanning/integrating-with-code-scanning/sarif-support-for-code-scanning
5. **VS Code SARIF Viewer:** https://marketplace.visualstudio.com/items?itemName=MS-SarifVSCode.sarif-viewer
6. **SARIF Validator:** https://sarifweb.azurewebsites.net/
7. **Microsoft.CodeAnalysis.Sarif Package:** https://www.nuget.org/packages/Microsoft.CodeAnalysis.Sarif
8. **GitHub Fingerprint Generation:** https://github.com/github/codeql-action/blob/main/src/fingerprints.ts

---

**End of Research Document**
