// Contract: CliApplication API
// Purpose: Defines the public API for executing CLI commands
// Language: C# 12 (.NET 10)

namespace Lintelligent.Cli.Infrastructure;

/// <summary>
/// Represents a configured CLI application that executes commands synchronously.
/// </summary>
/// <remarks>
/// CliApplication is immutable after construction. To change configuration, create a new builder.
/// Execute() can be called multiple times on the same instance (stateless execution).
/// </remarks>
public sealed class CliApplication : IDisposable
{
    // Internal constructor - instances created only via CliApplicationBuilder.Build()
    internal CliApplication(IServiceProvider serviceProvider, IReadOnlyDictionary<string, Type> commands);

    /// <summary>
    /// Executes the CLI command specified in args and returns the result synchronously.
    /// </summary>
    /// <param name="args">Command-line arguments (args[0] is command name, args[1+] are command arguments).</param>
    /// <returns>Command execution result with exit code, output, and error information.</returns>
    /// <remarks>
    /// This method is synchronous even if the command uses async I/O internally.
    /// Exceptions are caught and converted to CommandResult with appropriate exit codes:
    /// - ArgumentException → exit code 2 (invalid arguments)
    /// - Other exceptions → exit code 1 (general error)
    /// </remarks>
    public CommandResult Execute(string[] args);

    /// <summary>
    /// Disposes the service provider and releases resources.
    /// </summary>
    public void Dispose();
}
