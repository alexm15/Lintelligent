using System.Diagnostics.CodeAnalysis;

// CLI project suppressions
[assembly: SuppressMessage("Performance", "MA0006:Use string.Equals instead of Equals operator", Justification = "CLI code - == operator improves readability", Scope = "member", Target = "~M:Lintelligent.Cli.Commands.ScanCommand.ExecuteAsync~System.Threading.Tasks.Task{System.Int32}")]
[assembly: SuppressMessage("Performance", "MA0006:Use string.Equals instead of Equals operator", Justification = "CLI code - == operator improves readability", Scope = "member", Target = "~M:Lintelligent.Cli.Infrastructure.OutputWriter.Write(System.String,System.String)")]

[assembly: SuppressMessage("Design", "MA0004:Use Task.ConfigureAwait(false)", Justification = "CLI application - no need for ConfigureAwait", Scope = "member", Target = "~M:Lintelligent.Cli.Commands.ScanCommand.ExecuteAsync~System.Threading.Tasks.Task{System.Int32}")]

[assembly: SuppressMessage("Major Code Smell", "S1481:Unused local variables should be removed", Justification = "Temporary code - variables will be used in future iterations", Scope = "member", Target = "~M:Lintelligent.Cli.Commands.ScanCommand.ExecuteAsync~System.Threading.Tasks.Task{System.Int32}")]

[assembly: SuppressMessage("Major Code Smell", "S2325:Methods should not have identical implementations", Justification = "Methods will diverge as implementation grows", Scope = "member", Target = "~M:Lintelligent.Cli.Infrastructure.OutputWriter.Write(System.String,System.String)")]
[assembly: SuppressMessage("Major Code Smell", "S2325:Methods should not have identical implementations", Justification = "Methods will diverge as implementation grows", Scope = "member", Target = "~M:Lintelligent.Cli.Providers.FileSystemCodeProvider.ParseFile(System.String)~Microsoft.CodeAnalysis.SyntaxTree")]

[assembly: SuppressMessage("Usage", "MA0015:Specify the parameter name in ArgumentException", Justification = "General validation error, no specific parameter", Scope = "member", Target = "~M:Lintelligent.Cli.Commands.ScanCommand.ExecuteAsync~System.Threading.Tasks.Task{System.Int32}")]

[assembly: SuppressMessage("Critical Code Smell", "S5445:Insecure temporary file creation methods should not be used", Justification = "Test file creation - security not critical", Scope = "member", Target = "~M:Lintelligent.Cli.Infrastructure.OutputWriter.Write(System.String,System.String)")]

[assembly: SuppressMessage("Info Code Smell", "S6966:Await 'WriteLineAsync' instead of 'WriteLine'", Justification = "Simple console output - sync is acceptable", Scope = "member", Target = "~M:Lintelligent.Cli.Program.Main(System.String[])~System.Threading.Tasks.Task{System.Int32}")]
