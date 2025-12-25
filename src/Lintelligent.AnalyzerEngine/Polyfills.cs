// ReSharper disable once CheckNamespace
namespace System;

/// <summary>
/// Polyfills for argument validation methods in netstandard2.0.
/// Also available in net10.0 to ensure compatibility when analyzer (netstandard2.0) is loaded in test context.
/// </summary>
internal static class ArgumentExceptionPolyfills
{
#if NETSTANDARD2_0
    public static void ThrowIfNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }
    }
#else
    public static void ThrowIfNullOrWhiteSpace(string? value, string paramName) 
        => ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
#endif
}

internal static class ArgumentNullExceptionPolyfills
{
#if NETSTANDARD2_0
    public static void ThrowIfNull(object? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
#else
    public static void ThrowIfNull(object? value, string paramName)
        => ArgumentNullException.ThrowIfNull(value, paramName);
#endif
}

internal static class EnumPolyfills
{
#if NETSTANDARD2_0
    public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), value);
    }
#else
    public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum
        => Enum.IsDefined<TEnum>(value);
#endif
}

internal static class MathPolyfills
{
#if NETSTANDARD2_0
    public static int Clamp(int value, int min, int max)
    {
        if (min > max)
            throw new ArgumentException($"min ({min}) must be <= max ({max})", nameof(min));
        
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
#else
    public static int Clamp(int value, int min, int max)
        => Math.Clamp(value, min, max);
#endif
}
