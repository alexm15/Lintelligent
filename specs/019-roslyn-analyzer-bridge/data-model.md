# Data Model: Roslyn Analyzer Bridge

**Feature**: 019-roslyn-analyzer-bridge  
**Date**: December 24, 2025  
**Purpose**: Define entities, relationships, and state management for Roslyn analyzer integration

## Entities

### 1. LintelligentDiagnosticAnalyzer

**Purpose**: Main entry point for Roslyn analyzer host, manages rule discovery and analysis execution

**State**: Stateless (all data immutable, computed at initialization)

**Fields**:
```csharp
private static readonly IAnalyzerRule[] _rules;                    // Discovered at static init
private static readonly DiagnosticDescriptor[] _descriptors;      // One per rule
private static readonly Dictionary<string, DiagnosticDescriptor> _descriptorMap;  // RuleId → Descriptor
```

**Lifecycle**:
- **Initialize** (static constructor): Discover IAnalyzerRule types via reflection, create descriptors
- **Register** (Initialize method): Called by Roslyn host, registers SyntaxTreeAction callback
- **Analyze** (per compilation): Execute all rules on each syntax tree, report diagnostics
- **Dispose**: N/A (stateless, no unmanaged resources)

**Relationships**:
- **Has-Many**: IAnalyzerRule implementations (discovered via reflection)
- **Creates**: DiagnosticDescriptor for each rule (1:1 mapping)
- **Receives**: SyntaxTreeAnalysisContext from Roslyn host
- **Reports**: Diagnostic to Roslyn via context.ReportDiagnostic()

**Validation Rules**:
- Must discover at least 1 IAnalyzerRule (fail fast if none found)
- Each rule must have unique ID (duplicate IDs cause analyzer load failure)
- All rules must be constructible with parameterless constructor (or fail gracefully)

---

### 2. DiagnosticDescriptor

**Purpose**: Roslyn metadata for each rule (ID, severity, help link, category)

**State**: Immutable (created once per rule at analyzer initialization)

**Fields**:
```csharp
string Id;                      // LNT001-LNT008
string Title;                   // rule.Description
string MessageFormat;           // "{0}" (placeholder for dynamic message)
string Category;                // rule.Category (e.g., "Maintainability")
DiagnosticSeverity DefaultSeverity;  // Mapped from rule.Severity
bool IsEnabledByDefault;        // true (all rules enabled unless EditorConfig suppresses)
string Description;             // Same as Title
string HelpLinkUri;             // GitHub docs URL with anchor
string[] CustomTags;            // ["CodeQuality", category-specific tag]
```

**Lifecycle**:
- **Create** (static initialization): RuleDescriptorFactory.Create(IAnalyzerRule)
- **Use** (per diagnostic): Passed to Diagnostic.Create() with location
- **Dispose**: N/A (no cleanup needed)

**Relationships**:
- **Created-By**: RuleDescriptorFactory
- **Created-From**: IAnalyzerRule (1:1 mapping)
- **Used-By**: DiagnosticConverter when creating Roslyn Diagnostic

**Validation Rules**:
- Id must match rule.Id exactly (case-sensitive)
- HelpLinkUri must be valid URL (or null)
- Category must be non-empty (fallback to "CodeQuality")

---

### 3. DiagnosticResult (Existing - No Changes)

**Purpose**: Immutable record of single analysis finding from IAnalyzerRule

**State**: Immutable (defined in Lintelligent.AnalyzerEngine)

**Fields**:
```csharp
string FilePath;     // Absolute path to analyzed file
string RuleId;       // LNT001-LNT008
string Message;      // Human-readable description of violation
int LineNumber;      // 1-indexed line number
Severity Severity;   // Error/Warning/Info
string Category;     // Maintainability/CodeSmell/Documentation
```

**Lifecycle**:
- **Create**: IAnalyzerRule.Analyze() returns IEnumerable<DiagnosticResult>
- **Transform**: DiagnosticConverter.Convert(DiagnosticResult, SyntaxTree) → Roslyn Diagnostic
- **Dispose**: N/A (immutable record, GC handles cleanup)

**Relationships**:
- **Produced-By**: IAnalyzerRule implementations
- **Consumed-By**: DiagnosticConverter
- **Converted-To**: Roslyn Diagnostic

**Validation Rules**:
- LineNumber ≥ 1 (enforced by DiagnosticResult constructor)
- FilePath must be non-empty (enforced by constructor)
- RuleId must match pattern LNT\d{3} (convention, not enforced)

---

### 4. Roslyn Location

**Purpose**: Represents position in source code (file, line, column)

**State**: Immutable (created by Roslyn API)

**Fields** (conceptual - actual Roslyn type):
```csharp
string FilePath;               // From SyntaxTree
TextSpan Span;                 // Start/end position in file
LinePositionSpan LineSpan;     // 0-indexed line/column
```

**Lifecycle**:
- **Create**: Location.Create(SyntaxTree, TextSpan)
- **Use**: Passed to Diagnostic.Create() to pinpoint violation
- **Dispose**: N/A (managed by Roslyn)

**Relationships**:
- **Created-From**: SyntaxTree + DiagnosticResult.LineNumber
- **Used-By**: Roslyn Diagnostic
- **Displayed-In**: IDE Error List, build output

**Transformation**:
```
DiagnosticResult.LineNumber (1-indexed)
    ↓ Convert (subtract 1)
Roslyn LinePosition (0-indexed)
    ↓ Get TextLine from SyntaxTree
TextSpan (character offset)
    ↓ Create Location
Roslyn Location
```

---

### 5. EditorConfig Severity Override

**Purpose**: Per-rule severity configuration from .editorconfig

**State**: Read-only (loaded from .editorconfig by Roslyn)

**Schema** (.editorconfig format):
```ini
[*.cs]
dotnet_diagnostic.LNT001.severity = none        # Suppress rule
dotnet_diagnostic.LNT002.severity = suggestion  # IDE hint only
dotnet_diagnostic.LNT003.severity = warning     # Compiler warning
dotnet_diagnostic.LNT004.severity = error       # Build failure
```

**Lifecycle**:
- **Load**: Roslyn AnalyzerConfigOptionsProvider parses .editorconfig at build time
- **Query**: context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree).TryGetValue(key)
- **Apply**: If severity == "none", skip rule execution; else override default severity

**Relationships**:
- **Loaded-By**: Roslyn AnalyzerConfigOptionsProvider
- **Read-By**: LintelligentDiagnosticAnalyzer.AnalyzeSyntaxTree()
- **Affects**: DiagnosticDescriptor effective severity (not default severity)

**Precedence** (handled by Roslyn):
1. File-specific .editorconfig (same directory as file)
2. Directory-level .editorconfig (nearest ancestor)
3. Project-level .editorconfig (project root)
4. Default severity (from DiagnosticDescriptor)

---

## Entity Relationships Diagram

```
┌─────────────────────────────────────────┐
│  Roslyn Analyzer Host                   │
│  (Visual Studio / Rider / MSBuild)      │
└─────────────┬───────────────────────────┘
              │ Loads & Initializes
              ▼
┌─────────────────────────────────────────┐
│  LintelligentDiagnosticAnalyzer         │
│  - Discovers IAnalyzerRule types        │
│  - Creates DiagnosticDescriptor[]       │
│  - Registers SyntaxTreeAction           │
└─────┬───────────────────────────────────┘
      │
      │ Has-Many
      ▼
┌─────────────────────────────────────────┐
│  IAnalyzerRule (x8)                     │
│  - LongMethodRule                       │
│  - LongParameterListRule                │
│  - ComplexConditionalRule               │
│  - MagicNumberRule                      │
│  - GodClassRule                         │
│  - DeadCodeRule                         │
│  - ExceptionSwallowingRule              │
│  - MissingXmlDocumentationRule          │
└─────┬───────────────────────────────────┘
      │
      │ Produces
      ▼
┌─────────────────────────────────────────┐
│  DiagnosticResult                       │
│  - FilePath: string                     │
│  - RuleId: string                       │
│  - Message: string                      │
│  - LineNumber: int (1-indexed)          │
│  - Severity: Severity                   │
│  - Category: string                     │
└─────┬───────────────────────────────────┘
      │
      │ Converted-To
      ▼
┌─────────────────────────────────────────┐
│  Roslyn Diagnostic                      │
│  - Descriptor: DiagnosticDescriptor     │
│  - Location: Location (0-indexed)       │
│  - Severity: DiagnosticSeverity         │
│  - Message: string                      │
└─────┬───────────────────────────────────┘
      │
      │ Reported-To
      ▼
┌─────────────────────────────────────────┐
│  IDE Error List / Build Output          │
│  - Visual Studio Error List             │
│  - Rider Problems Tool Window           │
│  - MSBuild Diagnostic Output            │
└─────────────────────────────────────────┘
```

## Data Flow

### 1. Initialization Flow

```
Roslyn Host Loads Analyzer
    ↓
LintelligentDiagnosticAnalyzer static constructor runs
    ↓
Reflection discovers IAnalyzerRule types in AnalyzerEngine assembly
    ↓
Instantiate each rule (parameterless constructor)
    ↓
RuleDescriptorFactory.Create(rule) → DiagnosticDescriptor
    ↓
Store in _rules[], _descriptors[], _descriptorMap
    ↓
Initialize() called by Roslyn
    ↓
Register SyntaxTreeAction callback
    ↓
Analyzer ready for compilation
```

### 2. Analysis Flow (Per File)

```
Roslyn compiles SyntaxTree
    ↓
Calls registered SyntaxTreeAction callback
    ↓
LintelligentDiagnosticAnalyzer.AnalyzeSyntaxTree(context)
    ↓
For each rule in _rules:
    ├─ Check EditorConfig for dotnet_diagnostic.{RuleId}.severity
    │   └─ If "none" → skip this rule
    ├─ Call rule.Analyze(context.Tree) → IEnumerable<DiagnosticResult>
    ├─ For each DiagnosticResult:
    │   ├─ Find matching DiagnosticDescriptor from _descriptorMap
    │   ├─ Convert LineNumber (1-indexed) → Location (0-indexed)
    │   ├─ Create Roslyn Diagnostic(descriptor, location, message)
    │   └─ context.ReportDiagnostic(diagnostic)
    └─ Continue to next rule
```

### 3. EditorConfig Flow

```
User creates .editorconfig with dotnet_diagnostic.LNT001.severity = error
    ↓
Roslyn parses .editorconfig at build time
    ↓
AnalyzerConfigOptionsProvider caches parsed values
    ↓
Analyzer queries: GetOptions(tree).TryGetValue("dotnet_diagnostic.LNT001.severity")
    ↓
If found:
    ├─ "none" → suppress diagnostic (skip rule execution)
    ├─ "suggestion" → map to DiagnosticSeverity.Info
    ├─ "warning" → map to DiagnosticSeverity.Warning
    └─ "error" → map to DiagnosticSeverity.Error, fail build
```

## State Management

### Stateless Components

All components are stateless (immutable or static readonly):

1. **LintelligentDiagnosticAnalyzer**: Static fields initialized once, never mutated
2. **IAnalyzerRule instances**: Stateless by constitution (Principle III)
3. **DiagnosticDescriptor**: Immutable after creation
4. **DiagnosticResult**: Immutable record
5. **RuleDescriptorFactory**: Static methods only (no instance state)
6. **DiagnosticConverter**: Static methods only (no instance state)
7. **SeverityMapper**: Static methods only (no instance state)

**Thread Safety**: All components thread-safe by immutability. Roslyn enables concurrent execution via `context.EnableConcurrentExecution()`.

### Cached Data

**Static Initialization Cache** (computed once, used throughout analyzer lifetime):
- `_rules`: IAnalyzerRule[] - Discovered via reflection at static init
- `_descriptors`: DiagnosticDescriptor[] - Created once per rule
- `_descriptorMap`: Dictionary<string, DiagnosticDescriptor> - Fast lookup by rule ID

**No Runtime Caching**: Analysis results NOT cached (rules are deterministic, re-running is fast).

### Memory Footprint

Estimated per analyzer instance:
- `_rules`: 8 objects × ~500 bytes = 4 KB
- `_descriptors`: 8 objects × ~1 KB = 8 KB
- `_descriptorMap`: 8 entries × ~100 bytes = 0.8 KB
- **Total**: ~13 KB (negligible)

Per-file analysis (transient):
- DiagnosticResult objects: ~5 per file × 200 bytes = 1 KB
- Roslyn Location objects: ~5 per file × 100 bytes = 0.5 KB
- **Total**: ~1.5 KB per file (GC'd after reporting)

**100-file solution**: ~13 KB static + ~1.5 KB transient per file = ~163 KB peak (acceptable)

## Validation & Error Handling

### Initialization Validation

```csharp
private static IAnalyzerRule[] DiscoverRules()
{
    var ruleTypes = typeof(IAnalyzerRule).Assembly.GetTypes()
        .Where(t => typeof(IAnalyzerRule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    var rules = new List<IAnalyzerRule>();
    foreach (var ruleType in ruleTypes)
    {
        try
        {
            var rule = (IAnalyzerRule)Activator.CreateInstance(ruleType)!;
            rules.Add(rule);
        }
        catch (Exception ex)
        {
            // Log error (to MSBuild diagnostic output) but continue
            // Don't fail analyzer load if one rule fails to initialize
            Debug.WriteLine($"Failed to load rule {ruleType.Name}: {ex.Message}");
        }
    }

    if (rules.Count == 0)
    {
        throw new InvalidOperationException("No IAnalyzerRule implementations found");
    }

    return rules.ToArray();
}
```

### Runtime Error Handling

```csharp
private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
{
    foreach (var rule in _rules)
    {
        try
        {
            // EditorConfig check, analysis, diagnostic reporting
        }
        catch (Exception ex)
        {
            // Log error but don't crash analyzer
            // Roslyn will continue with other files/rules
            var diagnostic = Diagnostic.Create(
                id: "LINT999",
                category: "InternalError",
                message: $"Analyzer error in {rule.Id}: {ex.Message}",
                severity: DiagnosticSeverity.Warning,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 1);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

**Error Handling Principles**:
- **Fail Gracefully**: Don't crash build if rule throws exception
- **Report Errors**: Log to MSBuild diagnostic output (visible to user)
- **Continue Analysis**: One failing rule doesn't block others
- **No Silent Failures**: Always report errors (Warning severity)

## Conclusion

All entities are immutable or stateless, ensuring thread safety and determinism. Data flows one-way from IAnalyzerRule through DiagnosticConverter to Roslyn Diagnostic. No persistence layer required (read-only analysis). EditorConfig integration handled by Roslyn infrastructure (no custom parsing). Simple, predictable, constitutional.
