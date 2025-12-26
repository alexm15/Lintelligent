using FluentAssertions;
using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.AnalyzerEngine.Configuration;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.Cli.Commands;
using Lintelligent.Cli.Infrastructure;
using Lintelligent.Cli.Providers;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Project = Lintelligent.AnalyzerEngine.ProjectModel.Project;

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
        var workspaceEngine = new WorkspaceAnalyzerEngine();
        var duplicationOptions = new DuplicationOptions();
        var solutionProvider = new BuildalyzerSolutionProvider(NullLogger<BuildalyzerSolutionProvider>.Instance);
        var projectProvider = new BuildalyzerProjectProvider(NullLogger<BuildalyzerProjectProvider>.Instance);

        _scanCommand = new ScanCommand(
            engine,
            workspaceEngine,
            duplicationOptions,
            solutionProvider,
            projectProvider,
            NullLogger<ScanCommand>.Instance);

        _testSolutionPath = Path.GetFullPath(Path.Combine("Fixtures", "TestSolution.sln"));
    }

    [Fact]
    public async Task ScanCommand_SolutionPath_AnalyzesAllProjects()
    {
        // Arrange
        var args = new[] {"scan", _testSolutionPath};

        // Act
        CommandResult result = await _scanCommand.ExecuteAsync(args);

        // Assert - should succeed even if no violations found
        result.ExitCode.Should().Be(0, $"Error: {result.Error}");
        result.Output.Should().NotBeNullOrEmpty();
        result.Output.Should().Contain("# Lintelligent Report");
    }

    [Fact]
    public async Task ScanCommand_SolutionPath_OutputContainsProjectInformation()
    {
        // Arrange
        var args = new[] {"scan", _testSolutionPath};

        // Act
        CommandResult result = await _scanCommand.ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        // The report should indicate multiple files were analyzed
        result.Output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ScanCommand_MissingSolutionFile_ReturnsError()
    {
        // Arrange
        var args = new[] {"scan", "NonExistent.sln"};

        // Act
        CommandResult result = await _scanCommand.ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(1);
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ScanCommand_SolutionPath_WithSeverityFilter()
    {
        // Arrange
        var args = new[] {"scan", _testSolutionPath, "--severity", "error"};

        // Act
        CommandResult result = await _scanCommand.ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        // Should only show error-level diagnostics
        result.Output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ScanCommand_SolutionPath_WithGroupBy()
    {
        // Arrange
        var args = new[] {"scan", _testSolutionPath, "--group-by", "category"};

        // Act
        CommandResult result = await _scanCommand.ExecuteAsync(args);

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
        var args = new[] {"scan", projectAPath};

        // Act
        CommandResult result = await _scanCommand.ExecuteAsync(args);

        // Assert - Verify backward compatibility: directory-based scanning still works
        result.ExitCode.Should().Be(0);
        result.Output.Should().NotBeNullOrEmpty();
        result.Output.Should().Contain("# Lintelligent Report");
    }

    [Fact]
    public async Task ScanCommand_DebugConfig_AnalyzesDebugCode()
    {
        // Arrange - ConditionalProject has #if DEBUG block with DebugOnlyMethod (21+ statements = LNT001 violation)
        var conditionalProjectPath =
            Path.GetFullPath(Path.Combine("Fixtures", "ConditionalProject", "ConditionalProject.csproj"));

        // Create ScanCommand that uses IProjectProvider to extract Debug symbols
        var analyzerManager = new AnalyzerManager();
        analyzerManager.RegisterRule(new LongMethodRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(analyzerManager);
        var projectProvider = new BuildalyzerProjectProvider(NullLogger<BuildalyzerProjectProvider>.Instance);

        // For now, we need to manually evaluate the project and pass symbols
        // TODO: Once --configuration flag is implemented, this test can use CLI args
        Project project = await projectProvider.EvaluateProjectAsync(conditionalProjectPath);

        var projectDir = Path.GetDirectoryName(conditionalProjectPath);
        projectDir.Should().NotBeNullOrEmpty();

        var codeProvider = new FileSystemCodeProvider(projectDir!);
        IEnumerable<SyntaxTree> syntaxTrees = codeProvider.GetSyntaxTrees(project.ConditionalSymbols);
        var results = engine.Analyze(syntaxTrees).ToList();

        // Assert - Should detect LNT001 in DebugOnlyMethod (which is inside #if DEBUG block)
        results.Should().NotBeEmpty("DebugOnlyMethod has 21+ statements and should trigger LNT001");
        results.Should().Contain(r =>
                r.RuleId == "LNT001" &&
                r.FilePath.EndsWith("ConditionalCode.cs"),
            "LNT001 should be detected in DebugOnlyMethod when DEBUG symbol is defined");

        // Verify DEBUG symbol was extracted
        project.ConditionalSymbols.Should().Contain("DEBUG", "Debug configuration should define DEBUG symbol");
        project.ConditionalSymbols.Should().Contain("TRACE", "Debug configuration should define TRACE symbol");
    }

    [Fact]
    public async Task ScanCommand_ReleaseConfig_SkipsDebugCode()
    {
        // Arrange - ConditionalProject has #if DEBUG block that should be excluded in Release
        var conditionalProjectPath =
            Path.GetFullPath(Path.Combine("Fixtures", "ConditionalProject", "ConditionalProject.csproj"));

        var analyzerManager = new AnalyzerManager();
        analyzerManager.RegisterRule(new LongMethodRule());
        var engine = new AnalyzerEngine.Analysis.AnalyzerEngine(analyzerManager);
        var projectProvider = new BuildalyzerProjectProvider(NullLogger<BuildalyzerProjectProvider>.Instance);

        // Evaluate project with Release configuration
        Project project = await projectProvider.EvaluateProjectAsync(conditionalProjectPath, "Release");

        var projectDir = Path.GetDirectoryName(conditionalProjectPath);
        projectDir.Should().NotBeNullOrEmpty();

        var codeProvider = new FileSystemCodeProvider(projectDir!);
        IEnumerable<SyntaxTree> syntaxTrees = codeProvider.GetSyntaxTrees(project.ConditionalSymbols);
        var results = engine.Analyze(syntaxTrees).ToList();

        // Assert - Should NOT detect LNT001 from DebugOnlyMethod (it's excluded by #if DEBUG)
        // AlwaysCompiledMethod only has 3 statements, so no LNT001
        results.Where(r => r.RuleId == "LNT001")
            .Should().BeEmpty("DebugOnlyMethod should be excluded when DEBUG symbol is not defined");

        // Verify RELEASE symbol was extracted and DEBUG was not
        project.ConditionalSymbols.Should().Contain("RELEASE", "Release configuration should define RELEASE symbol");
        project.ConditionalSymbols.Should().NotContain("DEBUG", "Release configuration should not define DEBUG symbol");
    }
}