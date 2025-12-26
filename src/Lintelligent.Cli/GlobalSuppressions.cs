// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// Suppress CA1822 for OutputWriter - may need instance state in future for DI/testing
[assembly: SuppressMessage("Design", "CA1822:Mark members as static",
    Justification = "OutputWriter may need instance state for dependency injection and testing",
    Scope = "member",
    Target = "~M:Lintelligent.Cli.Infrastructure.OutputWriter.Write(System.String,System.String)")]

// Suppress CA1822 for FileSystemCodeProvider - may need instance state in future
[assembly: SuppressMessage("Design", "CA1822:Mark members as static",
    Justification = "ParseFile may need instance state for configuration or caching",
    Scope = "member",
    Target =
        "~M:Lintelligent.Cli.Providers.FileSystemCodeProvider.ParseFile(System.String)~Microsoft.CodeAnalysis.SyntaxTree")]
