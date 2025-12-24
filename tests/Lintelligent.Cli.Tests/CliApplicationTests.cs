using Lintelligent.Cli.Commands;
using Lintelligent.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lintelligent.Cli.Tests;

public class CliApplicationTests
{
    [Fact]
    public void Build_ReturnsValidCliApplication()
    {
        // Arrange
        var builder = new CliApplicationBuilder();
        builder.ConfigureServices(services => { services.AddTransient<TestCommand>(); });
        builder.AddCommand<TestCommand>();

        // Act
        using var app = builder.Build();

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public void Execute_WithValidCommand_ReturnsExitCode0()
    {
        // Arrange
        var builder = new CliApplicationBuilder();
        builder.ConfigureServices(services => { services.AddTransient<TestCommand>(); });
        builder.AddCommand<TestCommand>();
        using var app = builder.Build();

        // Act
        var result = app.Execute(["test", "arg1"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Test output", result.Output);
        Assert.Empty(result.Error);
    }

    [Fact]
    public void Execute_WithArgumentException_ReturnsExitCode2()
    {
        // Arrange
        var builder = new CliApplicationBuilder();
        builder.ConfigureServices(services => { services.AddTransient<ThrowingCommand>(); });
        builder.AddCommand<ThrowingCommand>();
        using var app = builder.Build();

        // Act
        var result = app.Execute(["throwing", "invalid"]);

        // Assert
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Invalid argument", result.Error);
        Assert.Empty(result.Output);
    }

    [Fact]
    public void Execute_WithGeneralException_ReturnsExitCode1()
    {
        // Arrange
        var builder = new CliApplicationBuilder();
        builder.ConfigureServices(services => { services.AddTransient<GeneralErrorCommand>(); });
        builder.AddCommand<GeneralErrorCommand>();
        using var app = builder.Build();

        // Act
        var result = app.Execute(["generalerror"]);

        // Assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("General error", result.Error);
        Assert.Empty(result.Output);
    }

    [Fact]
    public void Execute_WithUnknownCommand_ReturnsExitCode2()
    {
        // Arrange
        var builder = new CliApplicationBuilder();
        builder.ConfigureServices(services => { });
        using var app = builder.Build();

        // Act
        var result = app.Execute(["nonexistent"]);

        // Assert
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("No command specified", result.Error);
    }

    [Fact]
    public void Execute_WithEmptyArgs_ReturnsExitCode2()
    {
        // Arrange
        var builder = new CliApplicationBuilder();
        using var app = builder.Build();

        // Act
        var result = app.Execute([]);

        // Assert
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("No command specified", result.Error);
    }

    [Fact]
    public void Build_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CliApplicationBuilder();
        builder.Build().Dispose();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void CommandResult_WithInvalidExitCode_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert - exit code too high
        Assert.Throws<ArgumentOutOfRangeException>(() => new CommandResult(256, "", ""));

        // Act & Assert - exit code negative
        Assert.Throws<ArgumentOutOfRangeException>(() => new CommandResult(-1, "", ""));
    }

    [Fact]
    public void CommandResult_Failure_WithInvalidExitCode_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert - exit code 0 not allowed for Failure
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandResult.Failure(0, "error"));

        // Act & Assert - exit code too high
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandResult.Failure(256, "error"));
    }

    // Test helper commands
    private class TestCommand : ICommand
    {
        public CommandResult Execute(string[] args)
        {
            return CommandResult.Success("Test output");
        }
    }

    private class ThrowingCommand : ICommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length > 1 && args[1] == "invalid")
                throw new ArgumentException("Invalid argument");
            return CommandResult.Success("OK");
        }
    }

    private class GeneralErrorCommand : ICommand
    {
        public CommandResult Execute(string[] args)
        {
            throw new InvalidOperationException("General error");
        }
    }
}