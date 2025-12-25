# Quick Start Guide: Structured Output Formats

**Feature**: 006-structured-output-formats  
**Audience**: Developers, DevOps Engineers  
**Last Updated**: 2025-12-25

## Overview

Lintelligent supports three output formats to fit different use cases:
- **JSON**: Machine-readable format for CI/CD pipelines and automated tooling
- **SARIF**: Industry standard for IDE integration (VS Code, Visual Studio) and security tools (GitHub Code Scanning)
- **Markdown**: Human-readable format with optional color-coded severity levels

---

## Basic Usage

### Default Output (Markdown to stdout)

```bash
lintelligent scan
```

**Output**:
```markdown
# Lintelligent Report

## Summary

| Severity | Count |
|----------|-------|
| Error    | 1     |
| Warning  | 2     |
| Info     | 0     |
| **Total**| **3** |

## Violations by File

### src/Services/UserService.cs (2 violations)

- **[WARNING] LNT001** (Line 42): Method 'CreateUser' exceeds 20 statements (found: 27) (Maintainability)
- **[WARNING] LNT002** (Line 105): Method 'UpdateUserProfile' has 8 parameters (limit: 5) (Design)

### src/Models/User.cs (1 violation)

- **[ERROR] LNT005** (Line 15): Class 'User' has 22 methods (limit: 15) (Maintainability)
```

---

## JSON Output

### Generate JSON to stdout

```bash
lintelligent scan --format json
```

**Output**:
```json
{
  "status": "success",
  "summary": {
    "total": 3,
    "bySeverity": {
      "error": 1,
      "warning": 2
    }
  },
  "violations": [
    {
      "filePath": "src/Services/UserService.cs",
      "lineNumber": 42,
      "ruleId": "LNT001",
      "severity": "warning",
      "category": "Maintainability",
      "message": "Method 'CreateUser' exceeds 20 statements (found: 27)"
    },
    {
      "filePath": "src/Services/UserService.cs",
      "lineNumber": 105,
      "ruleId": "LNT002",
      "severity": "warning",
      "category": "Design",
      "message": "Method 'UpdateUserProfile' has 8 parameters (limit: 5)"
    },
    {
      "filePath": "src/Models/User.cs",
      "lineNumber": 15,
      "ruleId": "LNT005",
      "severity": "error",
      "category": "Maintainability",
      "message": "Class 'User' has 22 methods (limit: 15)"
    }
  ]
}
```

### Save JSON to file

```bash
lintelligent scan --format json --output results.json
```

**Result**: JSON written to `results.json`, only summary message on stdout:
```
Analysis complete. Results written to results.json
```

### Parse JSON with jq

```bash
# Count errors
lintelligent scan --format json | jq '.summary.bySeverity.error'

# Filter by severity
lintelligent scan --format json | jq '.violations[] | select(.severity == "error")'

# Group by file
lintelligent scan --format json | jq 'group_by(.filePath) | map({file: .[0].filePath, count: length})'
```

### Parse JSON with PowerShell

```powershell
# Parse JSON
$results = lintelligent scan --format json | ConvertFrom-Json

# Count warnings
$results.summary.bySeverity.warning

# Filter by category
$results.violations | Where-Object { $_.category -eq "Security" }
```

---

## SARIF Output

### Generate SARIF to stdout

```bash
lintelligent scan --format sarif
```

**Output**: Valid SARIF 2.1.0 JSON (see `contracts/sarif-examples/valid-output.sarif` for full example)

### Save SARIF to file

```bash
lintelligent scan --format sarif --output results.sarif
```

### Import SARIF into VS Code

**Method 1: SARIF Viewer Extension**
1. Install "SARIF Viewer" extension
2. Open Command Palette (`Ctrl+Shift+P` or `Cmd+Shift+P`)
3. Run "SARIF: Open SARIF log file"
4. Select `results.sarif`
5. Violations appear in Problems panel

**Method 2: Native Import (VS Code 2023+)**
1. Open Problems panel (`Ctrl+Shift+M` or `Cmd+Shift+M`)
2. Click "..." menu → "Import SARIF file"
3. Select `results.sarif`

**Result**: Clickable violations in Problems panel with file navigation

### Upload SARIF to GitHub Code Scanning

**GitHub Actions Workflow**:
```yaml
name: Code Quality

on: [push, pull_request]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Run Lintelligent Analysis
        run: |
          dotnet tool install --global Lintelligent.Cli
          lintelligent scan --format sarif --output results.sarif
      
      - name: Upload SARIF to GitHub
        uses: github/codeql-action/upload-sarif@v2
        with:
          sarif_file: results.sarif
```

**Result**: Violations appear in Security tab → Code Scanning alerts

---

## Enhanced Markdown with Colors

### Enable colors (auto-detected by default)

```bash
lintelligent scan --format markdown
```

**Terminal Output** (with ANSI colors):
```
# Lintelligent Report

## Summary

| Severity | Count |
|----------|-------|
| Error    | 1     |
| Warning  | 2     |

## Violations by File

### src/Services/UserService.cs (2 violations)

- [WARNING] LNT001 (Line 42): Method 'CreateUser' exceeds 20 statements (found: 27)
- [WARNING] LNT002 (Line 105): Method 'UpdateUserProfile' has 8 parameters (limit: 5)

### src/Models/User.cs (1 violation)

- [ERROR] LNT005 (Line 15): Class 'User' has 22 methods (limit: 15)
```

**Color Coding**:
- 🔴 **[ERROR]** - Bright Red
- 🟡 **[WARNING]** - Bright Yellow
- 🔵 **[INFO]** - Bright Cyan

### Force colors (even when redirected)

```bash
FORCE_COLOR=1 lintelligent scan --format markdown > report.txt
```

**Result**: ANSI codes included in `report.txt` (useful for HTML viewers)

### Disable colors

```bash
NO_COLOR=1 lintelligent scan --format markdown
# OR
lintelligent scan --format markdown --color=never
```

**Result**: Plain text markdown without ANSI codes

---

## Group Violations

### Group by file (default)

```bash
lintelligent scan --format markdown
```

**Output**: Violations grouped under file headings

### Group by category

```bash
lintelligent scan --format markdown --group-by category
```

**Output**:
```markdown
## Maintainability

- **[WARNING] LNT001** (src/Services/UserService.cs:42): Method 'CreateUser' exceeds 20 statements
- **[ERROR] LNT005** (src/Models/User.cs:15): Class 'User' has 22 methods

## Design

- **[WARNING] LNT002** (src/Services/UserService.cs:105): Method 'UpdateUserProfile' has 8 parameters
```

---

## CI/CD Integration Examples

### GitHub Actions: Fail on Errors

```yaml
- name: Run Analysis
  run: lintelligent scan --format json --output results.json
  
- name: Check for errors
  run: |
    ERROR_COUNT=$(jq '.summary.bySeverity.error // 0' results.json)
    if [ "$ERROR_COUNT" -gt 0 ]; then
      echo "❌ Found $ERROR_COUNT errors"
      jq '.violations[] | select(.severity == "error")' results.json
      exit 1
    fi
```

### Azure Pipelines: Publish SARIF as Artifact

```yaml
- task: PowerShell@2
  displayName: 'Run Lintelligent Analysis'
  inputs:
    targetType: 'inline'
    script: |
      lintelligent scan --format sarif --output $(Build.ArtifactStagingDirectory)/results.sarif

- task: PublishBuildArtifacts@1
  displayName: 'Publish SARIF Results'
  inputs:
    pathToPublish: '$(Build.ArtifactStagingDirectory)/results.sarif'
    artifactName: 'CodeAnalysis'
```

### GitLab CI: Fail on Warnings or Errors

```yaml
code_quality:
  script:
    - lintelligent scan --format json --output results.json
    - |
      TOTAL=$(jq '.summary.total' results.json)
      if [ "$TOTAL" -gt 0 ]; then
        echo "Found $TOTAL violations"
        jq '.violations[]' results.json
        exit 1
      fi
  artifacts:
    paths:
      - results.json
    expire_in: 1 week
```

---

## Output to File vs Stdout

### Default: Output to stdout

```bash
lintelligent scan --format json
```

**Result**: JSON printed to console (can be piped)

### Output to file

```bash
lintelligent scan --format json --output results.json
```

**Result**:
- JSON written to `results.json`
- Only summary printed to console: `"Analysis complete. Results written to results.json"`

### Explicit stdout (for clarity in scripts)

```bash
lintelligent scan --format json --output -
```

**Result**: Same as default (stdout), but explicit

---

## Error Handling

### Invalid format

```bash
lintelligent scan --format xml
```

**Output**:
```
Error: Invalid format 'xml'. Valid formats: json, sarif, markdown
Exit code: 2
```

### Non-writable output path

```bash
lintelligent scan --format json --output /read-only/path/results.json
```

**Output**:
```
Error: Output path is read-only or not writable: /read-only/path/results.json
Exit code: 1
```

### Directory doesn't exist

```bash
lintelligent scan --format json --output /nonexistent/dir/results.json
```

**Output**:
```
Error: Output directory does not exist: /nonexistent/dir. Create the directory or specify a valid path.
Exit code: 1
```

---

## Performance Expectations

### Small Projects (< 100 files)

- **Analysis**: ~1-2 seconds
- **Formatting**: < 100ms (all formats)
- **Total**: ~2 seconds

### Medium Projects (100-1,000 files)

- **Analysis**: ~10-20 seconds
- **Formatting**: < 500ms (all formats)
- **Total**: ~20 seconds

### Large Projects (1,000-10,000 files)

- **Analysis**: ~60-120 seconds
- **Formatting**: < 10 seconds (SC-008 guarantee)
- **Total**: ~2 minutes

**Note**: Formatting time is independent of file count, depends only on violation count.

---

## Common Workflows

### Daily Development: Quick Feedback

```bash
# Fast markdown output with colors
lintelligent scan
```

### Pre-Commit: Fail on Errors

```bash
lintelligent scan --format json | jq -e '.summary.bySeverity.error == 0' || {
  echo "Errors found! Fix before committing."
  exit 1
}
```

### CI/CD: Comprehensive SARIF Report

```bash
lintelligent scan --format sarif --output results.sarif
# Upload to GitHub Code Scanning or archive as build artifact
```

### Weekly Review: Grouped by Category

```bash
lintelligent scan --format markdown --group-by category --output weekly-report.md
```

---

## Troubleshooting

### Colors not appearing in terminal

**Check**:
1. Terminal supports ANSI codes? (Windows Terminal, iTerm2, modern Linux terminals)
2. `NO_COLOR` environment variable set? (`echo $NO_COLOR`)
3. Using PowerShell ISE? (No ANSI support, use PowerShell 7+ instead)

**Fix**:
```bash
# Force colors
lintelligent scan --format markdown --color=always
```

### JSON schema validation failed

**Check**:
```bash
# Validate with jq
jq empty results.json && echo "Valid JSON"
```

**Common Issue**: Malformed JSON due to special characters in messages

### SARIF not importing into VS Code

**Check**:
1. SARIF version is 2.1.0? (`jq '.version' results.sarif` should return `"2.1.0"`)
2. File URIs are absolute? (should start with `file:///`)
3. `tool.driver.name` field present?

**Validate**:
```bash
# Install SARIF validator
dotnet tool install --global Microsoft.CodeAnalysis.Sarif.Multitool

# Validate SARIF
sarif validate results.sarif
```

---

## Next Steps

- **Full JSON Schema**: See `contracts/json-schema.md`
- **SARIF Contract**: See `contracts/sarif-contract.md`
- **Data Model**: See `data-model.md` for formatter architecture
- **Implementation Plan**: See `plan.md` for development roadmap

---

## Summary Table

| Format | Best For | Output Destination | Color Support |
|--------|----------|-------------------|---------------|
| **JSON** | CI/CD pipelines, automated parsing | stdout, file | N/A |
| **SARIF** | IDE integration, GitHub Code Scanning | stdout, file | N/A |
| **Markdown** | Human review, terminal output | stdout, file | Yes (auto-detected) |

**Default Format**: Markdown  
**Default Destination**: stdout  
**Color Detection**: Auto (respects NO_COLOR, FORCE_COLOR, redirection)
