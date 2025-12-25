using Lintelligent.AnalyzerEngine.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lintelligent.Cli.Providers;

/// <summary>
///     Discovers and provides C# source files from the file system for analysis.
/// </summary>
/// <remarks>
///     This implementation:
///     - Recursively discovers all .cs files in a directory
///     - Supports both directory paths and single file paths
///     - Handles file system errors gracefully (logs and skips problematic files)
///     - Uses lazy evaluation (yield) to support large codebases without memory exhaustion
///     Error Handling:
///     - FileNotFoundException: Logged and skipped (file may have been deleted)
///     - UnauthorizedAccessException: Logged and skipped (no permission)
///     - PathTooLongException: Logged and skipped (path exceeds OS limits)
///     - Other IO errors: Logged and skipped
///     Performance:
///     - Uses Directory.EnumerateFiles for lazy file discovery
///     - Yields syntax trees one at a time (streaming)
///     - Suitable for projects with 10,000+ files
/// </remarks>
public class FileSystemCodeProvider : ICodeProvider
{
    private readonly string _rootPath;

    /// <summary>
    ///     Initializes a new instance of FileSystemCodeProvider.
    /// </summary>
    /// <param name="rootPath">
    ///     Root directory path to scan for .cs files, or path to a single .cs file.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if rootPath is null, empty, or whitespace.
    /// </exception>
    public FileSystemCodeProvider(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("Root path cannot be null or empty", nameof(rootPath));

        _rootPath = rootPath;
    }

    /// <summary>
    ///     Discovers C# files and yields parsed syntax trees.
    /// </summary>
    /// <param name="conditionalSymbols">
    ///     Optional list of preprocessor symbols for conditional compilation (e.g., DEBUG, TRACE, RELEASE).
    ///     These symbols determine which #if/#elif/#else blocks are included in the parsed syntax tree.
    /// </param>
    /// <returns>
    ///     Lazy sequence of syntax trees for all .cs files found.
    ///     Empty sequence if no files found or path doesn't exist.
    /// </returns>
    public IEnumerable<SyntaxTree> GetSyntaxTrees(IReadOnlyList<string>? conditionalSymbols = null)
    {
        // Check if root path is a single file
        if (File.Exists(_rootPath))
        {
            var tree = ParseFile(_rootPath, conditionalSymbols);
            if (tree != null) yield return tree;
            yield break;
        }

        // Check if root path is a directory
        if (!Directory.Exists(_rootPath))
        {
            Console.WriteLine($"Warning: Path {_rootPath} does not exist");
            yield break;
        }

        // Recursively discover all .cs files
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(_rootPath, "*.cs", SearchOption.AllDirectories);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            Console.WriteLine($"Error: Cannot enumerate files in {_rootPath}: {ex.Message}");
            yield break;
        }

        // Parse each file and yield syntax tree
        foreach (var file in files)
        {
            var tree = ParseFile(file, conditionalSymbols);
            if (tree != null) yield return tree;
        }
    }

    private static SyntaxTree? ParseFile(string filePath, IReadOnlyList<string>? conditionalSymbols)
    {
        try
        {
            var sourceCode = File.ReadAllText(filePath);
            
            // Create parse options with conditional symbols if provided
            var parseOptions = conditionalSymbols is { Count: > 0 }
                ? new CSharpParseOptions(LanguageVersion.Latest)
                    .WithPreprocessorSymbols(conditionalSymbols)
                : new CSharpParseOptions(LanguageVersion.Latest);
            
            return CSharpSyntaxTree.ParseText(sourceCode, parseOptions, path: filePath);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Warning: File not found (may have been deleted): {filePath}");
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Warning: Access denied: {filePath}");
            return null;
        }
        catch (PathTooLongException)
        {
            Console.WriteLine($"Warning: Path too long: {filePath}");
            return null;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Warning: IO error reading {filePath}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Unexpected error parsing {filePath}: {ex.Message}");
            return null;
        }
    }
}