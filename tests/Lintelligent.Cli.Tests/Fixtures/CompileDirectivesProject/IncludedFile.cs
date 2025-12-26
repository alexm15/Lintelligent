namespace CompileDirectivesProject;

/// <summary>
///     This is a normal file in the project directory.
///     Should be included by SDK-style default glob pattern (**/*.cs).
/// </summary>
public class IncludedFile
{
    public string GetMessage()
    {
        return "This file is included";
    }
}