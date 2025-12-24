# GitHub Actions CI Workflow Contract

**Feature**: 004-test-coverage-ci  
**Contract Type**: GitHub Actions Workflow YAML  
**File**: `.github/workflows/ci.yml`  
**Date**: 2025-12-24

---

## Workflow Schema

```yaml
name: .NET CI

on:
  push:
    branches: ["**"]  # All branches
  pull_request:
    branches: ["**"]  # All PRs

jobs:
  build-and-test:
    name: Build, Test, and Coverage
    runs-on: ubuntu-latest
    timeout-minutes: 10  # NFR-002: target <5min, hard limit 10min
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'  # .NET 10 preview
          
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Build
        run: dotnet build --no-restore --configuration Release
        
      - name: Run tests with coverage
        run: |
          dotnet test \
            --no-build \
            --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage
        
      - name: Install ReportGenerator
        run: dotnet tool install -g dotnet-reportgenerator-globaltool
        
      - name: Generate coverage report and enforce threshold
        run: |
          reportgenerator \
            -reports:"coverage/**/coverage.cobertura.xml" \
            -targetdir:"coveragereport" \
            -reporttypes:"Html;Cobertura" \
            -verbosity:Info \
            -failonminimumcoverage:90
        
      - name: Publish coverage report
        uses: actions/upload-artifact@v4
        if: always()  # Upload even if coverage check failed
        with:
          name: coverage-report
          path: coveragereport/
          retention-days: 90
```

---

## Stage Contracts

### Stage 1: Checkout

**Purpose**: Clone repository code  
**Tool**: `actions/checkout@v4`  
**Exit Codes**:
- `0`: Success
- Non-zero: Git clone failed (e.g., network issue, auth failure)

**Failure Impact**: Pipeline stops immediately

---

### Stage 2: Setup .NET

**Purpose**: Install .NET 10 SDK  
**Tool**: `actions/setup-dotnet@v4`  
**Inputs**:
- `dotnet-version`: `10.0.x` (latest .NET 10 preview)

**Exit Codes**:
- `0`: SDK installed successfully
- Non-zero: SDK download/installation failed

**Failure Impact**: Pipeline stops (cannot proceed without SDK)

---

### Stage 3: Restore Dependencies

**Purpose**: Download NuGet packages  
**Command**: `dotnet restore`  
**Exit Codes**:
- `0`: All packages restored
- Non-zero: Package download failure, authentication failure, or missing package

**Failure Behavior** (FR-016):
- No retries
- Fail immediately with clear error message
- Requires manual intervention

**Example Error Output**:
```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json.
The build failed. Fix the build errors and run again.
```

---

### Stage 4: Build

**Purpose**: Compile all projects  
**Command**: `dotnet build --no-restore --configuration Release`  
**Exit Codes**:
- `0`: Build succeeded
- Non-zero: Compilation errors

**Failure Behavior**:
- Pipeline stops before tests run
- Logs show compilation errors with file paths and line numbers

**Example Error Output**:
```
src/Lintelligent.AnalyzerEngine/Rules/LongMethodRule.cs(15,9): error CS0103: The name 'foo' does not exist in the current context
```

---

### Stage 5: Run Tests with Coverage

**Purpose**: Execute all xUnit tests and collect coverage  
**Command**: `dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage" --results-directory ./coverage`  
**Exit Codes**:
- `0`: All tests passed
- Non-zero: At least one test failed

**Failure Behavior** (FR-015):
- No automatic retries for flaky tests
- All test failures require manual investigation

**Output Structure**:
```
./coverage/
  {guid}/
    coverage.cobertura.xml  # Coverlet output
```

**Example Test Failure Output**:
```
Failed Lintelligent.AnalyzerEngine.Tests.Rules.LongMethodRuleTests.Analyze_MethodExceeds20Lines_ReturnsDiagnostic [< 1 ms]
  Error Message:
   Expected diagnostics to contain single item, but found 0.
  Stack Trace:
     at Lintelligent.AnalyzerEngine.Tests.Rules.LongMethodRuleTests.Analyze_MethodExceeds20Lines_ReturnsDiagnostic() in LongMethodRuleTests.cs:line 25
```

**Performance Contract** (NFR-001):
- Tests SHOULD complete in <5 seconds locally
- Timeout: 10 minutes in CI (FR-013)

---

### Stage 6: Install ReportGenerator

**Purpose**: Install coverage threshold enforcement tool  
**Command**: `dotnet tool install -g dotnet-reportgenerator-globaltool`  
**Exit Codes**:
- `0`: Tool installed
- Non-zero: Installation failed (e.g., NuGet failure)

**Failure Behavior**: Pipeline stops (cannot enforce coverage without tool)

---

### Stage 7: Generate Coverage Report and Enforce Threshold

**Purpose**: Calculate coverage metrics and fail build if <90%  
**Command**:
```bash
reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:"Html;Cobertura" \
  -verbosity:Info \
  -failonminimumcoverage:90
```

**Exit Codes**:
- `0`: Coverage ≥ 90% (FR-008 satisfied)
- Non-zero: Coverage < 90% (build MUST fail)

**Success Output**:
```
Summary
  Generated input report: coverage/abc123/coverage.cobertura.xml
  Line coverage: 92.5% (185 of 200 lines)
  Branch coverage: 88.3%
  Method coverage: 95.0%
  
  Coverage check passed: 92.5% >= 90%
```

**Failure Output** (FR-014):
```
Summary
  Line coverage: 85.0% (170 of 200 lines)
  
ERROR: Coverage check failed: 85.0% < 90%

Uncovered files:
  - src/Lintelligent.AnalyzerEngine/Analysis/AnalyzerManager.cs: 60% (lines 45-52, 78-80)
  - src/Lintelligent.Cli/Commands/ScanCommand.cs: 75% (lines 12-15)
```

**Generated Artifacts**:
- `coveragereport/index.html`: Human-readable HTML report (NFR-003)
- `coveragereport/Cobertura.xml`: Machine-readable coverage data

---

### Stage 8: Publish Coverage Report

**Purpose**: Upload HTML coverage report as CI artifact  
**Tool**: `actions/upload-artifact@v4`  
**Inputs**:
- `name`: `coverage-report`
- `path`: `coveragereport/`
- `retention-days`: `90`

**Condition**: `if: always()` (upload even if coverage check failed, for debugging)

**Exit Codes**:
- `0`: Artifact uploaded
- Non-zero: Upload failed (rare)

**Artifact Access**: Available via GitHub Actions UI for 90 days

---

## Failure Propagation

```
Checkout FAIL → Stop immediately
  ↓
Setup .NET FAIL → Stop immediately
  ↓
Restore FAIL → Stop immediately (no retries, FR-016)
  ↓
Build FAIL → Stop immediately (no tests run)
  ↓
Tests FAIL → Stop immediately (no retries, FR-015)
  ↓
Install ReportGenerator FAIL → Stop immediately
  ↓
Coverage <90% → FAIL build (FR-008, FR-014)
  ↓
Publish Artifact FAIL → Warning only (doesn't fail build)
```

---

## Logging Contract (NFR-005)

**What is logged**:
- Standard build output (compiler messages, warnings)
- All test results (passed/failed/skipped with counts)
- Failed test details: error message + full stack trace
- Coverage summary: line/branch/method percentages
- Uncovered file list when coverage check fails

**What is NOT logged**:
- Verbose MSBuild diagnostics (use `--verbosity:diagnostic` only for debugging)
- Individual test output (unless test fails)
- Binary files or intermediate build artifacts

**Example Standard Log**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Test run for /home/runner/work/Lintelligent/Lintelligent/tests/Lintelligent.AnalyzerEngine.Tests/bin/Release/net10.0/Lintelligent.AnalyzerEngine.Tests.dll (.NETCoreApp,Version=v10.0)
Microsoft (R) Test Execution Command Line Tool Version 17.14.1

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15, Duration: 1.2s
```

---

## Performance Contracts

| Metric | Target (NFR) | Hard Limit (FR) | Measurement |
|--------|-------------|----------------|-------------|
| Local test execution | <5s | N/A | `dotnet test` duration |
| CI pipeline total | <5min | 10min timeout | Stage 1-8 cumulative |
| Test execution in CI | N/A | 10min | Stage 5 timeout (FR-013) |

---

## Security & Access

**Required GitHub Settings**:
- GitHub Actions enabled for repository
- Workflow permissions: Read repository contents, write artifacts

**No Secrets Required**: This workflow uses only public NuGet feeds and GitHub-hosted runners

---

## Trigger Contract (SC-007)

**Branches**: All branches (`branches: ["**"]`)  
**Events**:
- `push`: Every commit to any branch
- `pull_request`: Every PR opened/updated

**No Gaps**: 100% of commits trigger CI validation (SC-007)

---

## Success Criteria Mapping

| Success Criteria | Contract Verification |
|------------------|----------------------|
| SC-001: 100% rules have tests | Tests fail if rule has no tests (implicit) |
| SC-002: ≥90% coverage | Stage 7 enforces threshold |
| SC-003: <5s local tests | Measured locally (not CI contract) |
| SC-004: <5min CI feedback | 10min timeout, target <5min |
| SC-005: No false positives | ReportGenerator deterministic calculation |
| SC-006: Clear reports | HTML report shows uncovered lines |
| SC-007: 100% commits validated | Trigger: `push` on all branches |
| SC-008: Single command local | `dotnet test --collect:"XPlat Code Coverage"` |
