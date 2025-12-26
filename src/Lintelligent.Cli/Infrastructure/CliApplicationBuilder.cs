using Microsoft.Extensions.DependencyInjection;

namespace Lintelligent.Cli.Infrastructure;

/// <summary>
///     Builder for constructing CLI applications with explicit command and service registration.
/// </summary>
/// <remarks>
///     This builder follows the explicit execution model (Constitutional Principle IV):
///     1. Create builder
///     2. Register commands and services
///     3. Build application
///     4. Execute command
///     5. Exit with result code
/// </remarks>
public sealed class CliApplicationBuilder
{
    private readonly List<Type> _commandTypes = [];
    private readonly ServiceCollection _services = [];
    private bool _isBuilt;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CliApplicationBuilder" /> class.
    /// </summary>
    public CliApplicationBuilder()
    {
    }

    /// <summary>
    ///     Registers a command type for execution.
    /// </summary>
    /// <typeparam name="TCommand">Command type implementing ICommand or IAsyncCommand.</typeparam>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    ///     Commands are resolved from the service provider at execution time.
    ///     TCommand must be registered in the service collection via ConfigureServices.
    ///     Last registration wins if the same command type is registered multiple times.
    /// </remarks>
    public CliApplicationBuilder AddCommand<TCommand>() where TCommand : class
    {
        if (_isBuilt)
            throw new InvalidOperationException("Cannot add commands after Build() has been called");

        // Last registration wins (remove previous if exists)
        _commandTypes.RemoveAll(t => t == typeof(TCommand));
        _commandTypes.Add(typeof(TCommand));

        return this;
    }

    /// <summary>
    ///     Configures services for dependency injection.
    /// </summary>
    /// <param name="configure">Action to configure the service collection.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    ///     Services registered here are available for constructor injection in commands.
    ///     Example: services.AddSingleton&lt;AnalyzerEngine&gt;();
    ///     Commands should be registered as Transient to avoid state leakage.
    /// </remarks>
    public CliApplicationBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        if (_isBuilt)
            throw new InvalidOperationException("Cannot configure services after Build() has been called");

        configure(_services);
        return this;
    }

    /// <summary>
    ///     Builds the CLI application with registered commands and services.
    /// </summary>
    /// <returns>Configured CLI application ready for execution.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Build() is called more than once on the same instance.</exception>
    /// <remarks>
    ///     Build() can only be called once per builder instance. Subsequent calls will throw.
    ///     Any exceptions during service provider construction will propagate to the caller.
    /// </remarks>
    public CliApplication Build()
    {
        if (_isBuilt)
            throw new InvalidOperationException("Build() has already been called on this builder instance");

        _isBuilt = true;

        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        return new CliApplication(serviceProvider, _commandTypes);
    }
}
