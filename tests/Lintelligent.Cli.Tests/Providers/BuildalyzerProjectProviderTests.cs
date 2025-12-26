using FluentAssertions;
using Lintelligent.AnalyzerEngine.ProjectModel;
using Lintelligent.Cli.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lintelligent.Cli.Tests.Providers;

public class BuildalyzerProjectProviderTests
{
    private readonly string _conditionalProjectPath;
    private readonly BuildalyzerProjectProvider _provider;
    private readonly string _testSolutionPath;

    public BuildalyzerProjectProviderTests()
    {
        _provider = new BuildalyzerProjectProvider(NullLogger<BuildalyzerProjectProvider>.Instance);
        _conditionalProjectPath =
            Path.GetFullPath(Path.Combine("Fixtures", "ConditionalProject", "ConditionalProject.csproj"));
        _testSolutionPath = Path.GetFullPath(Path.Combine("Fixtures", "TestSolution.sln"));
    }

    [Fact]
    public async Task EvaluateProjectAsync_DebugConfig_ExtractsDebugSymbols()
    {
        // Act
        Project project = await _provider.EvaluateProjectAsync(_conditionalProjectPath);

        // Assert
        project.Should().NotBeNull();
        project.Configuration.Should().Be("Debug");
        project.ConditionalSymbols.Should().Contain("DEBUG");
        project.ConditionalSymbols.Should().Contain("TRACE");
        project.ConditionalSymbols.Should().NotContain("RELEASE");
    }

    [Fact]
    public async Task EvaluateProjectAsync_ReleaseConfig_ExtractsReleaseSymbols()
    {
        // Act
        Project project = await _provider.EvaluateProjectAsync(_conditionalProjectPath, "Release");

        // Assert
        project.Should().NotBeNull();
        project.Configuration.Should().Be("Release");
        project.ConditionalSymbols.Should().Contain("RELEASE");
        project.ConditionalSymbols.Should().NotContain("DEBUG");
    }

    [Fact]
    public async Task EvaluateProjectAsync_ValidProject_ExtractsMetadata()
    {
        // Act
        Project project = await _provider.EvaluateProjectAsync(_conditionalProjectPath);

        // Assert
        project.FilePath.Should().Be(_conditionalProjectPath);
        project.Name.Should().Be("ConditionalProject");
        project.TargetFramework.Should().NotBeNull();
        project.TargetFramework.Moniker.Should().Contain("net");
        project.AllTargetFrameworks.Should().NotBeEmpty();
        project.Platform.Should().NotBeNullOrEmpty();
        project.OutputType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EvaluateProjectAsync_ValidProject_ExtractsSourceFiles()
    {
        // Act
        Project project = await _provider.EvaluateProjectAsync(_conditionalProjectPath);

        // Assert
        project.CompileItems.Should().NotBeEmpty();
        project.CompileItems.Should().Contain(item => item.FilePath.EndsWith("ConditionalCode.cs"));
    }

    [Fact]
    public async Task EvaluateProjectAsync_InvalidProject_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidProjectPath = Path.GetFullPath(Path.Combine("Fixtures", "NonExistent.csproj"));

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _provider.EvaluateProjectAsync(invalidProjectPath));
    }

    [Fact]
    public async Task EvaluateAllProjectsAsync_ValidSolution_EvaluatesAllProjects()
    {
        // Arrange
        var solutionProvider = new BuildalyzerSolutionProvider(NullLogger<BuildalyzerSolutionProvider>.Instance);
        Solution solution = await solutionProvider.ParseSolutionAsync(_testSolutionPath);

        // Act
        Solution evaluatedSolution = await _provider.EvaluateAllProjectsAsync(solution);

        // Assert
        evaluatedSolution.Should().NotBeNull();
        evaluatedSolution.Projects.Should().NotBeEmpty();
        evaluatedSolution.Projects.Should().AllSatisfy(p =>
        {
            p.ConditionalSymbols.Should().NotBeNull();
            p.CompileItems.Should().NotBeNull();
            p.TargetFramework.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task EvaluateAllProjectsAsync_OneProjectFails_ReturnsPartialResults()
    {
        // Arrange
        var solutionProvider = new BuildalyzerSolutionProvider(NullLogger<BuildalyzerSolutionProvider>.Instance);
        Solution solution = await solutionProvider.ParseSolutionAsync(_testSolutionPath);

        // Corrupt one project path to cause evaluation failure (use cross-platform path)
        var nonExistentPath = Path.DirectorySeparatorChar == '\\'
            ? "C:\\NonExistent\\Project.csproj"
            : "/nonexistent/Project.csproj";

        var corruptedProjects = solution.Projects.Take(1).Select(p =>
            new Project(
                nonExistentPath,
                "CorruptedProject",
                p.TargetFramework,
                p.AllTargetFrameworks,
                p.ConditionalSymbols,
                p.Configuration,
                p.Platform,
                p.OutputType,
                p.CompileItems,
                p.ProjectReferences
            )).Concat(solution.Projects.Skip(1)).ToList();

        var corruptedSolution = new Solution(
            solution.FilePath,
            solution.Name,
            corruptedProjects,
            solution.Configurations
        );

        // Act
        Solution result = await _provider.EvaluateAllProjectsAsync(corruptedSolution);

        // Assert
        result.Should().NotBeNull();
        result.Projects.Count.Should().BeLessThan(corruptedSolution.Projects.Count);
        result.Projects.Should().AllSatisfy(p => p.CompileItems.Should().NotBeNull());
    }

    [Fact]
    public async Task EvaluateProjectAsync_ExtractsPlatform()
    {
        // Act
        Project project = await _provider.EvaluateProjectAsync(_conditionalProjectPath);

        // Assert
        project.Platform.Should().NotBeNullOrEmpty();
        project.Platform.Should().BeOneOf("AnyCPU", "x64", "x86", "ARM", "ARM64");
    }

    [Fact]
    public async Task EvaluateProjectAsync_ExtractsOutputType()
    {
        // Act
        Project project = await _provider.EvaluateProjectAsync(_conditionalProjectPath);

        // Assert
        project.OutputType.Should().NotBeNullOrEmpty();
        project.OutputType.Should().BeOneOf("Library", "Exe", "WinExe");
    }

    [Fact]
    public async Task EvaluateProjectAsync_CompileRemove_ExcludesFiles()
    {
        // Arrange - CompileDirectivesProject has <Compile Remove="Generated/**/*.cs" />
        var compileDirectivesProjectPath = Path.GetFullPath(Path.Combine("Fixtures", "CompileDirectivesProject",
            "CompileDirectivesProject.csproj"));

        // Act
        Project project = await _provider.EvaluateProjectAsync(compileDirectivesProjectPath);

        // Assert - GeneratedFile.cs should be excluded from CompileItems
        project.CompileItems.Should().NotBeNull();
        project.CompileItems.Should().NotContain(item =>
                item.FilePath.Contains("Generated") && item.FilePath.EndsWith("GeneratedFile.cs"),
            "files matching <Compile Remove> pattern should be excluded");
    }

    [Fact]
    public async Task EvaluateProjectAsync_CompileInclude_IncludesLinkedFiles()
    {
        // Arrange - CompileDirectivesProject has <Compile Include="..\Shared\SharedCode.cs" Link="Shared\SharedCode.cs" />
        var compileDirectivesProjectPath = Path.GetFullPath(Path.Combine("Fixtures", "CompileDirectivesProject",
            "CompileDirectivesProject.csproj"));

        // Act
        Project project = await _provider.EvaluateProjectAsync(compileDirectivesProjectPath);

        // Assert - SharedCode.cs should be included as a LinkedFile
        project.CompileItems.Should().NotBeNull();
        project.CompileItems.Should().Contain(item =>
                item.FilePath.EndsWith("SharedCode.cs") &&
                item.InclusionType == CompileItemInclusionType.LinkedFile,
            "linked files from <Compile Include> should be included with LinkedFile type");
    }

    [Fact]
    public async Task EvaluateProjectAsync_SDKGlobs_IncludesAllCsFiles()
    {
        // Arrange - CompileDirectivesProject should include IncludedFile.cs by SDK default glob
        var compileDirectivesProjectPath = Path.GetFullPath(Path.Combine("Fixtures", "CompileDirectivesProject",
            "CompileDirectivesProject.csproj"));

        // Act
        Project project = await _provider.EvaluateProjectAsync(compileDirectivesProjectPath);

        // Assert - IncludedFile.cs should be included as DefaultGlob
        project.CompileItems.Should().NotBeNull();
        project.CompileItems.Should().Contain(item =>
                item.FilePath.EndsWith("IncludedFile.cs") &&
                item.InclusionType == CompileItemInclusionType.DefaultGlob,
            "files in project directory should be included by SDK glob pattern");

        // Verify the count makes sense (IncludedFile.cs + SharedCode.cs, but NOT GeneratedFile.cs)
        project.CompileItems.Count.Should().BeGreaterThanOrEqualTo(2,
            "should have at least IncludedFile.cs and SharedCode.cs");
    }

    // Phase 6: Multi-Project Analysis Aggregation Tests

    [Fact]
    public async Task EvaluateProjectAsync_ProjectReferences_CapturesReferences()
    {
        // Arrange - ProjectA references ProjectB
        var projectAPath = Path.GetFullPath(Path.Combine("Fixtures", "ProjectA", "ProjectA.csproj"));

        // Act
        Project project = await _provider.EvaluateProjectAsync(projectAPath);

        // Assert - ProjectReferences should contain ProjectB
        project.ProjectReferences.Should().NotBeNull();
        project.ProjectReferences.Should().HaveCount(1, "ProjectA references only ProjectB");

        ProjectReference projectBRef = project.ProjectReferences.First();
        projectBRef.ReferencedProjectName.Should().Be("ProjectB", "the referenced project name should be extracted");
        projectBRef.ReferencedProjectPath.Should().Contain("ProjectB.csproj", "should reference ProjectB.csproj");
        Path.IsPathRooted(projectBRef.ReferencedProjectPath).Should()
            .BeTrue("project reference paths should be absolute");
    }

    [Fact]
    public async Task EvaluateProjectAsync_NoProjectReferences_ReturnsEmptyList()
    {
        // Arrange - ProjectB has no project references
        var projectBPath = Path.GetFullPath(Path.Combine("Fixtures", "ProjectB", "ProjectB.csproj"));

        // Act
        Project project = await _provider.EvaluateProjectAsync(projectBPath);

        // Assert
        project.ProjectReferences.Should().NotBeNull();
        project.ProjectReferences.Should().BeEmpty("ProjectB has no project references");
    }

    // Phase 7: Multi-Targeting Support Tests

    [Fact]
    public async Task EvaluateProjectAsync_MultiTarget_SelectsFirstByDefault()
    {
        // Arrange - MultiTargetProject targets net472 and net8.0
        var multiTargetProjectPath =
            Path.GetFullPath(Path.Combine("Fixtures", "MultiTargetProject", "MultiTargetProject.csproj"));

        // Act - Evaluate without specifying target framework
        Project project = await _provider.EvaluateProjectAsync(multiTargetProjectPath);

        // Assert - Should select first target framework (net472)
        project.Should().NotBeNull();
        project.TargetFramework.Should().NotBeNull();
        project.TargetFramework.Moniker.Should().Be("net472", "net472 is first in <TargetFrameworks>");

        // Verify AllTargetFrameworks contains both targets
        project.AllTargetFrameworks.Should().HaveCount(2, "project targets both net472 and net8.0");
        project.AllTargetFrameworks.Should().Contain(tf => tf.Moniker == "net472");
        project.AllTargetFrameworks.Should().Contain(tf => tf.Moniker == "net8.0");

        project.IsMultiTargeted.Should().BeTrue("project has multiple target frameworks");
    }

    [Fact]
    public async Task EvaluateProjectAsync_MultiTarget_SelectsSpecifiedTarget()
    {
        // Arrange
        var multiTargetProjectPath =
            Path.GetFullPath(Path.Combine("Fixtures", "MultiTargetProject", "MultiTargetProject.csproj"));

        // Act - Evaluate with target framework specified
        Project project = await _provider.EvaluateProjectAsync(multiTargetProjectPath, targetFramework: "net8.0");

        // Assert - Should select net8.0
        project.Should().NotBeNull();
        project.TargetFramework.Moniker.Should().Be("net8.0", "net8.0 was explicitly specified");

        // AllTargetFrameworks should still contain both
        project.AllTargetFrameworks.Should().HaveCount(2);
        project.AllTargetFrameworks.Should().Contain(tf => tf.Moniker == "net472");
        project.AllTargetFrameworks.Should().Contain(tf => tf.Moniker == "net8.0");
    }

    [Fact]
    public async Task EvaluateProjectAsync_MultiTarget_InvalidTarget_ThrowsException()
    {
        // Arrange
        var multiTargetProjectPath =
            Path.GetFullPath(Path.Combine("Fixtures", "MultiTargetProject", "MultiTargetProject.csproj"));

        // Act & Assert - Should throw when invalid target framework specified
        Func<Task<Project>> act = async () =>
            await _provider.EvaluateProjectAsync(multiTargetProjectPath, targetFramework: "net6.0");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*net6.0*not found*", "net6.0 is not a target in this project")
            .WithMessage("*net472*net8.0*", "error message should list available frameworks");
    }
}