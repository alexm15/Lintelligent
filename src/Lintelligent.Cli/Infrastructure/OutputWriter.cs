using System.Text;

namespace Lintelligent.Cli.Infrastructure;

/// <summary>
///     Handles writing formatted output to files or stdout.
/// </summary>
/// <remarks>
///     Constitutional Compliance:
///     - I/O operations confined to CLI layer
///     - Formatters remain pure (no side effects)
///     - Testable via in-memory streams
/// </remarks>
public class OutputWriter
{
    /// <summary>
    ///     Writes content to the specified output destination.
    /// </summary>
    /// <param name="content">Formatted content to write.</param>
    /// <param name="outputPath">File path or "-" for stdout. Null means stdout.</param>
    /// <exception cref="IOException">Thrown if file write fails.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if path not writable.</exception>
    public void Write(string content, string? outputPath)
    {
        if (outputPath is null or "-")
        {
            // Write to stdout (FR-010)
            Console.WriteLine(content);
            return;
        }

        // Validate path is writable (FR-009)
        ValidateOutputPath(outputPath);

        // Warn if file exists
        if (File.Exists(outputPath)) Console.WriteLine($"Warning: Overwriting existing file: {outputPath}");

        // Write to file (atomic: temp file → rename)
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            File.Move(tempPath, outputPath, true);
        }
        catch
        {
            // Cleanup temp file on failure
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    private static void ValidateOutputPath(string path)
    {
        // Check if directory exists (not path traversal validation per Clarification #4)
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
#pragma warning disable RS1035
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
#pragma warning restore RS1035
        {
            throw new IOException(
                $"Output directory does not exist: {directory}. " +
                $"Create the directory or specify a valid path.");
        }

        // Check if path is writable (best-effort, actual write may still fail)
        try
        {
            var fullPath = Path.GetFullPath(path);
            // Try to open for write - if file doesn't exist, this creates it
            using var fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write);
        }
        catch (UnauthorizedAccessException)
        {
            throw new IOException($"Output path is read-only or not writable: {path}");
        }
    }
}
