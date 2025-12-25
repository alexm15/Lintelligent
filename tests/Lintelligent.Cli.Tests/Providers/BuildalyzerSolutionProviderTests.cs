using FluentAssertions;
using Lintelligent.Cli.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lintelligent.Cli.Tests.Providers;

public sealed class BuildalyzerSolutionProviderTests
{
    private readonly BuildalyzerSolutionProvider _provider;

    public BuildalyzerSolutionProviderTests()
    {
        _provider = new BuildalyzerSolutionProvider(NullLogger<BuildalyzerSolutionProvider>.Instance);
    }

    [Fact]
    public async Task ParseSolutionAsync_ValidSolution_ReturnsAllProjects()
    {
        // Arrange
        var solutionPath = Path.GetFullPath(Path.Combine("Fixtures", "TestSolution.sln"));

        // Act
        var solution = await _provider.ParseSolutionAsync(solutionPath);

        // Assert
        solution.Should().NotBeNull();
        solution.Name.Should().Be("TestSolution");
        solution.FilePath.Should().Be(solutionPath);
        solution.Projects.Should().HaveCount(3);
        solution.Projects.Should().Contain(p => p.Name == "ProjectA");
        solution.Projects.Should().Contain(p => p.Name == "ProjectB");
        solution.Projects.Should().Contain(p => p.Name == "ProjectC");
        solution.Configurations.Should().Contain("Debug|Any CPU");
        solution.Configurations.Should().Contain("Release|Any CPU");
    }

    [Fact]
    public async Task ParseSolutionAsync_ValidSolution_ProjectPathsAreAbsolute()
    {
        // Arrange
        var solutionPath = Path.GetFullPath(Path.Combine("Fixtures", "TestSolution.sln"));

        // Act
        var solution = await _provider.ParseSolutionAsync(solutionPath);

        // Assert
        solution.Projects.Should().AllSatisfy(p =>
        {
            Path.IsPathRooted(p.FilePath).Should().BeTrue();
            p.FilePath.Should().EndWith(".csproj");
        });
    }

    [Fact]
    public async Task ParseSolutionAsync_MissingSolution_ThrowsFileNotFoundException()
    {
        // Arrange
        var solutionPath = "NonExistent.sln";

        // Act
        var act = async () => await _provider.ParseSolutionAsync(solutionPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*NonExistent.sln*");
    }

    [Fact]
    public async Task ParseSolutionAsync_NullPath_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _provider.ParseSolutionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("solutionPath");
    }

    [Fact]
    public async Task ParseSolutionAsync_EmptyPath_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _provider.ParseSolutionAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("solutionPath");
    }

    [Fact]
    public async Task ParseSolutionAsync_MalformedSolution_ThrowsInvalidOperationException()
    {
        // Arrange - Create a malformed solution file
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "This is not a valid solution file content");

            // Act
            var act = async () => await _provider.ParseSolutionAsync(tempFile);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Solution file is malformed:*");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
