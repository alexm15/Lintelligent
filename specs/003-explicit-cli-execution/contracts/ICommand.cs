// Contract: ICommand Interface
// Purpose: Defines the contract for synchronous CLI commands
// Language: C# 12 (.NET 10)

namespace Lintelligent.Cli.Commands;

/// <summary>
/// Interface for synchronous CLI commands.
/// </summary>
/// <remarks>
/// Commands must be stateless and deterministic where possible.
/// Use this interface for commands that complete synchronously or don't require async I/O.
/// For async commands, implement IAsyncCommand instead.
/// </remarks>
public interface ICommand
{
    /// <summary>
    /// Executes the command with the specified arguments.
    /// </summary>
    /// <param name="args">Command-line arguments (args[0] may be command name, args[1+] are parameters).</param>
    /// <returns>Command execution result.</returns>
    CommandResult Execute(string[] args);
}

/// <summary>
/// Interface for asynchronous CLI commands.
/// </summary>
/// <remarks>
/// Use this interface for commands that perform async I/O (file reading, HTTP requests, etc.).
/// CliApplication.Execute() will await the result synchronously before returning.
/// </remarks>
public interface IAsyncCommand
{
    /// <summary>
    /// Executes the command asynchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Command-line arguments (args[0] may be command name, args[1+] are parameters).</param>
    /// <returns>Task representing the async operation, with command execution result.</returns>
    Task<CommandResult> ExecuteAsync(string[] args);
}
