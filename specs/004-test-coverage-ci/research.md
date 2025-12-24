# Research: Test Coverage & CI Setup

**Date**: 2025-12-24  
**Feature**: 004-test-coverage-ci  
**Phase**: 0 - Outline & Research

## Research Questions Resolved

### 1. Coverage Tool Configuration

**Decision**: Use coverlet.collector package with `dotnet test --collect:"XPlat Code Coverage"` command

**Rationale**:
- Built-in support in .NET project templates
- Cross-platform compatibility (Windows, Linux, macOS)
- Generates industry-standard Cobertura XML format
- Seamless integration with GitHub Actions runners

**Implementation**: 
- Package already installed: `coverlet.collector` v6.0.4 in test projects
- Command: `dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage`
- Output: `coverage/{guid}/coverage.cobertura.xml`

**Alternatives Considered**:
- Microsoft Code Coverage: Requires binary `.coverage` file conversion
- coverlet.msbuild: Less portable, more MSBuild coupling
- **Verdict**: coverlet.collector optimal for CI/CD scenarios

---

### 2. xUnit Test Organization & Patterns

**Decision**: Mirror source structure with parallel test directories; use Arrange-Act-Assert pattern with FluentAssertions

**Rationale**:
- Mirrored structure enables quick navigation (LongMethodRule.cs ↔ LongMethodRuleTests.cs)
- AAA pattern makes tests self-documenting
- FluentAssertions provides expressive diagnostic validation
- Single concern per test for isolation

**Implementation**:
```csharp
// Roslyn analyzer testing pattern
var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
var rule = new LongMethodRule();

var diagnostics = rule.Analyze(syntaxTree);

diagnostics.Should().ContainSingle()
    .Which.Id.Should().Be("LNT001");
```

**Best Practices**:
- Use `[Fact]` for invariant tests
- Use `[Theory]` + `[InlineData]` for parameterized tests
- Constructor injection for shared fixtures
- No Setup/Teardown - prefer helper methods

**Alternatives Considered**:
- MSTest/NUnit: xUnit more modern, better parallelization
- Flat test structure: Harder to navigate in large codebases

---

### 3. GitHub Actions CI/CD Pipeline

**Decision**: Multi-stage workflow with explicit build → test → coverage → enforce → publish stages

**Rationale**:
- Clear failure points (build vs test vs coverage)
- Artifacts enable downstream quality gates
- Performance optimization via `--no-build`, `--no-restore` flags
- Standard `ubuntu-latest` runners for cost efficiency

**Implementation**:
```yaml
steps:
  - Setup .NET 10.0.x
  - dotnet restore
  - dotnet build --no-restore --configuration Release
  - dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"
  - reportgenerator (coverage enforcement)
  - upload-artifact (HTML reports)
```

**Key Configuration**:
- `--results-directory ./coverage` consolidates output
- Glob pattern: `coverage/**/coverage.cobertura.xml` for multi-project solutions
- Always upload artifacts (`if: always()`) for debugging failures

**Alternatives Considered**:
- Azure Pipelines: More enterprise features but less GitHub integration
- Single-step workflow: Less transparent for failure diagnosis

---

### 4. Coverage Threshold Enforcement (90%)

**Decision**: Use ReportGenerator with `--failonminimumcoverage:90` flag in CI pipeline

**Rationale**:
- Build-time enforcement (fails fast before merge)
- No code changes required (enforced at pipeline level)
- Generates HTML reports for local inspection
- Self-contained tool, no external SaaS dependencies

**Implementation**:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool

reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:"Html;Cobertura" \
  -verbosity:Info \
  -failonminimumcoverage:90
```

**Exit Behavior**:
- Exit code 0: Coverage ≥ 90% (build passes)
- Non-zero exit code: Coverage < 90% (build fails, stops pipeline)

**Alternatives Considered**:
- coverlet.msbuild with `/p:Threshold=90`: Less flexible, MSBuild-only
- Post-test XML parsing scripts: Fragile, error-prone
- Third-party services (Codecov, Coveralls): External dependencies
- **Verdict**: ReportGenerator self-contained, works offline

---

### 5. In-Memory CLI Testing Strategy

**Decision**: Expose testable CLI entry point via dependency injection; test with in-memory service doubles

**Rationale**:
- No process spawning: Faster tests (ms vs seconds)
- Deterministic behavior: No file system coupling
- Service wiring verification: Catches DI misconfiguration
- Exit code capture: Validates command-line contract

**Implementation Pattern**:
```csharp
// Production CLI (expose ConfigureServices)
public static async Task<int> Main(string[] args)
{
    var services = new ServiceCollection();
    ConfigureServices(services);
    var provider = services.BuildServiceProvider();
    var app = provider.GetRequiredService<ICliApplication>();
    return await app.RunAsync(args);
}

// Test
[Fact]
public async Task ScanCommand_ValidPath_ReturnsZeroExitCode()
{
    var services = new ServiceCollection();
    services.AddSingleton<ICodeProvider, InMemoryCodeProvider>(); // Test double
    services.AddSingleton<ICliApplication, CliApplication>();
    
    var app = services.BuildServiceProvider().GetRequiredService<ICliApplication>();
    var exitCode = await app.RunAsync(new[] { "scan", "--path", "/test" });
    
    exitCode.Should().Be(0);
}
```

**Output Capture** (if needed):
```csharp
using var consoleOutput = new StringWriter();
Console.SetOut(consoleOutput);
var exitCode = await app.RunAsync(args);
consoleOutput.ToString().Should().Contain("Analysis complete");
```

**Gotchas**:
- Avoid `Environment.Exit()` in production - throw exceptions instead
- Test both success (0) and error scenarios (1, 2)

**Alternatives Considered**:
- Process.Start: Slow, platform-specific, requires build
- Integration tests only: Miss service wiring bugs
- **Verdict**: In-memory testing optimal speed/confidence balance

---

## Technology Stack Summary

| Component | Choice | Version/Details |
|-----------|--------|----------------|
| **Test Framework** | xUnit | 2.9.3 (already installed) |
| **Assertions** | FluentAssertions | 6.8.0 (already installed) |
| **Coverage Tool** | Coverlet Collector | 6.0.4 (already installed) |
| **Threshold Enforcement** | ReportGenerator | Global tool (install in CI) |
| **CI Platform** | GitHub Actions | ubuntu-latest runner |
| **Roslyn Testing** | Microsoft.CodeAnalysis.CSharp.Workspaces | 4.12.0 (already installed) |

**No additional NuGet packages required** - all dependencies already present in test projects.

---

## Key Decisions for Implementation

1. **No test retries**: Fail immediately on flaky tests (Constitutional alignment)
2. **No NuGet retries**: Fail immediately on package download failures
3. **Standard logging**: Build output + test results + stack traces (not verbose)
4. **Coverage granularity**: Line, branch, and method coverage metrics
5. **Timeout**: 10-minute test execution timeout in CI
6. **Local testing**: Single command `dotnet test --collect:"XPlat Code Coverage"`

---

## Next Phase: Design

With research complete, Phase 1 will generate:
- **data-model.md**: Test entities (TestResult, CoverageReport, TestSuite)
- **contracts/**: CI workflow YAML schema
- **quickstart.md**: Developer guide for running tests and coverage locally
