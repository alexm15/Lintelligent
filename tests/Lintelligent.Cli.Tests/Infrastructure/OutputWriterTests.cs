using FluentAssertions;
using Lintelligent.Cli.Infrastructure;
using Xunit;

namespace Lintelligent.Cli.Tests.Infrastructure;

public class OutputWriterTests
{
    private readonly OutputWriter _writer = new();

    [Fact]
    public void Write_WithNullPath_WritesToStdout()
    {
        // Arrange
        var content = "test content";
        TextWriter originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            _writer.Write(content, null);

            // Assert
            var output = stringWriter.ToString();
            output.Should().Contain(content);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Write_WithDashPath_WritesToStdout()
    {
        // Arrange
        var content = "test content";
        TextWriter originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            _writer.Write(content, "-");

            // Assert
            var output = stringWriter.ToString();
            output.Should().Contain(content);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Write_WithValidFilePath_CreatesFile()
    {
        // Arrange
        var content = "test content";
        var tempPath = Path.Combine(Path.GetTempPath(), $"lintelligent_test_{Guid.NewGuid()}.txt");

        try
        {
            // Act
            _writer.Write(content, tempPath);

            // Assert
            File.Exists(tempPath).Should().BeTrue();
            var written = File.ReadAllText(tempPath);
            written.Should().Be(content);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void Write_WithNonExistentDirectory_ThrowsIOException()
    {
        // Arrange
        var content = "test content";
        var invalidPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}", "file.txt");

        // Act & Assert
        Action act = () => _writer.Write(content, invalidPath);
        act.Should().Throw<IOException>()
            .WithMessage("*directory does not exist*");
    }

    [Fact]
    public void Write_WithExistingFile_OverwritesAndWarns()
    {
        // Arrange
        var content = "new content";
        var tempPath = Path.Combine(Path.GetTempPath(), $"lintelligent_test_{Guid.NewGuid()}.txt");
        File.WriteAllText(tempPath, "old content");

        TextWriter originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            _writer.Write(content, tempPath);

            // Assert
            var written = File.ReadAllText(tempPath);
            written.Should().Be(content);

            var consoleOutput = stringWriter.ToString();
            consoleOutput.Should().Contain("Warning");
            consoleOutput.Should().Contain("Overwriting");
        }
        finally
        {
            Console.SetOut(originalOut);
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}