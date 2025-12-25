using Lintelligent.Cli;
using Lintelligent.Cli.Commands;
using Lintelligent.Cli.Infrastructure;

#pragma warning disable S6966 // WriteLineAsync not needed for simple console output

// Build CLI application
var builder = new CliApplicationBuilder();

// Configure services (DI)
builder.ConfigureServices(Bootstrapper.Configure);

// Register commands
builder.AddCommand<ScanCommand>();

// Build and execute
using var app = builder.Build();

Console.WriteLine("Lintelligent CLI (NET 10)");
var result = app.Execute(args);

// Output results to console
if (!string.IsNullOrEmpty(result.Output))
    Console.WriteLine(result.Output);

if (!string.IsNullOrEmpty(result.Error))
    Console.Error.WriteLine(result.Error);

// Return exit code to shell
return result.ExitCode;