namespace MultiTargetProject;

public class FrameworkSpecificCode
{
#if NET8_0_OR_GREATER
    // This method should only be analyzed when targeting .NET 8.0
    public string ModernMethod()
    {
        // Use modern C# features
        return "This is .NET 8.0!";
    }
#endif

#if NET472
    // This method should only be analyzed when targeting .NET Framework 4.7.2
    public string LegacyMethod()
    {
        return "This is .NET Framework 4.7.2!";
    }
#endif

    // This method is always compiled
    public int Calculate(int a, int b)
    {
        return a + b;
    }
}
