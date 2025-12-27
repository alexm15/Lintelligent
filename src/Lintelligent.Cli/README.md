# Lintelligent.Cli

Command-line interface for Lintelligent: static code analysis for C#/.NET projects.

## Features

- **Scan Projects, Solutions, or Files**: Analyze C# codebases for code quality, maintainability, and duplication issues
- **Severity Filtering**: Show only errors, warnings, or info (`--severity Error,Warning`)
- **Grouping**: Organize results by category (`--group-by category`)
- **Output Formats**: Console (default), JSON (via Reporting)
- **Performance**: Streaming, memory-efficient for large repos
- **Cross-Platform**: Runs on Windows, macOS, Linux (.NET 10.0)
- **Extensible**: Add new commands or output formats

## Main Command

- `scan` — Analyze a project, solution, or file
	- Options:
		- `--severity <levels>`: Filter by severity (Error, Warning, Info)
		- `--group-by <field>`: Group results (e.g., by category)
		- `--output <format>`: Output format (console, json)

## Example Usage

Analyze a project:
```bash
dotnet run -- scan /path/to/project
```

Filter by severity:
```bash
dotnet run -- scan /path/to/project --severity Error,Warning
```

Group by category:
```bash
dotnet run -- scan /path/to/project --group-by category
```

Output as JSON:
```bash
dotnet run -- scan /path/to/project --output json
```

## Architecture

- **Commands**: Implement `ICommand` or `IAsyncCommand` (see `Commands/ScanCommand.cs`)
- **Infrastructure**: Use `CliApplicationBuilder` to configure and run the CLI
- **Providers**: File system and build-based code providers (see `Providers/`)

## Packaging

- Distributed as a .NET global tool (`dotnet tool install -g Lintelligent.Cli`)
- Tool command: `lintelligent`
- NuGet: [Lintelligent.Cli](https://www.nuget.org/packages/Lintelligent.Cli)

## Project Info

- **Target Framework:** .NET 10.0
- **License:** MIT
- **Repository:** https://github.com/alexm15/Lintelligent

## Directory Structure

```
Lintelligent.Cli/
	Program.cs
	Bootstrapper.cs
	Commands/
		ScanCommand.cs
		ICommand.cs
		IAsyncCommand.cs
	Infrastructure/
		CliApplication.cs
		CliApplicationBuilder.cs
		CommandResult.cs
		OutputWriter.cs
	Providers/
		FileSystemCodeProvider.cs
		BuildalyzerProjectProvider.cs
		BuildalyzerSolutionProvider.cs
```

---

For more, see the main [Lintelligent README](../../README.md).
