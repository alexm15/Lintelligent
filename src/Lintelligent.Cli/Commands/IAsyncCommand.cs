using Lintelligent.Cli.Infrastructure;

namespace Lintelligent.Cli.Commands;

/// <summary>
///     Interface for asynchronous CLI commands.
/// </summary>
/// <remarks>
///     Use this interface for commands that perform async I/O (file reading, HTTP requests, etc.).
///     CliApplication.Execute() will await the result synchronously before returning.
///     No ConfigureAwait(false) needed - console apps have no SynchronizationContext.
/// </remarks>
public interface IAsyncCommand
{
    /// <summary>
    ///     Executes the command asynchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Command-line arguments (args[0] may be command name, args[1+] are parameters).</param>
    /// <returns>Task representing the async operation, with CommandResult as the result.</returns>
    public Task<CommandResult> ExecuteAsync(string[] args);
}