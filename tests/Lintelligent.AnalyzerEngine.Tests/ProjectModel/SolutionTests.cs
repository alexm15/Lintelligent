using FluentAssertions;
using Lintelligent.AnalyzerEngine.ProjectModel;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.ProjectModel;

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
            projectBPath,
            "ProjectB",
            targetFramework,
            new[] {targetFramework},
            Array.Empty<string>(),
            "Debug",
            "AnyCPU",
            "Library",
            Array.Empty<CompileItem>(),
            Array.Empty<ProjectReference>());

        var projectA = new Project(
            projectAPath,
            "ProjectA",
            targetFramework,
            new[] {targetFramework},
            Array.Empty<string>(),
            "Debug",
            "AnyCPU",
            "Library",
            Array.Empty<CompileItem>(),
            new[] {projectBRef});

        var solution = new Solution(
            Path.GetFullPath(@"C:\Projects\TestSolution.sln"),
            "TestSolution",
            new[] {projectA, projectB},
            new[] {"Debug", "Release"});

        // Act
        IReadOnlyDictionary<string, IReadOnlyList<string>> graph = solution.GetDependencyGraph();

        // Assert
        graph.Should().NotBeNull();
        graph.Should().HaveCount(2, "solution has 2 projects");

        graph.Should().ContainKey(projectAPath, "ProjectA is in the solution");
        graph.Should().ContainKey(projectBPath, "ProjectB is in the solution");

        graph[projectAPath].Should().HaveCount(1, "ProjectA references one project");
        graph[projectAPath].Should().Contain(projectBPath, "ProjectA references ProjectB");

        graph[projectBPath].Should().BeEmpty("ProjectB has no project references");
    }

    [Fact]
    public void GetDependencyGraph_NoReferences_ReturnsEmptyDependencies()
    {
        // Arrange - Create Solution with no project references
        var projectAPath = Path.GetFullPath(@"C:\Projects\ProjectA\ProjectA.csproj");
        var projectBPath = Path.GetFullPath(@"C:\Projects\ProjectB\ProjectB.csproj");
        var targetFramework = new TargetFramework("net8.0");

        var projectA = new Project(
            projectAPath,
            "ProjectA",
            targetFramework,
            new[] {targetFramework},
            Array.Empty<string>(),
            "Debug",
            "AnyCPU",
            "Library",
            Array.Empty<CompileItem>(),
            Array.Empty<ProjectReference>());

        var projectB = new Project(
            projectBPath,
            "ProjectB",
            targetFramework,
            new[] {targetFramework},
            Array.Empty<string>(),
            "Debug",
            "AnyCPU",
            "Library",
            Array.Empty<CompileItem>(),
            Array.Empty<ProjectReference>());

        var solution = new Solution(
            Path.GetFullPath(@"C:\Projects\TestSolution.sln"),
            "TestSolution",
            new[] {projectA, projectB},
            new[] {"Debug"});

        // Act
        IReadOnlyDictionary<string, IReadOnlyList<string>> graph = solution.GetDependencyGraph();

        // Assert
        graph.Should().NotBeNull();
        graph.Should().HaveCount(2);
        graph[projectAPath].Should().BeEmpty();
        graph[projectBPath].Should().BeEmpty();
    }
}