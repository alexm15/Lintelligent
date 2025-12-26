namespace Lintelligent.AnalyzerEngine.ProjectModel;

/// <summary>
///     Represents a .NET target framework moniker (TFM).
/// </summary>
public sealed class TargetFramework : IEquatable<TargetFramework>
{
    public TargetFramework(string moniker)
    {
        if (string.IsNullOrWhiteSpace(moniker))
            throw new ArgumentException("Target framework moniker cannot be null or empty.", nameof(moniker));

        Moniker = moniker;
        (FrameworkFamily, Version) = ParseMoniker(moniker);
    }

    /// <summary>
    ///     Short-form TFM (e.g., net8.0, net472, netstandard2.0).
    /// </summary>
    public string Moniker { get; }

    /// <summary>
    ///     Framework family (e.g., .NETCoreApp, .NETFramework, .NETStandard).
    ///     Derived from moniker.
    /// </summary>
    public string FrameworkFamily { get; }

    /// <summary>
    ///     Framework version (e.g., 8.0, 4.7.2, 2.0).
    ///     Derived from moniker.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///     Indicates if this is a .NET Core/.NET 5+ framework.
    /// </summary>
    public bool IsModernDotNet => string.Equals(FrameworkFamily, ".NETCoreApp", StringComparison.Ordinal) ||
                                  (Moniker.StartsWith("net", StringComparison.OrdinalIgnoreCase) &&
                                   !Moniker.StartsWith("net4", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Indicates if this is .NET Framework (net45, net472, etc.).
    /// </summary>
    public bool IsNetFramework => string.Equals(FrameworkFamily, ".NETFramework", StringComparison.Ordinal) ||
                                  Moniker.StartsWith("net4", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Indicates if this is .NET Standard.
    /// </summary>
    public bool IsNetStandard => string.Equals(FrameworkFamily, ".NETStandard", StringComparison.Ordinal);

    private static (string Family, string Version) ParseMoniker(string moniker)
    {
        // netstandard2.0 -> .NETStandard, 2.0
        if (moniker.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
        {
            var version = moniker.Substring("netstandard".Length);
            return (".NETStandard", version);
        }

        // net472 -> .NETFramework, 4.7.2
        if (moniker.StartsWith("net4", StringComparison.OrdinalIgnoreCase))
        {
            var versionPart = moniker.Substring("net".Length);
            var version = versionPart.Length switch
            {
                2 => $"{versionPart[0]}.{versionPart[1]}", // net45 -> 4.5
                3 => $"{versionPart[0]}.{versionPart[1]}.{versionPart[2]}", // net472 -> 4.7.2
                _ => versionPart
            };
            return (".NETFramework", version);
        }

        // net5.0, net6.0, net7.0, net8.0, net9.0, net10.0 -> .NETCoreApp, 5.0/6.0/etc
        if (moniker.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            var version = moniker.Substring("net".Length);
            return (".NETCoreApp", version);
        }

        // Unknown format - return as-is
        return (moniker, string.Empty);
    }

    public bool Equals(TargetFramework? other)
    {
        return other is not null && (ReferenceEquals(this, other) ||
                              string.Equals(Moniker, other.Moniker, StringComparison.OrdinalIgnoreCase));
    }

    public override bool Equals(object? obj)
    {
        return obj is TargetFramework other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Moniker);
    }

    public override string ToString()
    {
        return Moniker;
    }

    public static bool operator ==(TargetFramework? left, TargetFramework? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TargetFramework? left, TargetFramework? right)
    {
        return !Equals(left, right);
    }
}
