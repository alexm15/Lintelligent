# JSON Output Format Research for Lintelligent

**Date:** January 2025  
**Purpose:** Inform JSON formatter design for Lintelligent static analysis CLI  
**Target Compatibility:** jq, PowerShell ConvertFrom-Json, CI/CD pipelines  
**Expected Scale:** 10,000+ violations with streaming/batching support

---

## 1. Industry Survey

### Popular Static Analysis Tools' JSON Formats

#### ESLint (JavaScript/TypeScript)

ESLint provides **two JSON formatters**: `json` and `json-with-metadata`.

**Basic JSON Format (`eslint -f json`):**
```json
[
  {
    "filePath": "/path/to/file.js",
    "messages": [
      {
        "ruleId": "semi",
        "severity": 2,
        "message": "Missing semicolon.",
        "line": 1,
        "column": 13,
        "nodeType": "VariableDeclaration",
        "messageId": "missingSemi",
        "endLine": 1,
        "endColumn": 14
      }
    ],
    "suppressedMessages": [],
    "errorCount": 1,
    "fatalErrorCount": 0,
    "warningCount": 0,
    "fixableErrorCount": 1,
    "fixableWarningCount": 0,
    "source": "var foo = \"bar\""
  }
]
```

**JSON with Metadata Format (`eslint -f json-with-metadata`):**
```json
{
  "results": [
    { /* same as basic format */ }
  ],
  "metadata": {
    "rulesMeta": {
      "semi": {
        "type": "layout",
        "docs": {
          "description": "require or disallow semicolons",
          "url": "https://eslint.org/docs/rules/semi"
        },
        "fixable": "code"
      }
    }
  }
}
```

**Key Design Decisions:**
- **camelCase naming** convention throughout
- **Numeric severity:** 1 = warning, 2 = error
- **Top-level array** for basic format (streaming-friendly)
- **Metadata wrapper** for enhanced format (includes rule documentation)
- **File-centric structure:** Each file gets one object with multiple messages
- **Count aggregates:** Pre-computed counts (errorCount, warningCount) for quick filtering
- **Source inclusion:** Original source code included for context (optional)

**Source:** https://eslint.org/docs/latest/use/formatters/

---

#### RuboCop (Ruby)

**RuboCop JSON Format (`rubocop --format json`):**
```json
{
  "metadata": {
    "rubocop_version": "1.50.0",
    "ruby_engine": "ruby",
    "ruby_version": "3.2.0",
    "ruby_patchlevel": "0",
    "ruby_platform": "x86_64-darwin22"
  },
  "files": [
    {
      "path": "lib/example.rb",
      "offenses": [
        {
          "severity": "warning",
          "message": "Prefer single-quoted strings when you don't need interpolation.",
          "cop_name": "Style/StringLiterals",
          "corrected": false,
          "correctable": true,
          "location": {
            "start_line": 5,
            "start_column": 10,
            "last_line": 5,
            "last_column": 25,
            "length": 16,
            "line": 5,
            "column": 10
          }
        }
      ]
    }
  ],
  "summary": {
    "offense_count": 1,
    "target_file_count": 1,
    "inspected_file_count": 1
  }
}
```

**Key Design Decisions:**
- **snake_case naming** convention throughout
- **String severity:** "convention", "warning", "error", "fatal"
- **Metadata object:** Tool version and runtime environment at top level
- **File array:** Similar to ESLint's file-centric approach
- **Correctable flag:** Indicates if auto-fix is available
- **Rich location:** Both start/end coordinates with length calculation
- **Summary object:** Overall statistics at bottom

**Source:** RuboCop documentation and source code analysis

---

#### golangci-lint (Go)

**golangci-lint JSON Format (`golangci-lint run --out-format json`):**
```json
{
  "Issues": [
    {
      "FromLinter": "errcheck",
      "Text": "Error return value is not checked",
      "Severity": "error",
      "SourceLines": [
        "    result, err := doSomething()",
        "    fmt.Println(result)"
      ],
      "Replacement": null,
      "Pos": {
        "Filename": "main.go",
        "Offset": 0,
        "Line": 15,
        "Column": 2
      },
      "ExpectNoLint": false,
      "ExpectedNoLintLinter": ""
    }
  ],
  "Report": {
    "Warnings": [],
    "Linters": [
      {
        "Name": "errcheck",
        "Enabled": true,
        "EnabledByDefault": true
      }
    ]
  }
}
```

**Key Design Decisions:**
- **PascalCase for top-level keys**, camelCase for nested properties
- **String severity:** "error", "warning"
- **Flat issue array:** Not grouped by file
- **Source context:** Includes surrounding source lines
- **Position object:** Separate Pos object with file, line, column, offset
- **Linter metadata:** Report section with linter configuration
- **Replacement field:** For auto-fix suggestions (null if not applicable)

**Source:** golangci-lint repository analysis

---

#### Clippy (Rust)

**Clippy JSON Format (`cargo clippy --message-format json`):**
```json
{
  "reason": "compiler-message",
  "package_id": "my-crate 0.1.0 (path+file:///path/to/crate)",
  "manifest_path": "/path/to/Cargo.toml",
  "target": {
    "kind": ["bin"],
    "crate_types": ["bin"],
    "name": "my-crate",
    "src_path": "/path/to/main.rs"
  },
  "message": {
    "message": "unused variable: `x`",
    "code": {
      "code": "unused_variables",
      "explanation": null
    },
    "level": "warning",
    "spans": [
      {
        "file_name": "src/main.rs",
        "byte_start": 45,
        "byte_end": 46,
        "line_start": 3,
        "line_end": 3,
        "column_start": 9,
        "column_end": 10,
        "is_primary": true,
        "text": [
          {
            "text": "    let x = 5;",
            "highlight_start": 9,
            "highlight_end": 10
          }
        ],
        "label": null,
        "suggested_replacement": null,
        "suggestion_applicability": null,
        "expansion": null
      }
    ],
    "children": [],
    "rendered": null
  }
}
```

**Key Design Decisions:**
- **snake_case naming** throughout
- **NDJSON format:** One JSON object per line (streaming-friendly)
- **Reason field:** Discriminator for different message types
- **String severity:** "error", "warning", "note", "help"
- **Byte-level precision:** Includes byte_start/byte_end for editors
- **Text spans:** Rich span information with highlighted text excerpts
- **Package context:** Includes package ID and manifest path
- **Rendered field:** Optional pre-formatted terminal output

**Source:** Rust Compiler message format documentation

---

### Common Patterns Across Tools

| Aspect | ESLint | RuboCop | golangci-lint | Clippy |
|--------|--------|---------|---------------|--------|
| **Naming Convention** | camelCase | snake_case | Mixed (PascalCase top) | snake_case |
| **Severity Format** | Numeric (1, 2) | String | String | String |
| **Structure** | Array or Object | Object | Object | NDJSON stream |
| **File Grouping** | Yes | Yes | No (flat) | No (flat) |
| **Metadata** | Optional wrapper | Top-level object | Report object | Package context |
| **Source Context** | Optional source field | Not included | SourceLines array | Text spans |
| **Location Format** | line, column, endLine, endColumn | start_line, start_column, last_line, last_column | Pos object | Spans array |
| **Rule Metadata** | Separate rulesMeta | In offenses | In linter info | Code object |

---

## 2. Common Schema Patterns

### Two Primary Approaches

#### Approach A: File-Centric (ESLint, RuboCop)
```json
{
  "files": [
    {
      "path": "file1.cs",
      "violations": [ /* array */ ]
    }
  ]
}
```
**Pros:**
- Natural grouping by file
- Easy to count violations per file
- Mirrors how humans think about code

**Cons:**
- Requires buffering all violations per file before output
- Harder to stream in real-time
- More complex to query across all files with jq

#### Approach B: Flat Issue Array (golangci-lint, Clippy)
```json
{
  "issues": [
    {
      "file": "file1.cs",
      "line": 10,
      /* ... */
    }
  ]
}
```
**Pros:**
- Streaming-friendly (can emit issues as discovered)
- Simple jq queries: `jq '.issues[] | select(.severity == "error")'`
- Easy to append/concatenate

**Cons:**
- File path repeated for every violation (larger size)
- Requires post-processing to group by file

#### Approach C: NDJSON/JSON Lines (Clippy, log files)
```json
{"file": "file1.cs", "line": 10, "severity": "error"}
{"file": "file2.cs", "line": 25, "severity": "warning"}
```
**Pros:**
- **Optimal for streaming:** Emit one line immediately after each violation
- **Unix-friendly:** Works with grep, sed, awk, head, tail
- **Resilient:** Partial file corruption doesn't invalidate entire dataset
- **Append-safe:** Multiple processes can append to same file
- **Low memory:** No need to hold entire structure in memory

**Cons:**
- Not a single valid JSON document (array of objects, not JSON array)
- Some JSON parsers require special handling
- PowerShell `ConvertFrom-Json` requires reading all lines then parsing each

---

### Top-Level Schema Recommendations

**Minimal Schema (Streaming-optimized):**
```json
{
  "version": "1.0.0",
  "tool": {
    "name": "Lintelligent",
    "version": "1.2.3"
  },
  "results": []
}
```

**Full Schema (Metadata-rich):**
```json
{
  "$schema": "https://example.com/lintelligent-output-v1.schema.json",
  "version": "1.0.0",
  "tool": {
    "name": "Lintelligent",
    "version": "1.2.3",
    "informationUri": "https://github.com/yourorg/lintelligent"
  },
  "invocation": {
    "executionSuccessful": true,
    "startTimeUtc": "2025-01-15T10:30:00Z",
    "endTimeUtc": "2025-01-15T10:30:15Z",
    "workingDirectory": "/path/to/project",
    "commandLine": ["lintelligent", "scan", "--format", "json"]
  },
  "results": [],
  "summary": {
    "totalFiles": 100,
    "filesWithIssues": 25,
    "totalIssues": 347,
    "errorCount": 12,
    "warningCount": 335,
    "infoCount": 0
  },
  "rules": [
    {
      "id": "LI001",
      "name": "LongMethodRule",
      "shortDescription": "Method exceeds maximum line count",
      "helpUri": "https://docs.example.com/rules/LI001"
    }
  ]
}
```

**Recommended for Lintelligent:** Start with **Approach B (flat array)** for v1.0, with option to add NDJSON mode later.

---

## 3. Field Naming Conventions

### Analysis of Popular Tools

| Tool | Convention | Example |
|------|-----------|---------|
| **ESLint** | camelCase | `filePath`, `lineNumber`, `ruleId`, `errorCount` |
| **RuboCop** | snake_case | `file_path`, `line_number`, `cop_name`, `offense_count` |
| **golangci-lint** | PascalCase (top), camelCase (nested) | `Issues`, `FromLinter`, `SourceLines` |
| **Clippy** | snake_case | `file_name`, `line_start`, `byte_end` |
| **SARIF 2.1** | camelCase | `ruleId`, `startLine`, `artifactLocation` |

### Rationale by Language Ecosystem

- **JavaScript/TypeScript tools** → camelCase (idiomatic for JS)
- **Ruby tools** → snake_case (idiomatic for Ruby)
- **Go tools** → PascalCase or camelCase (Go's public/private convention)
- **Rust tools** → snake_case (idiomatic for Rust)
- **.NET/C# tools** → Typically camelCase for JSON (see SARIF, ASP.NET Core defaults)

### Recommendation for Lintelligent

**Use camelCase** for all JSON property names:

**Rationale:**
1. **C# Ecosystem Standard:** Modern .NET JSON serialization (System.Text.Json, Newtonsoft.Json) defaults to camelCase
2. **SARIF Alignment:** SARIF 2.1.0 uses camelCase; if Lintelligent later supports SARIF, consistency helps
3. **JavaScript Ecosystem:** jq users and web-based CI dashboards expect camelCase
4. **Readability:** Slightly more compact than snake_case without underscores

**Example Properties:**
```json
{
  "filePath": "src/Program.cs",
  "lineNumber": 42,
  "columnNumber": 15,
  "ruleId": "LI001",
  "severity": "warning",
  "message": "Method exceeds 50 lines",
  "category": "Maintainability"
}
```

---

## 4. Severity Representation

### String vs. Numeric Approaches

#### String Severity (RuboCop, golangci-lint, Clippy, SARIF)

**Values:** `"error"`, `"warning"`, `"info"` (or `"note"`)

**Pros:**
- Self-documenting: `"error"` is clearer than `2`
- Extensible: Easy to add `"critical"` or `"suggestion"` without breaking numeric order
- Human-readable in jq output: `.[] | select(.severity == "error")`
- Case-insensitive matching: `"Error"`, `"error"`, `"ERROR"` can be normalized

**Cons:**
- Slightly more verbose (6-7 bytes vs 1 byte)
- String comparison slightly slower than integer (negligible for most use cases)

#### Numeric Severity (ESLint)

**Values:** `0` (off), `1` (warning), `2` (error)

**Pros:**
- Compact representation
- Sortable: `severity >= 2` for errors
- Fast integer comparison

**Cons:**
- Magic numbers: Requires documentation or constants
- Less discoverable: Users must look up what `2` means
- Hard to extend: What if you want 5 severity levels?

### Recommendation for Lintelligent

**Use string severity** with these exact values:
- `"error"` — Critical issues that must be fixed
- `"warning"` — Important issues that should be reviewed
- `"info"` — Informational findings or suggestions

**Rationale:**
1. **Industry Standard:** SARIF, RuboCop, golangci-lint, Clippy all use strings
2. **Self-Documenting:** No need to reference documentation
3. **Future-Proof:** Easy to add `"suggestion"` or `"note"` later
4. **CI/CD Friendly:** Pipeline scripts can filter with string comparisons: `jq '.results[] | select(.severity == "error")'`

**Example:**
```json
{
  "severity": "warning",
  "message": "Method 'Calculate' exceeds 50 lines (actual: 78)"
}
```

**Lowercase vs. Title Case:**
- **Recommendation:** Use **lowercase** (`"error"`, `"warning"`, `"info"`)
- **Rationale:** Matches SARIF spec, easier to type, consistent with most tools

---

## 5. Performance Considerations (Streaming vs. Buffering)

### Scenario: 10,000+ Violations

When analyzing large codebases, performance becomes critical. Consider a scan that finds 10,000 violations across 500 files.

#### Buffering Approach (Collect All, Then Output)

**How It Works:**
```csharp
var results = new List<DiagnosticResult>();
foreach (var file in files)
{
    results.AddRange(AnalyzeFile(file));
}
var json = JsonSerializer.Serialize(new { results });
Console.WriteLine(json);
```

**Pros:**
- Simple implementation
- Can compute summary statistics (totalIssues, errorCount) before output
- Output is valid single JSON document

**Cons:**
- **High memory usage:** All 10,000 results held in memory
- **No progress feedback:** User sees nothing until the end
- **Wasted work if canceled:** If user cancels after 5 minutes, no output
- **Large allocations:** May trigger GC pauses

**When to Use:**
- Small projects (<1000 violations)
- Summary statistics required upfront
- Output format requires entire structure (e.g., file-grouped format)

---

#### Streaming Approach (Emit as Discovered)

**How It Works:**
```csharp
var writer = new Utf8JsonWriter(Console.OpenStandardOutput());
writer.WriteStartObject();
writer.WriteString("version", "1.0.0");
writer.WriteStartArray("results");

foreach (var file in files)
{
    foreach (var result in AnalyzeFile(file))
    {
        JsonSerializer.Serialize(writer, result);
        writer.Flush(); // Emit immediately
    }
}

writer.WriteEndArray();
writer.WriteEndObject();
```

**Pros:**
- **Low memory:** Only current violation in memory
- **Progress visible:** Output appears immediately (can be piped to `head`, `grep`)
- **Resilient:** Partial results preserved if canceled
- **Scalable:** Works with 10,000 or 1,000,000 violations

**Cons:**
- **No upfront summary:** Can't compute totalIssues until end
- **More complex code:** Manual JSON writing
- **Harder to unit test:** Output is incremental

**When to Use:**
- Large projects (1000+ violations)
- Long-running scans (>10 seconds)
- CI/CD pipelines that process output incrementally

---

#### NDJSON Streaming (Line-by-Line)

**How It Works:**
```csharp
foreach (var file in files)
{
    foreach (var result in AnalyzeFile(file))
    {
        var json = JsonSerializer.Serialize(result);
        Console.WriteLine(json); // One JSON object per line
    }
}
```

**Pros:**
- **Simplest streaming:** Just serialize and print
- **Optimal memory:** Constant memory regardless of result count
- **Unix-friendly:** Works with `grep`, `awk`, `head -n 100`
- **Append-safe:** Multiple runs can append to same file
- **Resilient:** Partial output is valid (each line is valid JSON)

**Cons:**
- **Not a JSON array:** Requires special parsing (read line-by-line)
- **No summary upfront:** Must post-process for statistics
- **PowerShell compatibility:** Requires `Get-Content | ForEach { ConvertFrom-Json }`

**When to Use:**
- Very large projects (10,000+ violations)
- Log-like output (continuous monitoring)
- Unix pipeline integration

**Example Output:**
```json
{"filePath":"Program.cs","lineNumber":10,"severity":"error","message":"..."}
{"filePath":"Util.cs","lineNumber":25,"severity":"warning","message":"..."}
```

**Processing with jq:**
```bash
lintelligent scan --format jsonlines | jq 'select(.severity == "error")'
```

---

### Recommendation for Lintelligent

**Phase 1 (MVP):** Implement **buffering** with single JSON document.  
**Phase 2 (Scaling):** Add **NDJSON** mode as `--format jsonlines` option.

**Implementation Strategy:**
1. Default JSON format: Buffered array for simplicity
2. CLI flag `--format jsonlines`: Enables streaming output
3. Document both formats clearly
4. Provide jq examples for common queries

---

## 6. Edge Case Handling

### Empty Results

**Scenario:** No violations found in the scanned files.

**Bad Approach (null):**
```json
{
  "results": null
}
```
**Why Bad:** Forces consumers to null-check; inconsistent type

**Good Approach (empty array):**
```json
{
  "results": []
}
```
**Why Good:** Consistent type; `jq '.results | length'` returns 0

**Recommendation:** Always use **empty array `[]`** for zero results.

---

### Special Characters in Strings

**Scenario:** Violation message contains newlines, quotes, Unicode, or control characters.

**Example Message:**
```
Method "Calculate" contains invalid character: 	 (tab)
Suggestion: Use spaces instead.
```

**JSON Escaping:**
```json
{
  "message": "Method \"Calculate\" contains invalid character: \t (tab)\nSuggestion: Use spaces instead."
}
```

**Unicode Example:**
```json
{
  "message": "Variable name '变量' contains non-ASCII characters"
}
```

**Recommendation:**
- **Use proper JSON serializer:** `System.Text.Json` or `Newtonsoft.Json` handles escaping automatically
- **UTF-8 encoding:** Always output UTF-8 (no BOM)
- **Avoid manual string concatenation:** Use serializer to prevent escaping bugs

---

### Null vs. Omitted Fields

**Scenario:** Optional field (e.g., `endLine`) is not applicable.

**Option 1: Include as null**
```json
{
  "lineNumber": 10,
  "endLine": null
}
```

**Option 2: Omit field**
```json
{
  "lineNumber": 10
}
```

**Recommendation:** **Omit optional fields** if null/not applicable.  
**Rationale:**
- Smaller output size
- jq handles missing fields gracefully: `.endLine // .lineNumber`
- JSON schema can mark fields as optional

**Exception:** If field is semantically important (e.g., `severity` should always exist), include it even if empty string.

---

### File Paths (Absolute vs. Relative)

**Scenario:** Violation in `/home/user/project/src/Util.cs`

**Absolute Path:**
```json
{
  "filePath": "/home/user/project/src/Util.cs"
}
```
**Pros:** Unambiguous, works from any working directory  
**Cons:** Output not portable across machines, reveals full paths

**Relative Path (from working directory):**
```json
{
  "filePath": "src/Util.cs"
}
```
**Pros:** Portable, concise, matches `git diff` output  
**Cons:** Requires consumer to know working directory

**Recommendation:**
- **Default to relative paths** (from working directory or solution root)
- **Provide `--absolute-paths` CLI flag** for absolute paths if needed
- **Normalize path separators:** Use forward slash `/` (Unix convention, works on Windows too)

---

### Large Numbers

**Scenario:** Line number exceeds 32-bit integer (rare but possible with generated code).

**Recommendation:**
- Use `int` (32-bit) for line numbers in C#
- JSON spec supports arbitrary-precision integers
- Most parsers handle large integers correctly

**Caveat:** JavaScript (jq's runtime) uses 64-bit floats, losing precision above 2^53. This is unlikely for line numbers (<2 billion lines).

---

### Concurrent Output

**Scenario:** Multiple analyzer threads emit results simultaneously.

**Problem:** Interleaved JSON output corrupts structure:
```json
{"filePath":"A.cs"{"filePath":"B.cs"},"line":10}
```

**Solutions:**
1. **Lock stdout writes:** Serialize to string, then lock and write atomically
2. **Buffering:** Collect per-thread results, merge before output
3. **NDJSON:** Each line is atomic (less critical if lines interleave)

**Recommendation:** Use **buffering** or **lock stdout** for structured JSON. NDJSON is more forgiving.

---

## 7. CLI Tool Compatibility

### jq (JSON Query Tool)

**jq** is the de facto standard for querying JSON in shell scripts.

#### Example Queries

**Filter by severity:**
```bash
jq '.results[] | select(.severity == "error")' output.json
```

**Count errors:**
```bash
jq '[.results[] | select(.severity == "error")] | length' output.json
```

**Group by file:**
```bash
jq 'group_by(.filePath) | map({file: .[0].filePath, count: length})' output.json
```

**Extract specific fields:**
```bash
jq '.results[] | {file: .filePath, line: .lineNumber, msg: .message}' output.json
```

**Convert to CSV:**
```bash
jq -r '.results[] | [.filePath, .lineNumber, .severity, .message] | @csv' output.json
```

#### Compatibility Requirements

1. **Valid JSON:** Must parse without errors (`jq .` should work)
2. **Consistent types:** Don't mix `"10"` (string) and `10` (number) for same field
3. **No trailing commas:** JSON spec forbids them (unlike JavaScript)
4. **UTF-8 encoding:** jq expects UTF-8 by default
5. **No BOM:** Byte Order Mark (U+FEFF) breaks parsing

#### Testing Recommendations

**Automated jq tests:**
```bash
# Test 1: Valid JSON
jq . output.json > /dev/null || echo "FAIL: Invalid JSON"

# Test 2: Results is array
jq -e '.results | type == "array"' output.json || echo "FAIL: results not array"

# Test 3: Severity values
jq -e '.results[] | select(.severity | IN("error", "warning", "info") | not)' output.json && echo "FAIL: Invalid severity"
```

---

### PowerShell ConvertFrom-Json

**PowerShell's `ConvertFrom-Json`** is used in Windows CI/CD pipelines (Azure DevOps, GitHub Actions on Windows).

#### Example Queries

**Filter by severity:**
```powershell
$json = Get-Content output.json | ConvertFrom-Json
$errors = $json.results | Where-Object { $_.severity -eq "error" }
```

**Count warnings:**
```powershell
($json.results | Where-Object { $_.severity -eq "warning" }).Count
```

**Group by file:**
```powershell
$json.results | Group-Object filePath | Select-Object Name, Count
```

**Export to CSV:**
```powershell
$json.results | Select-Object filePath, lineNumber, severity, message | Export-Csv -Path output.csv
```

#### Compatibility Requirements

1. **Valid JSON:** Must be parsable as JSON
2. **Property access:** Use camelCase for easy property access: `$_.filePath` (not `$_.'file-path'`)
3. **Type consistency:** PowerShell is dynamically typed, but type changes can cause errors
4. **Large files:** ConvertFrom-Json loads entire JSON into memory (use `-Raw` for very large files)

#### NDJSON in PowerShell

**Processing line-by-line:**
```powershell
Get-Content output.jsonlines | ForEach-Object {
    $result = $_ | ConvertFrom-Json
    if ($result.severity -eq "error") {
        Write-Output $result
    }
}
```

#### Testing Recommendations

**Automated PowerShell tests:**
```powershell
# Test 1: Valid JSON
try {
    $json = Get-Content output.json | ConvertFrom-Json
} catch {
    Write-Error "FAIL: Invalid JSON"
}

# Test 2: Results is array
if ($json.results -isnot [Array]) {
    Write-Error "FAIL: results is not array"
}

# Test 3: Properties exist
$json.results | ForEach-Object {
    if (-not $_.filePath -or -not $_.lineNumber) {
        Write-Error "FAIL: Missing required properties"
    }
}
```

---

### CI/CD Integration Examples

#### GitHub Actions (Linux)
```yaml
- name: Run Lintelligent
  run: |
    lintelligent scan --format json > results.json
    
- name: Check for errors
  run: |
    ERROR_COUNT=$(jq '[.results[] | select(.severity == "error")] | length' results.json)
    if [ "$ERROR_COUNT" -gt 0 ]; then
      echo "Found $ERROR_COUNT errors"
      exit 1
    fi
```

#### Azure Pipelines (Windows)
```yaml
- powershell: |
    lintelligent scan --format json | Out-File results.json
    $json = Get-Content results.json | ConvertFrom-Json
    $errors = $json.results | Where-Object { $_.severity -eq "error" }
    if ($errors.Count -gt 0) {
      Write-Error "Found $($errors.Count) errors"
      exit 1
    }
```

---

## 8. Recommended Schema for Lintelligent

### Design Principles

1. **Simple by default, rich when needed**
2. **Streaming-friendly** (flat array structure)
3. **jq and PowerShell compatible**
4. **Extensible** (easy to add fields later)
5. **SARIF-inspired** (potential future migration path)

---

### Schema Definition

#### Top-Level Object

```json
{
  "$schema": "https://lintelligent.io/schemas/output-v1.0.json",
  "version": "1.0.0",
  "tool": {
    "name": "Lintelligent",
    "version": "1.2.3",
    "informationUri": "https://github.com/yourorg/lintelligent"
  },
  "invocation": {
    "executionSuccessful": true,
    "startTimeUtc": "2025-01-15T10:30:00Z",
    "endTimeUtc": "2025-01-15T10:30:15Z",
    "workingDirectory": "/path/to/project",
    "commandLine": ["lintelligent", "scan", "--format", "json"]
  },
  "results": [
    { /* result object */ }
  ],
  "summary": {
    "totalFiles": 100,
    "filesWithIssues": 25,
    "totalIssues": 347,
    "errorCount": 12,
    "warningCount": 335,
    "infoCount": 0
  }
}
```

**Field Descriptions:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `$schema` | string | No | JSON Schema URL for validation |
| `version` | string | **Yes** | Schema version (semver) |
| `tool` | object | **Yes** | Tool metadata |
| `tool.name` | string | **Yes** | Tool name ("Lintelligent") |
| `tool.version` | string | **Yes** | Tool version (semver) |
| `tool.informationUri` | string | No | URL to tool documentation |
| `invocation` | object | No | Execution context |
| `invocation.executionSuccessful` | boolean | No | True if scan completed without errors |
| `invocation.startTimeUtc` | string | No | ISO 8601 timestamp (UTC) |
| `invocation.endTimeUtc` | string | No | ISO 8601 timestamp (UTC) |
| `invocation.workingDirectory` | string | No | Working directory path |
| `invocation.commandLine` | array | No | Command-line arguments |
| `results` | array | **Yes** | Array of result objects |
| `summary` | object | No | Summary statistics |
| `summary.totalFiles` | integer | No | Total files scanned |
| `summary.filesWithIssues` | integer | No | Files with at least one issue |
| `summary.totalIssues` | integer | No | Total issues found |
| `summary.errorCount` | integer | No | Count of error-severity issues |
| `summary.warningCount` | integer | No | Count of warning-severity issues |
| `summary.infoCount` | integer | No | Count of info-severity issues |

---

#### Result Object

```json
{
  "ruleId": "LI001",
  "ruleName": "LongMethodRule",
  "severity": "warning",
  "message": "Method 'Calculate' exceeds maximum line count of 50 (actual: 78 lines)",
  "category": "Maintainability",
  "filePath": "src/Utils/Calculator.cs",
  "startLine": 10,
  "startColumn": 5,
  "endLine": 88,
  "endColumn": 6,
  "helpUri": "https://docs.lintelligent.io/rules/LI001"
}
```

**Field Descriptions:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `ruleId` | string | **Yes** | Unique rule identifier (e.g., "LI001") |
| `ruleName` | string | No | Human-readable rule name |
| `severity` | string | **Yes** | `"error"`, `"warning"`, or `"info"` |
| `message` | string | **Yes** | Diagnostic message |
| `category` | string | No | Rule category (e.g., "Performance", "Maintainability") |
| `filePath` | string | **Yes** | Relative file path (forward slashes) |
| `startLine` | integer | **Yes** | Start line number (1-based) |
| `startColumn` | integer | No | Start column number (1-based) |
| `endLine` | integer | No | End line number (1-based) |
| `endColumn` | integer | No | End column number (1-based) |
| `helpUri` | string | No | URL to rule documentation |

---

### Example Output

**Small Project (3 issues):**
```json
{
  "version": "1.0.0",
  "tool": {
    "name": "Lintelligent",
    "version": "1.2.3"
  },
  "results": [
    {
      "ruleId": "LI001",
      "ruleName": "LongMethodRule",
      "severity": "warning",
      "message": "Method 'Calculate' exceeds maximum line count of 50 (actual: 78 lines)",
      "category": "Maintainability",
      "filePath": "src/Calculator.cs",
      "startLine": 10,
      "endLine": 88,
      "helpUri": "https://docs.lintelligent.io/rules/LI001"
    },
    {
      "ruleId": "LI002",
      "ruleName": "ComplexConditionRule",
      "severity": "error",
      "message": "Cyclomatic complexity of method 'Process' is 15 (threshold: 10)",
      "category": "Maintainability",
      "filePath": "src/Processor.cs",
      "startLine": 45,
      "helpUri": "https://docs.lintelligent.io/rules/LI002"
    },
    {
      "ruleId": "LI003",
      "ruleName": "MagicNumberRule",
      "severity": "info",
      "message": "Consider extracting magic number '42' to a named constant",
      "category": "Readability",
      "filePath": "src/Config.cs",
      "startLine": 12,
      "startColumn": 20,
      "endColumn": 22
    }
  ],
  "summary": {
    "totalFiles": 3,
    "filesWithIssues": 3,
    "totalIssues": 3,
    "errorCount": 1,
    "warningCount": 1,
    "infoCount": 1
  }
}
```

---

### NDJSON Variant (Optional)

**Enable with `--format jsonlines`:**

```json
{"ruleId":"LI001","severity":"warning","message":"Method 'Calculate' exceeds 50 lines","filePath":"src/Calculator.cs","startLine":10}
{"ruleId":"LI002","severity":"error","message":"Cyclomatic complexity is 15","filePath":"src/Processor.cs","startLine":45}
{"ruleId":"LI003","severity":"info","message":"Magic number '42'","filePath":"src/Config.cs","startLine":12}
```

**Processing:**
```bash
# Filter errors
lintelligent scan --format jsonlines | jq 'select(.severity == "error")'

# Count by severity
lintelligent scan --format jsonlines | jq -s 'group_by(.severity) | map({severity: .[0].severity, count: length})'
```

---

### JSON Schema (for Validation)

**File: `lintelligent-output-v1.0.schema.json`**

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Lintelligent JSON Output Schema",
  "version": "1.0.0",
  "type": "object",
  "required": ["version", "tool", "results"],
  "properties": {
    "$schema": {
      "type": "string",
      "format": "uri"
    },
    "version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+$"
    },
    "tool": {
      "type": "object",
      "required": ["name", "version"],
      "properties": {
        "name": {"type": "string"},
        "version": {"type": "string"},
        "informationUri": {"type": "string", "format": "uri"}
      }
    },
    "invocation": {
      "type": "object",
      "properties": {
        "executionSuccessful": {"type": "boolean"},
        "startTimeUtc": {"type": "string", "format": "date-time"},
        "endTimeUtc": {"type": "string", "format": "date-time"},
        "workingDirectory": {"type": "string"},
        "commandLine": {"type": "array", "items": {"type": "string"}}
      }
    },
    "results": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["ruleId", "severity", "message", "filePath", "startLine"],
        "properties": {
          "ruleId": {"type": "string"},
          "ruleName": {"type": "string"},
          "severity": {"type": "string", "enum": ["error", "warning", "info"]},
          "message": {"type": "string"},
          "category": {"type": "string"},
          "filePath": {"type": "string"},
          "startLine": {"type": "integer", "minimum": 1},
          "startColumn": {"type": "integer", "minimum": 1},
          "endLine": {"type": "integer", "minimum": 1},
          "endColumn": {"type": "integer", "minimum": 1},
          "helpUri": {"type": "string", "format": "uri"}
        }
      }
    },
    "summary": {
      "type": "object",
      "properties": {
        "totalFiles": {"type": "integer", "minimum": 0},
        "filesWithIssues": {"type": "integer", "minimum": 0},
        "totalIssues": {"type": "integer", "minimum": 0},
        "errorCount": {"type": "integer", "minimum": 0},
        "warningCount": {"type": "integer", "minimum": 0},
        "infoCount": {"type": "integer", "minimum": 0}
      }
    }
  }
}
```

---

## Summary & Next Steps

### Key Decisions

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Naming Convention** | camelCase | .NET ecosystem standard, SARIF alignment, jq/JS friendly |
| **Severity Format** | String (`"error"`, `"warning"`, `"info"`) | Self-documenting, extensible, industry standard |
| **Structure** | Flat array (results[]) | Streaming-friendly, simple jq queries |
| **Streaming** | Buffered JSON (MVP), NDJSON later | Start simple, scale when needed |
| **File Paths** | Relative (default), `--absolute-paths` flag | Portable output, matches Git conventions |
| **Empty Results** | Empty array `[]` | Type consistency, jq-friendly |
| **Null Fields** | Omit if optional | Smaller output, clean jq queries |

### Implementation Phases

**Phase 1: MVP (Week 1-2)**
- Implement buffered JSON output
- Core schema: version, tool, results, summary
- Required fields only: ruleId, severity, message, filePath, startLine
- jq and PowerShell compatibility tests

**Phase 2: Enhancements (Week 3-4)**
- Add optional fields: category, helpUri, column numbers
- Implement `--format jsonlines` for NDJSON streaming
- Add JSON Schema for validation
- CI/CD integration examples

**Phase 3: Advanced (Future)**
- SARIF 2.1.0 output format (`--format sarif`)
- Incremental streaming for large projects
- Compressed output (`--compress` → gzip)
- Schema versioning and migration

### Testing Checklist

- [ ] Valid JSON (parse with `jq .`)
- [ ] jq filter queries work (`.results[] | select(.severity == "error")`)
- [ ] PowerShell ConvertFrom-Json works
- [ ] UTF-8 encoding (no BOM)
- [ ] Special characters escaped correctly
- [ ] Empty results output (`results: []`)
- [ ] Large result sets (10,000+ violations)
- [ ] NDJSON format (one JSON per line)
- [ ] JSON Schema validation passes

---

**End of Research Document**
