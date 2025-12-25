using Lintelligent.Cli.Infrastructure;

namespace Lintelligent.Cli.Commands;

/// <summary>
///     Interface for synchronous CLI commands.
/// </summary>
/// <remarks>
///     Commands must be stateless and deterministic where possible.
///     Use this interface for commands that complete synchronously or don't require async I/O.
///     For async commands, implement IAsyncCommand instead.
/// </remarks>
public interface ICommand
{
    /// <summary>
    ///     Executes the command with the specified arguments.
    /// </summary>
    /// <param name="args">Command-line arguments (args[0] may be command name, args[1+] are parameters).</param>
    /// <returns>Command execution result.</returns>
    public CommandResult Execute(string[] args);
}