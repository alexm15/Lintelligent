namespace Shared;

/// <summary>
/// This file is LINKED from outside the project directory.
/// Should be included in compilation via <Compile Include="..\Shared\SharedCode.cs" Link="Shared\SharedCode.cs" />
/// </summary>
public class SharedCode
{
    public string GetSharedMessage()
    {
        return "This is shared code";
    }
}
