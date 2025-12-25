# JSON Schema Contract

**Feature**: 006-structured-output-formats  
**Format**: JSON  
**Version**: 1.0

## Schema Definition (JSON Schema Draft 7)

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://lintelligent.dev/schemas/output-v1.json",
  "title": "Lintelligent JSON Output",
  "description": "Schema for Lintelligent static analysis JSON output format",
  "type": "object",
  "required": ["status", "summary", "violations"],
  "properties": {
    "status": {
      "type": "string",
      "enum": ["success", "error"],
      "description": "Overall analysis status"
    },
    "summary": {
      "type": "object",
      "required": ["total", "bySeverity"],
      "properties": {
        "total": {
          "type": "integer",
          "minimum": 0,
          "description": "Total number of violations found"
        },
        "bySeverity": {
          "type": "object",
          "description": "Violation counts grouped by severity level",
          "properties": {
            "error": {
              "type": "integer",
              "minimum": 0
            },
            "warning": {
              "type": "integer",
              "minimum": 0
            },
            "info": {
              "type": "integer",
              "minimum": 0
            }
          },
          "additionalProperties": false
        }
      },
      "additionalProperties": false
    },
    "violations": {
      "type": "array",
      "description": "Array of diagnostic violations",
      "items": {
        "type": "object",
        "required": ["filePath", "lineNumber", "ruleId", "severity", "category", "message"],
        "properties": {
          "filePath": {
            "type": "string",
            "minLength": 1,
            "description": "Relative or absolute path to the source file"
          },
          "lineNumber": {
            "type": "integer",
            "minimum": 1,
            "description": "1-based line number where violation occurs"
          },
          "ruleId": {
            "type": "string",
            "pattern": "^LNT\\d{3}$",
            "description": "Unique rule identifier (e.g., LNT001)"
          },
          "severity": {
            "type": "string",
            "enum": ["error", "warning", "info"],
            "description": "Violation severity level"
          },
          "category": {
            "type": "string",
            "enum": [
              "Maintainability",
              "Performance",
              "Security",
              "Reliability",
              "Design",
              "Documentation",
              "Naming",
              "Testing"
            ],
            "description": "Rule category from Feature 019 taxonomy"
          },
          "message": {
            "type": "string",
            "minLength": 1,
            "description": "Human-readable diagnostic message"
          }
        },
        "additionalProperties": false
      }
    }
  },
  "additionalProperties": false
}
```

## Example Outputs

### Successful Analysis with Violations

```json
{
  "status": "success",
  "summary": {
    "total": 3,
    "bySeverity": {
      "error": 1,
      "warning": 2,
      "info": 0
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

### Empty Results (No Violations)

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

### Analysis Error

```json
{
  "status": "error",
  "summary": {
    "total": 0,
    "bySeverity": {}
  },
  "violations": [],
  "errorMessage": "Failed to load project: File not found"
}
```
**Note**: `errorMessage` field is OPTIONAL and only present when status is "error"

## jq Query Examples

### Count total violations
```bash
jq '.summary.total' results.json
# Output: 3
```

### Count errors only
```bash
jq '.summary.bySeverity.error' results.json
# Output: 1
```

### Filter violations by severity
```bash
jq '.violations[] | select(.severity == "error")' results.json
# Output: { "filePath": "src/Models/User.cs", ... }
```

### Group violations by file
```bash
jq 'group_by(.filePath) | map({file: .[0].filePath, count: length})' results.json
# Output: [{"file": "src/Services/UserService.cs", "count": 2}, ...]
```

### Filter by category
```bash
jq '.violations[] | select(.category == "Security")' results.json
```

### Extract file paths with errors
```bash
jq '[.violations[] | select(.severity == "error") | .filePath] | unique' results.json
# Output: ["src/Models/User.cs"]
```

## PowerShell Examples

### Parse JSON
```powershell
$results = Get-Content results.json | ConvertFrom-Json
```

### Count warnings
```powershell
$results.summary.bySeverity.warning
# Output: 2
```

### Filter by category
```powershell
$results.violations | Where-Object { $_.category -eq "Security" }
```

### Group by severity
```powershell
$results.violations | Group-Object severity | Select-Object Name, Count
# Output:
# Name    Count
# ----    -----
# warning     2
# error       1
```

### Get files with errors
```powershell
$results.violations | Where-Object { $_.severity -eq "error" } | Select-Object -Unique filePath
```

## Validation

### Using jq
```bash
# Validate JSON syntax
jq empty results.json && echo "Valid JSON"

# Check required fields
jq -e '.status and .summary and .violations' results.json
```

### Using PowerShell
```powershell
# Validate JSON syntax
try {
    $null = Get-Content results.json | ConvertFrom-Json
    Write-Host "Valid JSON"
} catch {
    Write-Error "Invalid JSON: $_"
}

# Check schema compliance
$json = Get-Content results.json | ConvertFrom-Json
if (-not $json.status -or -not $json.summary -or -not $json.violations) {
    throw "Missing required fields"
}
```

## CI/CD Integration Examples

### GitHub Actions
```yaml
- name: Run Lintelligent Analysis
  run: lintelligent scan --format json --output results.json
  
- name: Check for errors
  run: |
    ERROR_COUNT=$(jq '.summary.bySeverity.error // 0' results.json)
    if [ "$ERROR_COUNT" -gt 0 ]; then
      echo "❌ Found $ERROR_COUNT errors"
      exit 1
    fi
```

### Azure Pipelines
```yaml
- task: PowerShell@2
  displayName: 'Run Lintelligent Analysis'
  inputs:
    targetType: 'inline'
    script: |
      lintelligent scan --format json --output results.json
      $results = Get-Content results.json | ConvertFrom-Json
      if ($results.summary.bySeverity.error -gt 0) {
        Write-Error "Found $($results.summary.bySeverity.error) errors"
        exit 1
      }
```

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-12-25 | Initial schema definition for Feature 006 |
