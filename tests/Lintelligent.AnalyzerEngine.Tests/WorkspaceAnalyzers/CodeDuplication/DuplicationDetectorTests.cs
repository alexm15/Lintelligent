using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Project = Lintelligent.AnalyzerEngine.ProjectModel.Project;
using ProjectReference = Lintelligent.AnalyzerEngine.ProjectModel.ProjectReference;
using Solution = Lintelligent.AnalyzerEngine.ProjectModel.Solution;
using TargetFramework = Lintelligent.AnalyzerEngine.ProjectModel.TargetFramework;
using CompileItem = Lintelligent.AnalyzerEngine.ProjectModel.CompileItem;

namespace Lintelligent.AnalyzerEngine.Tests.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
/// Tests for DuplicationDetector - validates end-to-end workspace analysis.
/// </summary>
public sealed class DuplicationDetectorTests
{
    [Fact]
    public void Analyze_TwoIdentical15LineMethods_ReportsOneDuplication()
    {
        // Arrange - Create two files with identical 15-line methods
        var code1 = """
            public class Calculator1
            {
                public decimal CalculateTotal(Order order)
                {
                    decimal subtotal = 0;
                    foreach (var item in order.Items)
                    {
                        subtotal += item.Price * item.Quantity;
                    }
                    
                    decimal tax = subtotal * 0.08m;
                    decimal shipping = order.Items.Count > 5 ? 0 : 9.99m;
                    decimal discount = order.CouponCode != null ? subtotal * 0.1m : 0;
                    
                    return subtotal + tax + shipping - discount;
                }
            }
            """;

        var code2 = """
            public class OrderProcessor
            {
                public decimal CalculateTotal(Order order)
                {
                    decimal subtotal = 0;
                    foreach (var item in order.Items)
                    {
                        subtotal += item.Price * item.Quantity;
                    }
                    
                    decimal tax = subtotal * 0.08m;
                    decimal shipping = order.Items.Count > 5 ? 0 : 9.99m;
                    decimal discount = order.CouponCode != null ? subtotal * 0.1m : 0;
                    
                    return subtotal + tax + shipping - discount;
                }
            }
            """;

        var tree1 = CSharpSyntaxTree.ParseText(code1, path: "Calculator1.cs");
        var tree2 = CSharpSyntaxTree.ParseText(code2, path: "OrderProcessor.cs");

        var context = CreateWorkspaceContext("TestProject");
        var detector = new DuplicationDetector();

        // Act
        var diagnostics = detector.Analyze(new[] { tree1, tree2 }, context).ToList();

        // Assert
        diagnostics.Should().HaveCount(1, "because there is one duplication group");

        var diagnostic = diagnostics[0];
        diagnostic.Severity.Should().Be(Severity.Warning, "because duplications are warnings");
        diagnostic.FilePath.Should().Be("Calculator1.cs", "because it's the first instance alphabetically");
        diagnostic.Message.Should().Contain("duplicated", "because the message describes duplication");
        diagnostic.Message.Should().Contain("2", "because there are 2 instances");
    }

    [Fact]
    public void Analyze_NoDuplications_ReturnsEmpty()
    {
        // Arrange
        var code1 = "public class A { void Method1() { int x = 1; } }";
        var code2 = "public class B { void Method2() { string y = \"test\"; } }";

        var tree1 = CSharpSyntaxTree.ParseText(code1, path: "A.cs");
        var tree2 = CSharpSyntaxTree.ParseText(code2, path: "B.cs");

        var context = CreateWorkspaceContext("TestProject");
        var detector = new DuplicationDetector();

        // Act
        var diagnostics = detector.Analyze(new[] { tree1, tree2 }, context).ToList();

        // Assert
        diagnostics.Should().BeEmpty("because there are no duplications");
    }

    [Fact]
    public void Analyze_CrossProjectDuplication_IdentifiesProjectNames()
    {
        // Arrange
        var code = """
            public class SharedCode
            {
                public void Process()
                {
                    Console.WriteLine("Processing");
                }
            }
            """;

        var tree1 = CSharpSyntaxTree.ParseText(code, path: @"C:\Project1\Shared.cs");
        var tree2 = CSharpSyntaxTree.ParseText(code, path: @"C:\Project2\Shared.cs");

        var context = CreateWorkspaceContext("Solution", 
            ("Project1", @"C:\Project1\Project1.csproj"),
            ("Project2", @"C:\Project2\Project2.csproj"));

        var detector = new DuplicationDetector();

        // Act
        var diagnostics = detector.Analyze(new[] { tree1, tree2 }, context).ToList();

        // Assert
        diagnostics.Should().HaveCount(1);
        var diagnostic = diagnostics[0];
        
        // Message should indicate cross-project duplication
        diagnostic.Message.Should().Contain("Project1")
            .And.Contain("Project2", "because duplication spans both projects");
    }

    [Fact]
    public void Analyze_NullTrees_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateWorkspaceContext("TestProject");
        var detector = new DuplicationDetector();

        // Act
        var act = () => detector.Analyze(null!, context).ToList();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("trees");
    }

    [Fact]
    public void Analyze_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText("class A { }", path: "A.cs");
        var detector = new DuplicationDetector();

        // Act
        var act = () => detector.Analyze(new[] { tree }, null!).ToList();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Id_ReturnsExpectedValue()
    {
        // Arrange
        var detector = new DuplicationDetector();

        // Assert
        detector.Id.Should().Be("DUP001", "because this is the duplication detector ID");
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        // Arrange
        var detector = new DuplicationDetector();

        // Assert
        detector.Description.Should().NotBeNullOrWhiteSpace()
            .And.Contain("duplication", "because it describes duplication detection");
    }

    [Fact]
    public void Severity_IsWarning()
    {
        // Arrange
        var detector = new DuplicationDetector();

        // Assert
        detector.Severity.Should().Be(Severity.Warning,
            "because duplications are warnings, not errors");
    }

    [Fact]
    public void Category_IsCodeQuality()
    {
        // Arrange
        var detector = new DuplicationDetector();

        // Assert
        detector.Category.Should().Be("Code Quality",
            "because duplication is a code quality concern");
    }

    private static WorkspaceContext CreateWorkspaceContext(string solutionName, params (string Name, string Path)[] projects)
    {
        // Note: We only need minimal Project instances for testing DuplicationDetector
        // The actual project structure is not important for duplication detection tests
        var projectList = projects.Select(p =>
        {
            // Create a minimal Project with dummy values
            // In real usage, these would come from ISolutionProvider
            return new Project(
                filePath: p.Path,
                name: p.Name,
                targetFramework: new TargetFramework("net10.0"),
                allTargetFrameworks: new[] { new TargetFramework("net10.0") },
                conditionalSymbols: Array.Empty<string>(),
                configuration: "Debug",
                platform: "AnyCPU",
                outputType: "Library",
                compileItems: Array.Empty<CompileItem>(),
                projectReferences: Array.Empty<ProjectReference>());
        }).ToList();

        var solution = new Solution(
            filePath: $"C:\\{solutionName}.sln",
            name: solutionName,
            projects: projectList,
            configurations: new[] { "Debug", "Release" });

        var projectsByPath = solution.Projects.ToDictionary(
            p => p.FilePath,
            p => p,
            StringComparer.OrdinalIgnoreCase);

        return new WorkspaceContext
        {
            Solution = solution,
            ProjectsByPath = projectsByPath
        };
    }

    private static WorkspaceContext CreateWorkspaceContext(string projectName)
    {
        return CreateWorkspaceContext("TestSolution", (projectName, $"C:\\{projectName}\\{projectName}.csproj"));
    }
}
