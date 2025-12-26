namespace Lintelligent.AnalyzerEngine.ProjectModel;

/// <summary>
///     Represents a source file included in project compilation.
/// </summary>
public sealed class CompileItem
{
    public CompileItem(string filePath, CompileItemInclusionType inclusionType, string? originalIncludePath = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!IsAbsolutePath(filePath))
            throw new ArgumentException("File path must be absolute.", nameof(filePath));

        if (!Enum.IsDefined(typeof(CompileItemInclusionType), inclusionType))
            throw new ArgumentException("Invalid inclusion type.", nameof(inclusionType));

        // Validation: DefaultGlob should have null OriginalIncludePath
        if (inclusionType == CompileItemInclusionType.DefaultGlob && originalIncludePath != null)
            throw new ArgumentException("DefaultGlob items should not have OriginalIncludePath.",
                nameof(originalIncludePath));

        // Validation: ExplicitInclude and LinkedFile should have non-null OriginalIncludePath
        if (inclusionType is CompileItemInclusionType.ExplicitInclude or CompileItemInclusionType.LinkedFile
            && string.IsNullOrWhiteSpace(originalIncludePath))
            throw new ArgumentException($"{inclusionType} items must have OriginalIncludePath.",
                nameof(originalIncludePath));

        FilePath = filePath;
        InclusionType = inclusionType;
        OriginalIncludePath = originalIncludePath;
    }

    /// <summary>
    ///     Absolute path to the source file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    ///     Indicates how this file was included in the project.
    /// </summary>
    public CompileItemInclusionType InclusionType { get; }

    /// <summary>
    ///     Original path as specified in project file (may be relative or outside project).
    ///     Null for files included via default glob patterns.
    /// </summary>
    public string? OriginalIncludePath { get; }

    private static bool IsAbsolutePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Path.IsPathRooted returns true for:
        // - Windows: C:\path, \\network\path
        // - Unix: /path
        // But also for relative paths like \path on Windows
        // So we need additional checks

        if (!Path.IsPathRooted(path))
            return false;

        // On Windows, reject paths that are just "\" or "/" (rooted but not absolute)
        // On Unix, "/" is the root and is absolute
        var root = Path.GetPathRoot(path);
        if (root == null)
            return false;

        // Windows: root should be like "C:\" or "\\network\share", not just "\"
        // Unix: root is "/", which is valid
        if (Path.DirectorySeparatorChar == '\\')
        {
            // Windows
            return root.Length > 1 || root.Equals("\\\\\\\\", StringComparison.Ordinal);
        }

        // Unix
        return root.Equals("/", StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return $"{InclusionType}: {FilePath}";
    }
}
