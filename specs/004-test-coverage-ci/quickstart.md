# Quickstart: Test Coverage & CI Setup

**Feature**: 004-test-coverage-ci  
**Target Audience**: Developers working on Lintelligent  
**Last Updated**: 2025-12-24

---

## Prerequisites

- .NET 10 SDK installed ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Git repository cloned: `git clone <repo-url>`
- IDE: Visual Studio 2025, Rider 2024.3, or VS Code with C# extension

---

## Quick Commands

### Run All Tests (No Coverage)

```bash
cd Lintelligent
dotnet test
```

**Expected Output**:
```
Passed!  - Failed:     0, Passed:    89, Skipped:     0, Total:    89, Duration: 2.5s
```

---

### Run Tests with Coverage (Single Command)

```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Output Location**: `tests/{Project}.Tests/TestResults/{guid}/coverage.cobertura.xml`

**Use Case**: Quick coverage check during development

---

### Generate HTML Coverage Report Locally

```bash
# Install ReportGenerator (one-time setup)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report
reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:"Html"

# Open report in browser
start coveragereport/index.html  # Windows
open coveragereport/index.html   # macOS
xdg-open coveragereport/index.html  # Linux
```

**Visual Output**: Interactive HTML showing covered/uncovered lines per file

---

### Enforce 90% Coverage Threshold Locally

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:"Html;Cobertura" \
  -failonminimumcoverage:90
```

**Exit Codes**:
- `0`: Coverage ≥ 90% ✅
- `1`: Coverage < 90% ❌

**Use Case**: Pre-commit check to ensure CI will pass

---

## Running Specific Tests

### Run Tests for a Single Project

```bash
dotnet test tests/Lintelligent.AnalyzerEngine.Tests/
```

---

### Run Tests for a Specific Class

```bash
dotnet test --filter "FullyQualifiedName~LongMethodRuleTests"
```

---

### Run a Single Test Method

```bash
dotnet test --filter "FullyQualifiedName=Lintelligent.AnalyzerEngine.Tests.Rules.LongMethodRuleTests.Analyze_MethodExceeds20Lines_ReturnsDiagnostic"
```

---

## Writing New Tests

### Rule Test Template (100% Coverage Required)

**File**: `tests/Lintelligent.AnalyzerEngine.Tests/Rules/{RuleName}Tests.cs`

```csharp
using FluentAssertions;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules;

public class LongMethodRuleTests
{
    private readonly LongMethodRule _rule = new();

    [Fact]
    public void Analyze_MethodExceeds20Lines_ReturnsDiagnostic()
    {
        // Arrange
        var sourceCode = @"
            class TestClass
            {
                void LongMethod()
                {
                    var a = 1;
                    var b = 2;
                    // ... 20+ lines
                }
            }";
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Act
        var diagnostics = _rule.Analyze(syntaxTree).ToList();

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Id.Should().Be("LNT001");
    }

    [Fact]
    public void Analyze_MethodUnder20Lines_ReturnsNoDiagnostics()
    {
        // Arrange
        var sourceCode = @"
            class TestClass
            {
                void ShortMethod()
                {
                    var a = 1;
                }
            }";
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Act
        var diagnostics = _rule.Analyze(syntaxTree);

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_NullMethodBody_ReturnsNoDiagnostics()
    {
        // Arrange (abstract method has null body)
        var sourceCode = @"
            abstract class TestClass
            {
                abstract void AbstractMethod();
            }";
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Act
        var diagnostics = _rule.Analyze(syntaxTree);

        // Assert
        diagnostics.Should().BeEmpty();
    }
}
```

**Coverage Goal**: 100% of rule logic (all branches, all edge cases)

---

### Integration Test Template (Multi-Rule Orchestration)

**File**: `tests/Lintelligent.AnalyzerEngine.Tests/Analysis/AnalyzerEngineTests.cs`

```csharp
using FluentAssertions;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Rules;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Analysis;

public class AnalyzerEngineTests
{
    [Fact]
    public void Analyze_MultipleRules_AggregatesResults()
    {
        // Arrange
        var rules = new[] { new LongMethodRule() };
        var manager = new AnalyzerManager(rules);
        var engine = new AnalyzerEngine(manager);

        var sourceCode = @"
            class A { void Long() { /* 25 lines */ } }
            class B { void Short() { /* 5 lines */ } }";
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Act
        var diagnostics = engine.Analyze(syntaxTree).ToList();

        // Assert
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be("LNT001");
    }

    [Fact]
    public void Analyze_RuleThrowsException_ContinuesWithOtherRules()
    {
        // Arrange
        var faultyRule = new FaultyRule(); // Throws exception
        var goodRule = new LongMethodRule();
        var manager = new AnalyzerManager(new[] { faultyRule, goodRule });
        var engine = new AnalyzerEngine(manager);

        var syntaxTree = CSharpSyntaxTree.ParseText("class A {}");

        // Act
        var diagnostics = engine.Analyze(syntaxTree).ToList();

        // Assert - should not throw, goodRule should still run
        diagnostics.Should().NotBeNull();
    }
}
```

---

### CLI Test Template (In-Memory Execution)

**File**: `tests/Lintelligent.Cli.Tests/Commands/ScanCommandTests.cs`

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lintelligent.Cli.Tests.Commands;

public class ScanCommandTests
{
    [Fact]
    public async Task Execute_ValidArguments_ReturnsZeroExitCode()
    {
        // Arrange
        var services = new ServiceCollection();
        // Register in-memory test doubles here
        var app = services.BuildServiceProvider().GetRequiredService<ICliApplication>();

        // Act
        var exitCode = await app.RunAsync(new[] { "scan", "--path", "/test" });

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Execute_InvalidArguments_ReturnsExitCode2()
    {
        // Arrange
        var services = new ServiceCollection();
        var app = services.BuildServiceProvider().GetRequiredService<ICliApplication>();

        // Act
        var exitCode = await app.RunAsync(new[] { "invalid-command" });

        // Assert
        exitCode.Should().Be(2);
    }
}
```

---

## Understanding Coverage Reports

### HTML Report Structure

```
coveragereport/
├── index.html           # Overview (click to navigate)
├── Summary.html         # Coverage summary table
├── src_*.html           # Per-file coverage details
└── (CSS/JS assets)
```

**Color Coding** (in HTML report):
- 🟢 **Green**: Covered lines
- 🔴 **Red**: Uncovered lines
- 🟡 **Yellow**: Partially covered branches

---

### Interpreting Coverage Metrics

**Line Coverage**: Percentage of executable lines that were executed  
- **Target**: ≥90% (enforced in CI)
- **Example**: 185/200 lines = 92.5%

**Branch Coverage**: Percentage of conditional branches (if/switch) executed  
- **Target**: No explicit threshold (informational only)
- **Example**: 53/60 branches = 88.3%

**Method Coverage**: Percentage of methods executed at least once  
- **Target**: No explicit threshold (informational only)
- **Example**: 38/40 methods = 95.0%

---

## CI Integration (GitHub Actions)

### Workflow File Location

`.github/workflows/ci.yml`

**Triggers**:
- Every commit to any branch
- Every pull request

**Stages**:
1. Build all projects
2. Run all tests
3. Calculate coverage
4. Enforce 90% threshold
5. Publish HTML report as artifact

---

### Viewing CI Results

1. **Navigate to**: GitHub repository → Actions tab
2. **Find workflow**: Click on latest run (commit SHA)
3. **View logs**: Click on "Build, Test, and Coverage" job
4. **Download artifacts**: Scroll to "Artifacts" section → Download `coverage-report.zip`

**Coverage Failure Example**:
```
ERROR: Coverage check failed: 85.0% < 90%

Uncovered files:
  - src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerManager.cs: 60%
```

---

## Troubleshooting

### Tests Pass Locally, Fail in CI

**Cause**: Flaky tests (timing, randomness, environment-specific)  
**Solution**: Fix the test - CI does NOT retry (Constitutional requirement)

**Debug Steps**:
1. Reproduce locally: `dotnet test --logger "console;verbosity=detailed"`
2. Check for non-determinism (DateTime.Now, random numbers, file system)
3. Use in-memory implementations for external dependencies

---

### Coverage Report Not Generated

**Symptom**: `reportgenerator` fails with "No input reports found"

**Solution**:
```bash
# Verify coverage files were created
ls tests/*/TestResults/*/coverage.cobertura.xml

# If missing, ensure coverlet.collector package is installed
dotnet list package | grep coverlet
```

**Fix**: Run `dotnet add package coverlet.collector` in test projects

---

### Build Fails with "NU1301: Unable to load service index"

**Symptom**: NuGet package restore fails in CI

**Cause**: Network issue or NuGet service outage

**Solution**: CI will fail immediately (no retries). Wait for NuGet service to recover, then re-run workflow.

---

## Performance Benchmarks

**Local Development** (NFR-001):
- Test execution: <5 seconds for full suite
- Coverage report generation: ~2 seconds

**CI Pipeline** (NFR-002):
- Target: <5 minutes total
- Hard timeout: 10 minutes (FR-013)

**Current Stats** (as of 2025-12-24):
- Total tests: 89
- Typical local run: 2.5s
- Typical CI run: 3.2min (well under target)

---

## Next Steps

1. **For New Rules**: Create `{RuleName}Tests.cs` with 100% coverage
2. **For New Features**: Add integration tests in `AnalyzerEngine.Tests`
3. **Before Commit**: Run `dotnet test --collect:"XPlat Code Coverage"` and verify ≥90%
4. **After PR**: Check GitHub Actions for green build

---

## Resources

- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [FluentAssertions Documentation](https://fluentassertions.com/introduction)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
