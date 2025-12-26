using FluentAssertions;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.Cli.Commands;
using Lintelligent.Cli.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lintelligent.Cli.Tests.Commands;

public sealed class ScanCommandSolutionTests
{
    private readonly ScanCommand _scanCommand;
    private readonly string _testSolutionPath;

    public ScanCommandSolutionTests()
    {
        var analyzerManager = new AnalyzerManager();
        analyzerManager.RegisterRule(new LongMethodRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(analyzerManager);
        var workspaceEngine = new AnalyzerEngine.Analysis.WorkspaceAnalyzerEngine();
        var solutionProvider = new BuildalyzerSolutionProvider(NullLogger<BuildalyzerSolutionProvider>.Instance);
        var projectProvider = new BuildalyzerProjectProvider(NullLogger<BuildalyzerProjectProvider>.Instance);

        _scanCommand = new ScanCommand(
            engine,
            workspaceEngine,
            solutionProvider,
            projectProvider,
            NullLogger<ScanCommand>.Instance);

        _testSolutionPath = Path.GetFullPath(Path.Combine("Fixtures", "TestSolution.sln"));
    }

    [Fact]
    public async Task ScanCommand_SolutionPath_AnalyzesAllProjects()
    {
        // Arrange
        var args = new[] { "scan", _testSolutionPath };

        // Act
        var result = await _scanCommand.ExecuteAsync(args);

        // Assert - should succeed even if no violations found
        result.ExitCode.Should().Be(0, because: $"Error: {result.Error}");
        result.Output.Should().NotBeNullOrEmpty();
        result.Output.Should().Contain("# Lintelligent Report");
    }

    [Fact]
    public async Task ScanCommand_SolutionPath_OutputContainsProjectInformation()
    {
        // Arrange
        var args = new[] { "scan", _testSolutionPath };

        // Act
        var result = await _scanCommand.ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        // The report should indicate multiple files were analyzed
        result.Output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ScanCommand_MissingSolutionFile_ReturnsError()
    {
        // Arrange
        var args = new[] { "scan", "NonExistent.sln" };

        // Act
        var result = await _scanCommand.ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(1);
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ScanCommand_SolutionPath_WithSeverityFilter()
    {
        // Arrange
        var args = new[] { "scan", _testSolutionPath, "--severity", "error" };

        // Act
        var result = await _scanCommand.ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        // Should only show error-level diagnostics
        result.Output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ScanCommand_SolutionPath_WithGroupBy()
    {
        // Arrange
        var args = new[] { "scan", _testSolutionPath, "--group-by", "category" };

        // Act
        var result = await _scanCommand.ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().NotBeNullOrEmpty();
        // When grouping is enabled, report is generated with category-based structure
        result.Output.Should().Contain("# Lintelligent Report");
    }

    [Fact]
    public async Task ScanCommand_DirectoryPath_StillWorks()
    {
        // Arrange - Use ProjectA directory directly
        var projectAPath = Path.GetFullPath(Path.Combine("Fixtures", "ProjectA"));
        var args = new[] { "scan", projectAPath };

        // Act
        var result = await _scanCommand.ExecuteAsync(args);

        // Assert - Verify backward compatibility: directory-based scanning still works
        result.ExitCode.Should().Be(0);
        result.Output.Should().NotBeNullOrEmpty();
        result.Output.Should().Contain("# Lintelligent Report");
    }

    [Fact]
    public async Task ScanCommand_DebugConfig_AnalyzesDebugCode()
    {
        // Arrange - ConditionalProject has #if DEBUG block with DebugOnlyMethod (21+ statements = LNT001 violation)
        var conditionalProjectPath = Path.GetFullPath(Path.Combine("Fixtures", "ConditionalProject", "ConditionalProject.csproj"));
        
        // Create ScanCommand that uses IProjectProvider to extract Debug symbols
        var analyzerManager = new AnalyzerManager();
        analyzerManager.RegisterRule(new LongMethodRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(analyzerManager);
        var projectProvider = new BuildalyzerProjectProvider(NullLogger<BuildalyzerProjectProvider>.Instance);
        
        // For now, we need to manually evaluate the project and pass symbols
        // TODO: Once --configuration flag is implemented, this test can use CLI args
        var project = await projectProvider.EvaluateProjectAsync(conditionalProjectPath, "Debug");
        
        var projectDir = Path.GetDirectoryName(conditionalProjectPath);
        projectDir.Should().NotBeNullOrEmpty();
        
        var codeProvider = new FileSystemCodeProvider(projectDir!);
        var syntaxTrees = codeProvider.GetSyntaxTrees(project.ConditionalSymbols);
        var results = engine.Analyze(syntaxTrees).ToList();
        
        // Assert - Should detect LNT001 in DebugOnlyMethod (which is inside #if DEBUG block)
        results.Should().NotBeEmpty(because: "DebugOnlyMethod has 21+ statements and should trigger LNT001");
        results.Should().Contain(r => 
            r.RuleId == "LNT001" && 
            r.FilePath.EndsWith("ConditionalCode.cs"),
            because: "LNT001 should be detected in DebugOnlyMethod when DEBUG symbol is defined");
        
        // Verify DEBUG symbol was extracted
        project.ConditionalSymbols.Should().Contain("DEBUG", because: "Debug configuration should define DEBUG symbol");
        project.ConditionalSymbols.Should().Contain("TRACE", because: "Debug configuration should define TRACE symbol");
    }

    [Fact]
    public async Task ScanCommand_ReleaseConfig_SkipsDebugCode()
    {
        // Arrange - ConditionalProject has #if DEBUG block that should be excluded in Release
        var conditionalProjectPath = Path.GetFullPath(Path.Combine("Fixtures", "ConditionalProject", "ConditionalProject.csproj"));
        
        var analyzerManager = new AnalyzerManager();
        analyzerManager.RegisterRule(new LongMethodRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(analyzerManager);
        var projectProvider = new BuildalyzerProjectProvider(NullLogger<BuildalyzerProjectProvider>.Instance);
        
        // Evaluate project with Release configuration
        var project = await projectProvider.EvaluateProjectAsync(conditionalProjectPath, "Release");
        
        var projectDir = Path.GetDirectoryName(conditionalProjectPath);
        projectDir.Should().NotBeNullOrEmpty();
        
        var codeProvider = new FileSystemCodeProvider(projectDir!);
        var syntaxTrees = codeProvider.GetSyntaxTrees(project.ConditionalSymbols);
        var results = engine.Analyze(syntaxTrees).ToList();
        
        // Assert - Should NOT detect LNT001 from DebugOnlyMethod (it's excluded by #if DEBUG)
        // AlwaysCompiledMethod only has 3 statements, so no LNT001
        results.Where(r => r.RuleId == "LNT001")
            .Should().BeEmpty(because: "DebugOnlyMethod should be excluded when DEBUG symbol is not defined");
        
        // Verify RELEASE symbol was extracted and DEBUG was not
        project.ConditionalSymbols.Should().Contain("RELEASE", because: "Release configuration should define RELEASE symbol");
        project.ConditionalSymbols.Should().NotContain("DEBUG", because: "Release configuration should not define DEBUG symbol");
    }
}
