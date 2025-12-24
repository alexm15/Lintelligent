# Quickstart Guide: Roslyn Analyzer Bridge Implementation

**Feature**: 019-roslyn-analyzer-bridge  
**Date**: December 24, 2025  
**Audience**: Developers implementing the Roslyn analyzer integration

## Overview

This guide provides step-by-step instructions for implementing the Roslyn Analyzer Bridge. Implementation follows a bottom-up approach: utilities first, then adapters, then main analyzer, then packaging.

## Prerequisites

- ✅ Feature 005 (Core Rule Library) complete - IAnalyzerRule implementations available
- ✅ .NET SDK 6.0+ installed (for netstandard2.0 compilation)
- ✅ Visual Studio 2022 or Rider 2024.3+ (for testing)
- ✅ Microsoft.CodeAnalysis.CSharp 4.0+ NuGet package knowledge

## Implementation Phases

### Phase 1: Project Setup (15 minutes)

**1.1 Create Lintelligent.Analyzers Project**

```bash
cd src/
dotnet new classlib -n Lintelligent.Analyzers -f netstandard2.0
dotnet sln add Lintelligent.Analyzers/Lintelligent.Analyzers.csproj
```

**1.2 Update .csproj Configuration**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    
    <!-- NuGet Packaging -->
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <IncludeBuildOutput>false</IncludeBuildOutput> <!-- Exclude from lib/ directory -->
    <DevelopmentDependency>true</DevelopmentDependency>
    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
    
    <!-- Package Metadata -->
    <PackageId>Lintelligent.Analyzers</PackageId>
    <Version>1.0.0</Version>
    <Authors>Lintelligent Team</Authors>
    <Description>Roslyn analyzer integration for Lintelligent code quality rules (LNT001-LNT008)</Description>
    <PackageTags>roslyn analyzer code-quality static-analysis</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <!-- Roslyn Analyzer APIs -->
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.11.0" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference AnalyzerEngine (contains IAnalyzerRule implementations) -->
    <ProjectReference Include="../Lintelligent.AnalyzerEngine/Lintelligent.AnalyzerEngine.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Include analyzer DLL in package -->
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
    <None Include="$(OutputPath)\Lintelligent.AnalyzerEngine.dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  </ItemGroup>
</Project>
```

**1.3 Verify Build**

```bash
dotnet build src/Lintelligent.Analyzers/
# Expected: Build succeeded, netstandard2.0 DLL created
```

---

### Phase 2: Metadata & Utilities (30 minutes)

**2.1 Create SeverityMapper**

**File**: `src/Lintelligent.Analyzers/Metadata/SeverityMapper.cs`

```csharp
using Microsoft.CodeAnalysis;
using Lintelligent.AnalyzerEngine.Abstractions;

namespace Lintelligent.Analyzers.Metadata;

/// <summary>
/// Converts Lintelligent Severity to Roslyn DiagnosticSeverity.
/// </summary>
public static class SeverityMapper
{
    public static DiagnosticSeverity ToRoslynSeverity(Severity severity)
    {
        return severity switch
        {
            Severity.Error => DiagnosticSeverity.Error,
            Severity.Warning => DiagnosticSeverity.Warning,
            Severity.Info => DiagnosticSeverity.Info,
            _ => throw new ArgumentException($"Undefined severity: {severity}", nameof(severity))
        };
    }

    public static DiagnosticSeverity FromEditorConfigSeverity(string editorConfigSeverity)
    {
        return editorConfigSeverity?.ToLowerInvariant() switch
        {
            "suggestion" => DiagnosticSeverity.Info,
            "warning" => DiagnosticSeverity.Warning,
            "error" => DiagnosticSeverity.Error,
            "none" => throw new InvalidOperationException("Severity 'none' should be handled by caller"),
            _ => throw new ArgumentException($"Invalid EditorConfig severity: {editorConfigSeverity}", nameof(editorConfigSeverity))
        };
    }

    public static bool IsSuppressed(string editorConfigSeverity)
    {
        return string.Equals(editorConfigSeverity, "none", StringComparison.OrdinalIgnoreCase);
    }
}
```

**2.2 Create RuleDescriptorFactory**

**File**: `src/Lintelligent.Analyzers/Adapters/RuleDescriptorFactory.cs`

```csharp
using Microsoft.CodeAnalysis;
using Lintelligent.AnalyzerEngine.Rules;

namespace Lintelligent.Analyzers.Adapters;

/// <summary>
/// Factory for creating DiagnosticDescriptor from IAnalyzerRule.
/// </summary>
public static class RuleDescriptorFactory
{
    private const string BaseHelpUrl = "https://github.com/[ORG]/Lintelligent/blob/main/specs/005-core-rule-library/rules-documentation.md";

    private static readonly Dictionary<string, string> RuleAnchors = new()
    {
        ["LNT001"] = "lnt001-long-method",
        ["LNT002"] = "lnt002-long-parameter-list",
        ["LNT003"] = "lnt003-complex-conditional",
        ["LNT004"] = "lnt004-magic-number",
        ["LNT005"] = "lnt005-god-class",
        ["LNT006"] = "lnt006-dead-code",
        ["LNT007"] = "lnt007-exception-swallowing",
        ["LNT008"] = "lnt008-missing-xml-documentation"
    };

    public static DiagnosticDescriptor Create(IAnalyzerRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new DiagnosticDescriptor(
            id: rule.Id,
            title: rule.Description,
            messageFormat: "{0}",  // Filled with DiagnosticResult.Message
            category: rule.Category,
            defaultSeverity: SeverityMapper.ToRoslynSeverity(rule.Severity),
            isEnabledByDefault: true,
            description: rule.Description,
            helpLinkUri: GetHelpLinkUri(rule.Id),
            customTags: GetCustomTags(rule.Category));
    }

    private static string GetHelpLinkUri(string ruleId)
    {
        return RuleAnchors.TryGetValue(ruleId, out var anchor)
            ? $"{BaseHelpUrl}#{anchor}"
            : BaseHelpUrl;
    }

    private static string[] GetCustomTags(string category)
    {
        var tags = new List<string> { "CodeQuality" };

        if (category == "Maintainability") tags.Add("Maintainability");
        else if (category == "CodeSmell") tags.Add("CodeSmell");
        else if (category == "Documentation") tags.Add("Documentation");

        return tags.ToArray();
    }
}
```

**2.3 Create DiagnosticConverter**

**File**: `src/Lintelligent.Analyzers/Adapters/DiagnosticConverter.cs`

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.Analyzers.Adapters;

/// <summary>
/// Converts DiagnosticResult to Roslyn Diagnostic.
/// </summary>
public static class DiagnosticConverter
{
    public static Diagnostic Convert(DiagnosticResult result, SyntaxTree tree, DiagnosticDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(descriptor);

        var location = CreateLocation(result.LineNumber, tree);
        return Diagnostic.Create(descriptor, location, result.Message);
    }

    private static Location CreateLocation(int lineNumber, SyntaxTree tree)
    {
        var text = tree.GetText();
        if (text.Lines.Count == 0)
        {
            return Location.None;  // Empty file edge case
        }

        // Convert 1-indexed to 0-indexed, clamp to valid range
        var roslynLine = Math.Clamp(lineNumber - 1, 0, text.Lines.Count - 1);
        var textLine = text.Lines[roslynLine];

        return Location.Create(tree, textLine.Span);
    }
}
```

---

### Phase 3: Main Analyzer (45 minutes)

**3.1 Create LintelligentDiagnosticAnalyzer**

**File**: `src/Lintelligent.Analyzers/LintelligentDiagnosticAnalyzer.cs`

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.Analyzers.Adapters;
using Lintelligent.Analyzers.Metadata;

namespace Lintelligent.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LintelligentDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private static readonly IAnalyzerRule[] Rules = DiscoverRules();
    private static readonly DiagnosticDescriptor[] Descriptors = CreateDescriptors(Rules);
    private static readonly Dictionary<string, DiagnosticDescriptor> DescriptorMap = Descriptors.ToDictionary(d => d.Id);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
        => ImmutableArray.Create(Descriptors);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);  // Skip generated code
        context.EnableConcurrentExecution();  // Thread-safe parallel execution
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var configOptions = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);

        foreach (var rule in Rules)
        {
            try
            {
                // Check EditorConfig for severity override
                if (configOptions.TryGetValue($"dotnet_diagnostic.{rule.Id}.severity", out var severity))
                {
                    if (SeverityMapper.IsSuppressed(severity))
                    {
                        continue;  // Suppressed via EditorConfig
                    }
                }

                // Execute rule analysis
                var results = rule.Analyze(context.Tree);
                var descriptor = DescriptorMap[rule.Id];

                foreach (var result in results)
                {
                    var diagnostic = DiagnosticConverter.Convert(result, context.Tree, descriptor);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash analyzer
                ReportInternalError(context, rule.Id, ex.Message);
            }
        }
    }

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
                System.Diagnostics.Debug.WriteLine($"Failed to load rule {ruleType.Name}: {ex.Message}");
            }
        }

        if (rules.Count == 0)
        {
            throw new InvalidOperationException("No IAnalyzerRule implementations found");
        }

        return rules.ToArray();
    }

    private static DiagnosticDescriptor[] CreateDescriptors(IAnalyzerRule[] rules)
    {
        return rules.Select(RuleDescriptorFactory.Create).ToArray();
    }

    private static void ReportInternalError(SyntaxTreeAnalysisContext context, string ruleId, string error)
    {
        var descriptor = new DiagnosticDescriptor(
            id: "LINT999",
            title: "Internal Analyzer Error",
            messageFormat: "Analyzer error in {0}: {1}",
            category: "InternalError",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        var diagnostic = Diagnostic.Create(descriptor, Location.None, ruleId, error);
        context.ReportDiagnostic(diagnostic);
    }
}
```

---

### Phase 4: Testing Setup (30 minutes)

**4.1 Create Test Project**

```bash
cd tests/
dotnet new xunit -n Lintelligent.Analyzers.Tests
dotnet sln add Lintelligent.Analyzers.Tests/Lintelligent.Analyzers.Tests.csproj
```

**4.2 Add Test Dependencies**

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit" Version="1.1.2" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.12.0" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="FluentAssertions" Version="6.8.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="../../src/Lintelligent.Analyzers/Lintelligent.Analyzers.csproj" />
</ItemGroup>
```

**4.3 Create Sample Integration Test**

**File**: `tests/Lintelligent.Analyzers.Tests/Integration/LongMethodRuleAnalyzerTests.cs`

```csharp
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Lintelligent.Analyzers.Tests.Integration;

public class LongMethodRuleAnalyzerTests
{
    [Fact]
    public async Task Analyze_MethodWith30Statements_ProducesDiagnostic()
    {
        var testCode = @"
class TestClass
{
    void LongMethod()
    {
        var x = 1;
        var y = 2;
        // ... 28 more statements to reach 30 total
    }
}";

        var expected = new DiagnosticResult("LNT001", DiagnosticSeverity.Warning)
            .WithSpan(4, 10, 4, 20);

        await CSharpAnalyzerVerifier<LintelligentDiagnosticAnalyzer, XUnitVerifier>
            .VerifyAnalyzerAsync(testCode, expected);
    }
}
```

---

### Phase 5: Package & Deploy (20 minutes)

**5.1 Build NuGet Package**

```bash
dotnet pack src/Lintelligent.Analyzers/ -c Release
# Output: bin/Release/Lintelligent.Analyzers.1.0.0.nupkg
```

**5.2 Inspect Package Structure**

```bash
# Extract .nupkg (it's a zip file)
unzip -l bin/Release/Lintelligent.Analyzers.1.0.0.nupkg

# Expected structure:
# analyzers/dotnet/cs/Lintelligent.Analyzers.dll
# analyzers/dotnet/cs/Lintelligent.AnalyzerEngine.dll
# [package metadata]
```

**5.3 Test Package Locally**

```bash
# Add local package source
dotnet nuget add source ./bin/Release --name LocalAnalyzers

# Create test project
dotnet new console -n AnalyzerTest
cd AnalyzerTest
dotnet add package Lintelligent.Analyzers

# Write code violating LNT001
# Build and verify diagnostic appears
dotnet build
```

**5.4 Publish to NuGet** (optional)

```bash
dotnet nuget push bin/Release/Lintelligent.Analyzers.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

---

## Verification Checklist

- [ ] Project builds without errors (netstandard2.0)
- [ ] All 8 rules discovered via reflection
- [ ] DiagnosticDescriptors created with correct metadata
- [ ] SeverityMapper correctly maps Error/Warning/Info
- [ ] DiagnosticConverter handles 1-indexed → 0-indexed conversion
- [ ] Analyzer reports diagnostics in test project
- [ ] EditorConfig suppression works (`dotnet_diagnostic.LNT001.severity = none`)
- [ ] Help links navigate to correct documentation
- [ ] NuGet package structure correct (analyzers/dotnet/cs directory)
- [ ] Package installs successfully in test project

---

## Troubleshooting

**Problem**: Analyzer not discovered by IDE

**Solution**: Ensure package is in `analyzers/dotnet/cs` directory, restart IDE after package install

---

**Problem**: Rules not found (InvalidOperationException)

**Solution**: Verify Lintelligent.AnalyzerEngine.dll is included in package alongside analyzer DLL

---

**Problem**: EditorConfig settings ignored

**Solution**: Check .editorconfig syntax (`dotnet_diagnostic.LNT001.severity = warning`), ensure file in project root

---

**Problem**: Build performance slow (>10% overhead)

**Solution**: Profile with `dotnet build /clp:PerformanceSummary`, check for rule execution bottlenecks

---

## Next Steps

After completing implementation:
1. Run `/speckit.tasks` to generate detailed task breakdown
2. Implement each phase systematically
3. Test with real-world projects (100+ files)
4. Document deployment process for CI/CD
5. Create user documentation for package consumers

**Estimated Total Time**: 2-3 hours (excluding testing and refinement)
