using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Analysis;using Lintelligent.AnalyzerEngine.Configuration;using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using CompileItem = Lintelligent.AnalyzerEngine.ProjectModel.CompileItem;
using Project = Lintelligent.AnalyzerEngine.ProjectModel.Project;
using ProjectReference = Lintelligent.AnalyzerEngine.ProjectModel.ProjectReference;
using Solution = Lintelligent.AnalyzerEngine.ProjectModel.Solution;
using TargetFramework = Lintelligent.AnalyzerEngine.ProjectModel.TargetFramework;
using WorkspaceContext = Lintelligent.AnalyzerEngine.Abstractions.WorkspaceContext;

namespace Lintelligent.AnalyzerEngine.Tests.Analysis;

/// <summary>
/// Tests for WorkspaceAnalyzerEngine - validates workspace-level analysis orchestration.
/// </summary>
public sealed class WorkspaceAnalyzerEngineTests
{
    [Fact]
    public void Analyze_FiveProjects_AllProjectsIncluded()
    {
        // Arrange - Create workspace context with 5 projects
        var projects = new[]
        {
            CreateProject("Project1", @"C:\Solution\Project1\Project1.csproj"),
            CreateProject("Project2", @"C:\Solution\Project2\Project2.csproj"),
            CreateProject("Project3", @"C:\Solution\Project3\Project3.csproj"),
            CreateProject("Project4", @"C:\Solution\Project4\Project4.csproj"),
            CreateProject("Project5", @"C:\Solution\Project5\Project5.csproj")
        };

        var solution = new Solution(
            filePath: @"C:\Solution\TestSolution.sln",
            name: "TestSolution",
            projects: projects,
            configurations: new[] { "Debug", "Release" });

        var context = new WorkspaceContext(
            solution,
            solution.Projects.ToDictionary(
                p => p.FilePath,
                p => p,
                StringComparer.OrdinalIgnoreCase));

        // Create 5 syntax trees (one per project) with identical code (multi-line to ensure detection)
        var code = """
            public class TestClass
            {
                void Method()
                {
                    int x = 42;
                }
            }
            """;
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText(code, path: @"C:\Solution\Project1\Test.cs"),
            CSharpSyntaxTree.ParseText(code, path: @"C:\Solution\Project2\Test.cs"),
            CSharpSyntaxTree.ParseText(code, path: @"C:\Solution\Project3\Test.cs"),
            CSharpSyntaxTree.ParseText(code, path: @"C:\Solution\Project4\Test.cs"),
            CSharpSyntaxTree.ParseText(code, path: @"C:\Solution\Project5\Test.cs")
        };

        var engine = new WorkspaceAnalyzerEngine();
        var options = new DuplicationOptions { MinLines = 1, MinTokens = 1 }; // Low thresholds to detect this small code
        engine.RegisterAnalyzer(new DuplicationDetector(options));

        // Act
        var diagnostics = engine.Analyze(trees, context).ToList();

        // Assert - Should detect 1 duplication group spanning all 5 projects
        diagnostics.Should().HaveCount(1, "because there is one duplication across all projects");

        var diagnostic = diagnostics[0];
        diagnostic.Message.Should().Contain("5 files", "because duplication spans 5 projects");
        
        // Verify all project names appear in message
        diagnostic.Message.Should().Contain("Project1")
            .And.Contain("Project2")
            .And.Contain("Project3")
            .And.Contain("Project4")
            .And.Contain("Project5");
    }

    [Fact]
    public void RegisterAnalyzer_ValidAnalyzer_Succeeds()
    {
        // Arrange
        var engine = new WorkspaceAnalyzerEngine();
        var analyzer = new DuplicationDetector();

        // Act
        engine.RegisterAnalyzer(analyzer);

        // Assert
        engine.Analyzers.Should().HaveCount(1);
        engine.Analyzers.First().Should().Be(analyzer);
    }

    [Fact]
    public void RegisterAnalyzer_NullAnalyzer_ThrowsArgumentNullException()
    {
        // Arrange
        var engine = new WorkspaceAnalyzerEngine();

        // Act
        var act = () => engine.RegisterAnalyzer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("analyzer");
    }

    [Fact]
    public void RegisterAnalyzers_MultipleAnalyzers_AllRegistered()
    {
        // Arrange
        var engine = new WorkspaceAnalyzerEngine();
        var analyzers = new IWorkspaceAnalyzer[]
        {
            new DuplicationDetector(),
            new DuplicationDetector() // In real usage, would be different analyzers
        };

        // Act
        engine.RegisterAnalyzers(analyzers);

        // Assert
        engine.Analyzers.Should().HaveCount(2);
    }

    [Fact]
    public void Analyze_NullTrees_ThrowsArgumentNullException()
    {
        // Arrange
        var engine = new WorkspaceAnalyzerEngine();
        engine.RegisterAnalyzer(new DuplicationDetector());
        var context = CreateMinimalContext();

        // Act
        var act = () => engine.Analyze(null!, context).ToList();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("trees");
    }

    [Fact]
    public void Analyze_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var engine = new WorkspaceAnalyzerEngine();
        engine.RegisterAnalyzer(new DuplicationDetector());
        var trees = new[] { CSharpSyntaxTree.ParseText("class A { }", path: "A.cs") };

        // Act
        var act = () => engine.Analyze(trees, null!).ToList();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Analyze_NoAnalyzersRegistered_ReturnsEmpty()
    {
        // Arrange
        var engine = new WorkspaceAnalyzerEngine();
        var context = CreateMinimalContext();
        var trees = new[] { CSharpSyntaxTree.ParseText("class A { }", path: "A.cs") };

        // Act
        var diagnostics = engine.Analyze(trees, context).ToList();

        // Assert
        diagnostics.Should().BeEmpty("because no analyzers are registered");
    }

    [Fact]
    public void Analyze_SequentialExecution_DeterministicResults()
    {
        // Arrange
        var code = "public class Duplicate { void Method() { Console.WriteLine(\"test\"); } }";
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText(code, path: "File1.cs"),
            CSharpSyntaxTree.ParseText(code, path: "File2.cs")
        };

        var context = CreateMinimalContext();
        var engine = new WorkspaceAnalyzerEngine();
        engine.RegisterAnalyzer(new DuplicationDetector());

        // Act - Run analysis twice
        var results1 = engine.Analyze(trees, context).ToList();
        var results2 = engine.Analyze(trees, context).ToList();

        // Assert - Results should be identical (deterministic)
        results1.Should().HaveCount(results2.Count);
        for (int i = 0; i < results1.Count; i++)
        {
            results1[i].RuleId.Should().Be(results2[i].RuleId);
            results1[i].FilePath.Should().Be(results2[i].FilePath);
            results1[i].LineNumber.Should().Be(results2[i].LineNumber);
            results1[i].Message.Should().Be(results2[i].Message);
        }
    }

    private static Project CreateProject(string name, string path)
    {
        return new Project(
            filePath: path,
            name: name,
            targetFramework: new TargetFramework("net10.0"),
            allTargetFrameworks: new[] { new TargetFramework("net10.0") },
            conditionalSymbols: Array.Empty<string>(),
            configuration: "Debug",
            platform: "AnyCPU",
            outputType: "Library",
            compileItems: Array.Empty<CompileItem>(),
            projectReferences: Array.Empty<ProjectReference>());
    }

    private static WorkspaceContext CreateMinimalContext()
    {
        var project = CreateProject("TestProject", @"C:\TestProject\TestProject.csproj");
        var solution = new Solution(
            filePath: @"C:\TestSolution.sln",
            name: "TestSolution",
            projects: new[] { project },
            configurations: new[] { "Debug" });

        return new WorkspaceContext(
            solution,
            new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase)
            {
                { project.FilePath, project }
            });
    }
}
