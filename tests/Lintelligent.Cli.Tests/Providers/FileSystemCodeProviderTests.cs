using Xunit;
using Lintelligent.Cli.Providers;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace Lintelligent.Cli.Tests.Providers;

public class FileSystemCodeProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly List<string> _createdFiles = [];

    public FileSystemCodeProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"Lintelligent_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private string CreateTestFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, content);
        _createdFiles.Add(fullPath);
        return fullPath;
    }

    [Fact]
    public void Constructor_NullPath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new FileSystemCodeProvider(null!));
        exception.ParamName.Should().Be("rootPath");
    }

    [Fact]
    public void Constructor_EmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new FileSystemCodeProvider(string.Empty));
        exception.ParamName.Should().Be("rootPath");
    }

    [Fact]
    public void Constructor_WhitespacePath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new FileSystemCodeProvider("   "));
        exception.ParamName.Should().Be("rootPath");
    }

    [Fact]
    public void GetSyntaxTrees_DirectoryWithCsFiles_ReturnsAllFiles()
    {
        // Arrange
        CreateTestFile("File1.cs", "class Class1 { }");
        CreateTestFile("File2.cs", "class Class2 { }");
        CreateTestFile("Subfolder/File3.cs", "class Class3 { }");

        var provider = new FileSystemCodeProvider(_tempDir);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().HaveCount(3);
        trees.Should().OnlyContain(tree => tree != null);
        trees.Should().OnlyContain(tree => !string.IsNullOrEmpty(tree.FilePath));
        
        var filePaths = trees.Select(t => t.FilePath).ToList();
        filePaths.Should().Contain(path => path.EndsWith("File1.cs"));
        filePaths.Should().Contain(path => path.EndsWith("File2.cs"));
        filePaths.Should().Contain(path => path.EndsWith("File3.cs"));
    }

    [Fact]
    public void GetSyntaxTrees_SingleFile_ReturnsSingleTree()
    {
        // Arrange
        var filePath = CreateTestFile("Single.cs", "class SingleClass { }");
        var provider = new FileSystemCodeProvider(filePath);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().ContainSingle();
        trees[0].FilePath.Should().Be(filePath);
        trees[0].ToString().Should().Contain("SingleClass");
    }

    [Fact]
    public void GetSyntaxTrees_EmptyDirectory_ReturnsEmptyCollection()
    {
        // Arrange - _tempDir exists but has no .cs files
        var provider = new FileSystemCodeProvider(_tempDir);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().BeEmpty();
    }

    [Fact]
    public void GetSyntaxTrees_DirectoryWithMixedFiles_ReturnsOnlyCsFiles()
    {
        // Arrange
        CreateTestFile("Code.cs", "class Code { }");
        CreateTestFile("ReadMe.txt", "This is a text file");
        CreateTestFile("Data.json", "{\"key\": \"value\"}");
        CreateTestFile("Script.js", "console.log('hello');");
        CreateTestFile("Config.xml", "<config></config>");

        var provider = new FileSystemCodeProvider(_tempDir);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().ContainSingle();
        trees[0].FilePath.Should().EndWith("Code.cs");
    }

    [Fact]
    public void GetSyntaxTrees_NonExistentDirectory_ReturnsEmptyCollection()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "NonExistent");
        var provider = new FileSystemCodeProvider(nonExistentPath);

        // Redirect console output to capture warnings
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            var trees = provider.GetSyntaxTrees().ToList();

            // Assert
            trees.Should().BeEmpty();
            
            Console.SetOut(originalOut);
            var output = sw.ToString();
            output.Should().Contain("Warning");
            output.Should().Contain("does not exist");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void GetSyntaxTrees_NonExistentFile_ReturnsEmptyCollection()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_tempDir, "NonExistent.cs");
        var provider = new FileSystemCodeProvider(nonExistentFile);

        // Redirect console output to capture warnings
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            var trees = provider.GetSyntaxTrees().ToList();

            // Assert
            trees.Should().BeEmpty();
            
            Console.SetOut(originalOut);
            var output = sw.ToString();
            output.Should().Contain("Warning");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void GetSyntaxTrees_SyntaxTreesHaveValidFilePaths()
    {
        // Arrange
        CreateTestFile("Test.cs", "class Test { }");
        var provider = new FileSystemCodeProvider(_tempDir);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().ContainSingle();
        trees[0].FilePath.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(trees[0].FilePath).Should().BeTrue();
    }

    [Fact]
    public void GetSyntaxTrees_LazyEvaluation_DoesNotEnumerateUntilIterated()
    {
        // Arrange
        CreateTestFile("File1.cs", "class Class1 { }");
        CreateTestFile("File2.cs", "class Class2 { }");
        
        var provider = new FileSystemCodeProvider(_tempDir);

        // Act - Call GetSyntaxTrees but don't iterate
        var enumerable = provider.GetSyntaxTrees();

        // Assert - This should not throw even if we haven't materialized
        enumerable.Should().NotBeNull();
        
        // Now materialize and verify
        var trees = enumerable.ToList();
        trees.Should().HaveCount(2);
    }

    [Fact]
    public void GetSyntaxTrees_MultipleEnumerations_ReturnsFreshResults()
    {
        // Arrange
        CreateTestFile("File1.cs", "class Class1 { }");
        var provider = new FileSystemCodeProvider(_tempDir);

        // Act - Enumerate twice
        var trees1 = provider.GetSyntaxTrees().ToList();
        var trees2 = provider.GetSyntaxTrees().ToList();

        // Assert - Both enumerations should produce valid results
        trees1.Should().ContainSingle();
        trees2.Should().ContainSingle();
        
        // The trees should be separate instances (not cached)
        trees1[0].Should().NotBeSameAs(trees2[0]);
    }

    [Fact]
    public void GetSyntaxTrees_DeepNestedDirectory_RecoversAllFiles()
    {
        // Arrange
        CreateTestFile("Level1/Level2/Level3/Deep.cs", "class Deep { }");
        CreateTestFile("Root.cs", "class Root { }");

        var provider = new FileSystemCodeProvider(_tempDir);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().HaveCount(2);
        trees.Should().Contain(tree => tree.FilePath.EndsWith("Deep.cs"));
        trees.Should().Contain(tree => tree.FilePath.EndsWith("Root.cs"));
    }

    [Fact]
    public void GetSyntaxTrees_ValidCSharpCode_ParsesCorrectly()
    {
        // Arrange
        var code = """

                   using System;

                   namespace TestNamespace
                   {
                       public class TestClass
                       {
                           public void TestMethod()
                           {
                               Console.WriteLine("Hello");
                           }
                       }
                   }
                   """;
        CreateTestFile("Valid.cs", code);
        var provider = new FileSystemCodeProvider(_tempDir);

        // Act
        var trees = provider.GetSyntaxTrees().ToList();

        // Assert
        trees.Should().ContainSingle();
        var tree = trees[0];
        tree.GetRoot().Should().NotBeNull();
        tree.ToString().Should().Contain("TestClass");
        tree.ToString().Should().Contain("TestMethod");
    }
}
