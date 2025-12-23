using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

using Lintelligent.AnalyzerEngine.Analysis;
using Lintelligent.Cli.Commands;
using Lintelligent.Reporting;

namespace Lintelligent.Cli.Tests;

public abstract class TestBase : IAsyncLifetime, IDisposable
{
    // Single host per test (xUnit creates a new instance per test), simpler than tracking a list
    private IHost? _host;
    private bool _disposed;

    /// <summary>
    /// Create a Host using the same defaults as the application.
    /// Tests can pass a configure callback to replace or add registrations (use this to swap live deps).
    /// If <paramref name="useBootstrapper"/> is true, the production <c>Bootstrapper.Configure</c> is applied first.
    /// </summary>
    protected IHost CreateHost(bool useBootstrapper = true, Action<IServiceCollection>? configure = null)
    {
        if (_host is not null)
            throw new InvalidOperationException("A host has already been created for this test. Dispose the test or use a fresh test instance.");

        var builder = Host.CreateDefaultBuilder(Array.Empty<string>());

        builder.ConfigureServices(services =>
        {
            if (useBootstrapper)
            {
                Bootstrapper.Configure(services);
            }
            else
            {
                // Minimal core services (same core surface as Bootstrapper without rules)
                services.AddSingleton<AnalyzerManager>();
                services.AddSingleton<AnalyzerEngine.Analysis.AnalyzerEngine>();
                services.AddSingleton<ReportGenerator>();
                services.AddSingleton<ScanCommand>();
            }

            // Allow tests to swap or replace registrations
            configure?.Invoke(services);
        });

        var host = builder.Build();
        _host = host;
        return host;
    }

    /// <summary>
    /// Convenience method that returns a ServiceCollection pre-configured by Bootstrapper.
    /// Tests can call this to build a service provider outside of a full Host if desired.
    /// </summary>
    protected IServiceCollection ConfigureServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        Bootstrapper.Configure(services);
        configure?.Invoke(services);
        return services;
    }

    /// <summary>
    /// Create a host that replaces an existing registration for <typeparamref name="TService"/> with the given instance.
    /// This is the simple API surface tests should use when they need to swap a live dependency.
    /// </summary>
    protected IHost CreateHostWithOverride<TService>(TService instance) where TService : class
    {
        return CreateHost(true, services =>
        {
            services.RemoveAll<TService>();
            services.AddSingleton<TService>(instance);
        });
    }

    /// <summary>
    /// Create a host that replaces an existing registration for <typeparamref name="TService"/> with the given factory.
    /// The factory receives the built IServiceProvider so you can resolve other dependencies when creating the test double.
    /// </summary>
    protected IHost CreateHostWithFactory<TService>(Func<IServiceProvider, TService> factory) where TService : class
    {
        return CreateHost(true, services =>
        {
            services.RemoveAll<TService>();
            services.AddSingleton<TService>(factory);
        });
    }

    /// <summary>
    /// Build a service provider from the production Bootstrapper configuration allowing the test to build
    /// a provider without spinning up a full Host. Useful for lightweight unit tests.
    /// </summary>
    protected IServiceProvider BuildServiceProvider(Action<IServiceCollection>? configure = null)
    {
        var services = ConfigureServices(configure);
        return services.BuildServiceProvider();
    }

    // xUnit IAsyncLifetime - no-op initialization for derived tests
    public virtual Task InitializeAsync() => Task.CompletedTask;

    // xUnit will call this after each test; perform graceful async host shutdown here.
    public virtual async Task DisposeAsync()
    {
        if (_disposed)
            return;

        if (_host is null)
        {
            _disposed = true;
            return;
        }

        // Try to resolve an ILogger from the host to emit structured errors during teardown.
        // Must do this BEFORE stopping the host
        ILogger<TestBase>? logger = null;
        try
        {
            logger = _host.Services.GetService<ILogger<TestBase>>();
        }
        catch
        {
            // Host may already be disposed
        }

        var timeout = TimeSpan.FromSeconds(5);
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            await _host.StopAsync(cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger is not null)
            {
                logger.LogError(ex, "Host StopAsync failed during test teardown.");
            }
            else
            {
                await Console.Error.WriteLineAsync($"Host StopAsync failed during test teardown: {ex}");
            }
        }

        try
        {
            _host.Dispose();
        }
        catch (Exception ex)
        {
            if (logger is not null)
            {
                logger.LogError(ex, "Host Dispose failed during test teardown.");
            }
            else
            {
                await Console.Error.WriteLineAsync($"Host Dispose failed during test teardown: {ex}");
            }
        }

        _host = null;
        _disposed = true;
    }

    // Implement IDisposable too so external consumers can synchronously dispose the base if needed.
    public void Dispose()
    {
        if (_disposed)
            return;

        // Synchronously wait for async dispose to complete. This is for compatibility with helpers
        // that expect IDisposable; prefer xUnit's async teardown for tests.
        DisposeAsync().GetAwaiter().GetResult();
    }
}