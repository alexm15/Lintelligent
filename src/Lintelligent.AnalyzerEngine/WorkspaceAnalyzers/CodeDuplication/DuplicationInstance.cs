using Microsoft.CodeAnalysis.Text;

namespace Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
///     Represents a single occurrence of duplicated code.
/// </summary>
/// <remarks>
///     Immutable record type for thread-safety and determinism.
///     Used to track individual instances that are grouped into DuplicationGroup.
/// </remarks>
/// <param name="FilePath">Absolute path to the file containing the duplication</param>
/// <param name="ProjectName">Name of the project containing this instance</param>
/// <param name="Location">Start and end line/column positions in the file</param>
/// <param name="TokenCount">Number of tokens in the duplicated block</param>
/// <param name="Hash">Rolling hash value computed from token stream</param>
/// <param name="SourceText">Actual duplicated code (for reporting purposes)</param>
public record DuplicationInstance(
    string FilePath,
    string ProjectName,
    LinePositionSpan Location,
    int TokenCount,
    ulong Hash,
    string SourceText);
