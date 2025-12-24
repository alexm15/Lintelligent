// Contract: CliApplicationBuilder API
// Purpose: Defines the public API for building CLI applications
// Language: C# 12 (.NET 10)

namespace Lintelligent.Cli.Infrastructure;

/// <summary>
/// Builder for constructing CLI applications with explicit command and service registration.
/// </summary>
/// <remarks>
/// This builder follows the explicit execution model (Constitutional Principle IV):
/// 1. Create builder
/// 2. Register commands and services
/// 3. Build application
/// 4. Execute command
/// 5. Exit with result code
/// </remarks>
public sealed class CliApplicationBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliApplicationBuilder"/> class.
    /// </summary>
    public CliApplicationBuilder();

    /// <summary>
    /// Registers a command type for execution.
    /// </summary>
    /// <typeparam name="TCommand">Command type implementing ICommand or IAsyncCommand.</typeparam>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// Commands are resolved from the service provider at execution time.
    /// TCommand must be registered in the service collection via ConfigureServices.
    /// </remarks>
    public CliApplicationBuilder AddCommand<TCommand>() where TCommand : class;

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    /// <param name="configure">Action to configure the service collection.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// Services registered here are available for constructor injection in commands.
    /// Example: services.AddSingleton&lt;AnalyzerEngine&gt;();
    /// </remarks>
    public CliApplicationBuilder ConfigureServices(Action<IServiceCollection> configure);

    /// <summary>
    /// Builds the CLI application with registered commands and services.
    /// </summary>
    /// <returns>Configured CLI application ready for execution.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no commands are registered.</exception>
    public CliApplication Build();
}
