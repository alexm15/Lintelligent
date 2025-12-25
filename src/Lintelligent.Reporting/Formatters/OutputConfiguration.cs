namespace Lintelligent.Reporting.Formatters;

/// <summary>
/// Configuration for output destination and formatting options.
/// </summary>
/// <remarks>
/// This model is immutable (record type) for thread-safety and determinism.
/// </remarks>
public record OutputConfiguration
{
    /// <summary>
    /// Output format (json, sarif, markdown).
    /// </summary>
    public required string Format { get; init; }
    
    /// <summary>
    /// Output path (file path or "-" for stdout). Null means stdout (default).
    /// </summary>
    public string? OutputPath { get; init; }
    
    /// <summary>
    /// Whether to use ANSI color codes (auto-detected or user-specified).
    /// Only applicable to Markdown format.
    /// </summary>
    public bool EnableColor { get; init; } = true;
    
    /// <summary>
    /// Validates configuration rules (e.g., markdown format with color support).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Format))
        {
            throw new ArgumentException("Format cannot be null or whitespace", nameof(Format));
        }
        
        var validFormats = new[] { "json", "sarif", "markdown" };
        if (!validFormats.Contains(Format.ToLowerInvariant()))
        {
            throw new ArgumentException(
                $"Invalid format '{Format}'. Valid formats: {string.Join(", ", validFormats)}", 
                nameof(Format));
        }
    }
}
