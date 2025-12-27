# Lintelligent.Reporting

Flexible reporting and output formatting for Lintelligent static analysis results.

## Features

- **Multiple Output Formats:**
	- Console (human-readable, default)
	- JSON (machine-readable, for integrations)
- **Extensible Formatters:**
	- Implement `IReportFormatter` to add custom formats
- **Output Configuration:**
	- Control verbosity, grouping, and output destination via `OutputConfiguration`
- **Integration:**
	- Used by Lintelligent CLI and can be embedded in other tools

## Formatters

- `ConsoleFormatter`: Pretty-prints analysis results to the terminal
- `JsonFormatter`: Outputs results as structured JSON (see `Models/JsonOutputModel.cs`)

## Usage Example

In the CLI or your own tool:

```csharp
var generator = new ReportGenerator();
generator.GenerateReport(results, new ConsoleFormatter(), config);
```

To output as JSON:

```csharp
generator.GenerateReport(results, new JsonFormatter(), config);
```

## Extending

To add a new output format, implement the `IReportFormatter` interface in `Formatters/` and register it with your application.

## Project Info

- **Target Framework:** .NET 10.0
- **License:** MIT
- **NuGet:** [Lintelligent.Reporting](https://www.nuget.org/packages/Lintelligent.Reporting)
- **Repository:** https://github.com/alexm15/Lintelligent

## Directory Structure

```
Lintelligent.Reporting/
	ReportGenerator.cs
	Formatters/
		ConsoleFormatter.cs
		JsonFormatter.cs
		IReportFormatter.cs
		OutputConfiguration.cs
		Models/
			JsonOutputModel.cs
```

---

For more, see the main [Lintelligent README](../../README.md).
