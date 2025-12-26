using FluentAssertions;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Configuration;
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

        // Assert - Now finds multiple duplications: whole method + sub-sequences within method
        diagnostics.Should().HaveCountGreaterThanOrEqualTo(1, "because at minimum the whole method is duplicated");
        
        // Should find the full 17-line duplication (class + method declaration + body)
        var fullDup = diagnostics.FirstOrDefault(d => d.Message.Contains("17 lines"));
        fullDup.Should().NotBeNull("because the full class/method should be detected");
        fullDup!.Severity.Should().Be(Severity.Warning, "because duplications are warnings");
        fullDup.FilePath.Should().Be("Calculator1.cs", "because it's the first instance alphabetically");
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

        var tree1 = CSharpSyntaxTree.ParseText(code, path: Path.GetFullPath(Path.Combine("Project1", "Shared.cs")));
        var tree2 = CSharpSyntaxTree.ParseText(code, path: Path.GetFullPath(Path.Combine("Project2", "Shared.cs")));

        var context = CreateWorkspaceContext("Solution", 
            ("Project1", Path.GetFullPath(Path.Combine("Project1", "Project1.csproj"))),
            ("Project2", Path.GetFullPath(Path.Combine("Project2", "Project2.csproj"))));

        // Use low thresholds to ensure this 8-line code is detected
        var options = new DuplicationOptions { MinLines = 5, MinTokens = 10 };
        var detector = new DuplicationDetector(options);

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

    [Fact]
    public void Analyze_ConditionalCompilation_RespectsSymbols()
    {
        // Arrange - Create code with conditional compilation that results in different structures
        var codeWithDebug = """
            public class ConfigClass
            {
                public void Execute()
                {
            #if DEBUG
                    Console.WriteLine("Debug mode");
                    var debugVar = true;
            #else
                    Console.WriteLine("Release mode");
                    var releaseVar = false;
            #endif
                }
            }
            """;

        var codeWithRelease = """
            public class ConfigClass
            {
                public void Execute()
                {
            #if DEBUG
                    Console.WriteLine("Debug mode");
                    var debugVar = true;
            #else
                    Console.WriteLine("Release mode");
                    var releaseVar = false;
            #endif
                }
            }
            """;

        // Parse with different preprocessor symbols
        var parseOptionsDebug = CSharpParseOptions.Default.WithPreprocessorSymbols("DEBUG");
        var parseOptionsRelease = CSharpParseOptions.Default; // No DEBUG symbol

        var tree1 = CSharpSyntaxTree.ParseText(codeWithDebug, parseOptionsDebug, path: "Debug.cs");
        var tree2 = CSharpSyntaxTree.ParseText(codeWithRelease, parseOptionsRelease, path: "Release.cs");

        var context = CreateWorkspaceContext("TestProject");
        var detector = new DuplicationDetector();

        // Act
        var diagnostics = detector.Analyze(new[] { tree1, tree2 }, context).ToList();

        // Assert - Should NOT detect duplication because conditional compilation produces different code
        diagnostics.Should().BeEmpty(
            "because conditional compilation results in different effective code");
    }

    private static WorkspaceContext CreateWorkspaceContext(string solutionName, params (string Name, string Path)[] projects)
    {
        // Note: We only need minimal Project instances for testing DuplicationDetector
        // The actual project structure is not important for duplication detection tests
        var projectList = projects.Select(p =>
        {
            // Create a minimal Project with dummy values
            // In real usage, these would come from ISolutionProvider
            // Ensure paths are absolute and cross-platform
            var absolutePath = Path.IsPathRooted(p.Path) ? p.Path : Path.GetFullPath(p.Path);
            
            return new Project(
                filePath: absolutePath,
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
            filePath: Path.GetFullPath(Path.Combine(solutionName + ".sln")),
            name: solutionName,
            projects: projectList,
            configurations: new[] { "Debug", "Release" });

        var projectsByPath = solution.Projects.ToDictionary(
            p => p.FilePath,
            p => p,
            StringComparer.OrdinalIgnoreCase);

        return new WorkspaceContext(solution, projectsByPath);
    }

    private static WorkspaceContext CreateWorkspaceContext(string projectName)
    {
        return CreateWorkspaceContext("TestSolution", 
            (projectName, Path.GetFullPath(Path.Combine(projectName, $"{projectName}.csproj"))));
    }

    [Fact]
    public void DuplicationDetector_8LineDuplication_MinThreshold10_NoReport()
    {
        // Arrange - Create short duplication (7 lines) with MinLines = 10
        var code1 = """
            public class Class1 {
                public void Method1() {
                    var x = 1;
                    var y = 2;
                    var z = x + y;
                    Console.WriteLine(z);
                }
            """;

        var code2 = """
            public class Class2 {
                public void Method2() {
                    var x = 1;
                    var y = 2;
                    var z = x + y;
                    Console.WriteLine(z);
                }
            """;

        var tree1 = CSharpSyntaxTree.ParseText(code1, path: "C:\\File1.cs");
        var tree2 = CSharpSyntaxTree.ParseText(code2, path: "C:\\File2.cs");
        var context = CreateWorkspaceContext("Project1");

        var options = new DuplicationOptions { MinLines = 10, MinTokens = 50 };
        var detector = new DuplicationDetector(options);

        // Act
        var results = detector.Analyze(new[] { tree1, tree2 }, context).ToList();

        // Assert - 7 lines is below 10-line threshold AND 37 tokens is below 50-token threshold, should not report
        results.Should().BeEmpty();
    }

    [Fact]
    public void DuplicationDetector_ShortTokenDense_MinTokenThreshold_Reported()
    {
        // Arrange - Short code (3 lines) but many tokens (>50)
        var code1 = """
            public class Class1
            {
                public int Calculate() => ((a + b) * (c - d)) / ((e + f) * (g - h)) + ((i + j) * (k - l)) / ((m + n) * (o - p));
            }
            """;

        var code2 = """
            public class Class2
            {
                public int Process() => ((a + b) * (c - d)) / ((e + f) * (g - h)) + ((i + j) * (k - l)) / ((m + n) * (o - p));
            }
            """;

        var tree1 = CSharpSyntaxTree.ParseText(code1, path: "C:\\File1.cs");
        var tree2 = CSharpSyntaxTree.ParseText(code2, path: "C:\\File2.cs");
        var context = CreateWorkspaceContext("Project1");

        var options = new DuplicationOptions { MinLines = 100, MinTokens = 50 }; // High line threshold, low token threshold
        var detector = new DuplicationDetector(options);

        // Act
        var results = detector.Analyze(new[] { tree1, tree2 }, context).ToList();

        // Assert - Should report because token count exceeds MinTokens (even though lines < MinLines)
        results.Should().NotBeEmpty();
        results.Should().ContainSingle(r => r.RuleId == "DUP001");
    }
}
