namespace Lintelligent.AnalyzerEngine.ProjectModel;

/// <summary>
/// How a compile item was included in the project.
/// </summary>
public enum CompileItemInclusionType
{
    /// <summary>
    /// Included via default SDK glob pattern (**/*.cs).
    /// </summary>
    DefaultGlob,

    /// <summary>
    /// Explicitly included via &lt;Compile Include="..." /&gt;.
    /// </summary>
    ExplicitInclude,

    /// <summary>
    /// Linked file from outside project directory.
    /// </summary>
    LinkedFile
}
