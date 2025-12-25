namespace Lintelligent.AnalyzerEngine.Tests.ProjectModel;

using Lintelligent.AnalyzerEngine.ProjectModel;
using FluentAssertions;
using Xunit;

public class SolutionTests
{
    [Fact]
    public void GetDependencyGraph_WithReferences_ReturnsDependencyMap()
    {
        // Arrange - Create Solution with ProjectA → ProjectB dependency
        var projectBPath = Path.GetFullPath(@"C:\Projects\ProjectB\ProjectB.csproj");
        var projectAPath = Path.GetFullPath(@"C:\Projects\ProjectA\ProjectA.csproj");
        
        var projectBRef = new ProjectReference(projectBPath, "ProjectB");
        var targetFramework = new TargetFramework("net8.0");
        
        var projectB = new Project(
            filePath: projectBPath,
            name: "ProjectB",
            targetFramework: targetFramework,
            allTargetFrameworks: new[] { targetFramework },
            conditionalSymbols: Array.Empty<string>(),
            configuration: "Debug",
            platform: "AnyCPU",
            outputType: "Library",
            compileItems: Array.Empty<CompileItem>(),
            projectReferences: Array.Empty<ProjectReference>());
        
        var projectA = new Project(
            filePath: projectAPath,
            name: "ProjectA",
            targetFramework: targetFramework,
            allTargetFrameworks: new[] { targetFramework },
            conditionalSymbols: Array.Empty<string>(),
            configuration: "Debug",
            platform: "AnyCPU",
            outputType: "Library",
            compileItems: Array.Empty<CompileItem>(),
            projectReferences: new[] { projectBRef });
        
        var solution = new Solution(
            Path.GetFullPath(@"C:\Projects\TestSolution.sln"),
            "TestSolution",
            new[] { projectA, projectB },
            new[] { "Debug", "Release" });

        // Act
        var graph = solution.GetDependencyGraph();

        // Assert
        graph.Should().NotBeNull();
        graph.Should().HaveCount(2, because: "solution has 2 projects");
        
        graph.Should().ContainKey(projectAPath, because: "ProjectA is in the solution");
        graph.Should().ContainKey(projectBPath, because: "ProjectB is in the solution");
        
        graph[projectAPath].Should().HaveCount(1, because: "ProjectA references one project");
        graph[projectAPath].Should().Contain(projectBPath, because: "ProjectA references ProjectB");
        
        graph[projectBPath].Should().BeEmpty(because: "ProjectB has no project references");
    }

    [Fact]
    public void GetDependencyGraph_NoReferences_ReturnsEmptyDependencies()
    {
        // Arrange - Create Solution with no project references
        var projectAPath = Path.GetFullPath(@"C:\Projects\ProjectA\ProjectA.csproj");
        var projectBPath = Path.GetFullPath(@"C:\Projects\ProjectB\ProjectB.csproj");
        var targetFramework = new TargetFramework("net8.0");
        
        var projectA = new Project(
            filePath: projectAPath,
            name: "ProjectA",
            targetFramework: targetFramework,
            allTargetFrameworks: new[] { targetFramework },
            conditionalSymbols: Array.Empty<string>(),
            configuration: "Debug",
            platform: "AnyCPU",
            outputType: "Library",
            compileItems: Array.Empty<CompileItem>(),
            projectReferences: Array.Empty<ProjectReference>());
        
        var projectB = new Project(
            filePath: projectBPath,
            name: "ProjectB",
            targetFramework: targetFramework,
            allTargetFrameworks: new[] { targetFramework },
            conditionalSymbols: Array.Empty<string>(),
            configuration: "Debug",
            platform: "AnyCPU",
            outputType: "Library",
            compileItems: Array.Empty<CompileItem>(),
            projectReferences: Array.Empty<ProjectReference>());
        
        var solution = new Solution(
            Path.GetFullPath(@"C:\Projects\TestSolution.sln"),
            "TestSolution",
            new[] { projectA, projectB },
            new[] { "Debug" });

        // Act
        var graph = solution.GetDependencyGraph();

        // Assert
        graph.Should().NotBeNull();
        graph.Should().HaveCount(2);
        graph[projectAPath].Should().BeEmpty();
        graph[projectBPath].Should().BeEmpty();
    }
}
