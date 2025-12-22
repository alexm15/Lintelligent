using Lintelligent.Cli;
using Lintelligent.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(Bootstrapper.Configure)
    .Build();

Console.WriteLine("Lintelligent CLI (NET 10)");
await host.Services
    .GetRequiredService<ScanCommand>()
    .ExecuteAsync(args);


// Graceful shutdown
await host.StopAsync();
