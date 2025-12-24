namespace Lintelligent.Cli.Infrastructure;

/// <summary>
/// Immutable value object representing the result of a CLI command execution.
/// </summary>
/// <param name="ExitCode">Exit code (0 = success, 1 = general error, 2 = invalid arguments, 3+ = command-specific). Valid range: 0-255.</param>
/// <param name="Output">Standard output content (stdout).</param>
/// <param name="Error">Error output content (stderr). Contains exception.Message only (no stack traces).</param>
/// <remarks>
/// This record enables in-memory testing of CLI commands without process spawning.
/// Use factory methods (Success, Failure) for common scenarios.
/// The primary constructor validates that ExitCode is in the valid OS range (0-255).
/// </remarks>
public sealed record CommandResult(int ExitCode, string Output, string Error)
{
    /// <summary>
    /// Gets the exit code of the command execution.
    /// </summary>
    public int ExitCode { get; init; } = ExitCode >= 0 && ExitCode <= 255 
        ? ExitCode 
        : throw new ArgumentOutOfRangeException(nameof(ExitCode), ExitCode, "Exit code must be between 0 and 255");

    /// <summary>
    /// Gets the standard output content.
    /// </summary>
    public string Output { get; init; } = Output;

    /// <summary>
    /// Gets the error output content.
    /// </summary>
    public string Error { get; init; } = Error;

    /// <summary>
    /// Creates a successful command result with output.
    /// </summary>
    /// <param name="output">Command output to return.</param>
    /// <returns>CommandResult with exit code 0 and no errors.</returns>
    public static CommandResult Success(string output) => new(0, output, string.Empty);

    /// <summary>
    /// Creates a failed command result with error message.
    /// </summary>
    /// <param name="exitCode">Exit code (must be 1-255).</param>
    /// <param name="error">Error message describing the failure.</param>
    /// <returns>CommandResult with specified exit code and error.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if exitCode is not in range 1-255.</exception>
    public static CommandResult Failure(int exitCode, string error)
    {
        if (exitCode < 1 || exitCode > 255)
            throw new ArgumentOutOfRangeException(nameof(exitCode), "Exit code must be between 1 and 255");
        
        return new(exitCode, string.Empty, error);
    }
}
