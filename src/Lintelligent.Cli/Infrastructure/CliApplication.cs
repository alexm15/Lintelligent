using Lintelligent.Cli.Commands;

namespace Lintelligent.Cli.Infrastructure;

/// <summary>
///     Represents a configured CLI application that executes commands synchronously.
/// </summary>
/// <remarks>
///     CliApplication is immutable after construction. To change configuration, create a new builder.
///     Execute() can be called multiple times on the same instance (stateless execution).
///     The service provider is disposed when CliApplication is disposed.
/// </remarks>
public sealed class CliApplication : IDisposable
{
    private readonly IReadOnlyList<Type> _commandTypes;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    // Internal constructor - instances created only via CliApplicationBuilder.Build()
    internal CliApplication(IServiceProvider serviceProvider, IReadOnlyList<Type> commandTypes)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _commandTypes = commandTypes ?? throw new ArgumentNullException(nameof(commandTypes));
    }

    /// <summary>
    ///     Disposes the service provider and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_serviceProvider is IDisposable disposable) disposable.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    ///     Executes the CLI command specified in args and returns the result synchronously.
    /// </summary>
    /// <param name="args">Command-line arguments (args[0] is command name, args[1+] are command arguments).</param>
    /// <returns>Command execution result with exit code, output, and error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown if args is null.</exception>
    /// <remarks>
    ///     This method is synchronous even if the command uses async I/O internally.
    ///     Exceptions are caught and converted to CommandResult with appropriate exit codes:
    ///     - ArgumentException and derived types → exit code 2 (invalid arguments)
    ///     - Other exceptions → exit code 1 (general error)
    ///     - DI resolution failures → exit code 1 (general error)
    ///     Exception.Message is stored in CommandResult.Error (no stack traces).
    /// </remarks>
    public CommandResult Execute(string[] args)
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));

        // Handle empty args[] - no command specified
        if (args.Length == 0 || _commandTypes.Count == 0)
            return CommandResult.Failure(2, "No command specified");

        try
        {
            var commandName = args[0].ToLowerInvariant();

            // Find command type by name (simple name matching)
            var commandType = _commandTypes.FirstOrDefault(t =>
                t.Name.Equals($"{commandName}Command", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

            if (commandType == null)
                return CommandResult.Failure(2, $"Unknown command: {commandName}");

            // Resolve command instance from DI
            var commandInstance = _serviceProvider.GetService(commandType);
            if (commandInstance == null)
                return CommandResult.Failure(1, $"Failed to resolve command: {commandType.Name}");

            // Execute command (async or sync)
            if (commandInstance is IAsyncCommand asyncCommand)
                // GetAwaiter().GetResult() unwraps AggregateException automatically
                return asyncCommand.ExecuteAsync(args).GetAwaiter().GetResult();

            if (commandInstance is ICommand syncCommand) return syncCommand.Execute(args);

            return CommandResult.Failure(1, $"Command {commandType.Name} does not implement ICommand or IAsyncCommand");
        }
        catch (ArgumentException ex)
        {
            // ArgumentException and derived types → exit code 2
            return CommandResult.Failure(2, ex.Message);
        }
        catch (Exception ex)
        {
            // All other exceptions → exit code 1
            return CommandResult.Failure(1, ex.Message);
        }
    }
}