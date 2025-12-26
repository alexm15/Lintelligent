using FluentAssertions;
using Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
/// Tests for ExactDuplicationFinder - validates two-pass duplication detection algorithm.
/// </summary>
public sealed class ExactDuplicationFinderTests
{
    [Fact]
    public void FindDuplicates_TwoIdenticalMethods_ReturnsOneDuplicationGroup()
    {
        // Arrange
        var code1 = """
            public class Class1
            {
                public void Calculate()
                {
                    int x = 10;
                    int y = 20;
                    int sum = x + y;
                    Console.WriteLine(sum);
                }
            }
            """;

        var code2 = """
            public class Class2
            {
                public void Calculate()
                {
                    int x = 10;
                    int y = 20;
                    int sum = x + y;
                    Console.WriteLine(sum);
                }
            }
            """;

        var tree1 = CSharpSyntaxTree.ParseText(code1, path: "File1.cs");
        var tree2 = CSharpSyntaxTree.ParseText(code2, path: "File2.cs");
        var trees = new[] { tree1, tree2 };

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(trees).ToList();

        // Assert
        groups.Should().HaveCount(1, "because there is one set of duplicated methods");

        var group = groups[0];
        group.Instances.Should().HaveCount(2, "because the method appears in two files");
        group.Instances[0].FilePath.Should().Be("File1.cs");
        group.Instances[1].FilePath.Should().Be("File2.cs");
        group.TokenCount.Should().BeGreaterThan(0, "because the duplicated code has tokens");
        group.LineCount.Should().BeGreaterThan(0, "because the duplicated code spans multiple lines");
    }

    [Fact]
    public void FindDuplicates_ThreeIdenticalClasses_GroupsAllThreeInstances()
    {
        // Arrange
        var code1 = """
            public class UserService
            {
                private readonly IRepository _repo;

                public UserService(IRepository repo)
                {
                    _repo = repo;
                }

                public User GetById(int id)
                {
                    return _repo.Find(id);
                }
            }
            """;

        var code2 = """
            public class UserService
            {
                private readonly IRepository _repo;

                public UserService(IRepository repo)
                {
                    _repo = repo;
                }

                public User GetById(int id)
                {
                    return _repo.Find(id);
                }
            }
            """;

        var code3 = """
            public class UserService
            {
                private readonly IRepository _repo;

                public UserService(IRepository repo)
                {
                    _repo = repo;
                }

                public User GetById(int id)
                {
                    return _repo.Find(id);
                }
            }
            """;

        var tree1 = CSharpSyntaxTree.ParseText(code1, path: "Project1/UserService.cs");
        var tree2 = CSharpSyntaxTree.ParseText(code2, path: "Project2/UserService.cs");
        var tree3 = CSharpSyntaxTree.ParseText(code3, path: "Project3/UserService.cs");
        var trees = new[] { tree1, tree2, tree3 };

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(trees).ToList();

        // Assert
        groups.Should().HaveCount(1, "because all three classes are identical");

        var group = groups[0];
        group.Instances.Should().HaveCount(3, "because the class appears in three files");
        group.Instances.Select(i => i.FilePath).Should().Contain(new[]
        {
            "Project1/UserService.cs",
            "Project2/UserService.cs",
            "Project3/UserService.cs"
        });

        // Verify severity score increases with more instances
        var expectedScore = group.Instances.Count * group.LineCount;
        group.GetSeverityScore().Should().Be(expectedScore,
            "because severity = instances × lines");
    }

    [Fact]
    public void FindDuplicates_NoMatchingCode_ReturnsEmpty()
    {
        // Arrange
        var code1 = "public class A { void Method1() { } }";
        var code2 = "public class B { int Calculate() { return 42; } }";

        var tree1 = CSharpSyntaxTree.ParseText(code1, path: "A.cs");
        var tree2 = CSharpSyntaxTree.ParseText(code2, path: "B.cs");
        var trees = new[] { tree1, tree2 };

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(trees).ToList();

        // Assert
        groups.Should().BeEmpty("because there are no duplications");
    }

    [Fact]
    public void FindDuplicates_NullTrees_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ExactDuplicationFinder.FindDuplicates(null!).ToList();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("trees");
    }

    [Fact]
    public void FindDuplicates_EmptyTreeList_ReturnsEmpty()
    {
        // Arrange
        var trees = Array.Empty<SyntaxTree>();

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(trees).ToList();

        // Assert
        groups.Should().BeEmpty("because there are no trees to analyze");
    }

    [Fact]
    public void FindDuplicates_SingleTree_ReturnsEmpty()
    {
        // Arrange
        var code = """
            public class Single
            {
                void Method() { }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code, path: "Single.cs");

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(new[] { tree }).ToList();

        // Assert
        groups.Should().BeEmpty("because duplications require at least 2 instances");
    }
}
