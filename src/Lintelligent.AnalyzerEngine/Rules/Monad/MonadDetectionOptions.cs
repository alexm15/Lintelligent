using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.AnalyzerEngine.Rules.Monad;

/// <summary>
/// Configuration options for language-ext monad detection.
/// Read from .editorconfig via AnalyzerConfigOptionsProvider.
/// </summary>
public sealed record MonadDetectionOptions
{
    private static readonly char[] CommaSeparator = { ',' };
    
    /// <summary>
    /// EditorConfig key: language_ext_monad_detection
    /// </summary>
    public const string EnabledKey = "language_ext_monad_detection";

    /// <summary>
    /// EditorConfig key: language_ext_min_complexity
    /// </summary>
    public const string MinComplexityKey = "language_ext_min_complexity";

    /// <summary>
    /// EditorConfig key: language_ext_enabled_monads
    /// </summary>
    public const string EnabledMonadsKey = "language_ext_enabled_monads";

    /// <summary>
    /// Whether monad detection is enabled globally.
    /// Default: false (opt-in).
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Minimum complexity threshold for triggering diagnostics.
    /// - Option&lt;T&gt;: minimum null checks or null returns
    /// - Validation&lt;T&gt;: minimum sequential validations
    /// Default: 3
    /// </summary>
    public int MinComplexity { get; init; } = 3;

    /// <summary>
    /// Set of monad types enabled for detection.
    /// Default: all types enabled.
    /// </summary>
    public ISet<MonadType> EnabledTypes { get; init; } = new HashSet<MonadType>
    {
        MonadType.Option,
        MonadType.Either,
        MonadType.Validation,
        MonadType.Try
    };

    /// <summary>
    /// Default configuration (all disabled, must opt-in).
    /// </summary>
    public static MonadDetectionOptions Default => new()
    {
        Enabled = false,
        MinComplexity = 3,
        EnabledTypes = new HashSet<MonadType>
        {
            MonadType.Option,
            MonadType.Either,
            MonadType.Validation,
            MonadType.Try
        }
    };

    /// <summary>
    /// Parse configuration from EditorConfig options.
    /// </summary>
    /// <param name="analyzerConfigOptions">Analyzer config for current syntax tree.</param>
    /// <returns>Parsed configuration or default if not specified.</returns>
    public static MonadDetectionOptions Parse(AnalyzerConfigOptions analyzerConfigOptions)
    {
        // Parse enabled flag
        var enabled = false;
        if (analyzerConfigOptions.TryGetValue(EnabledKey, out var enabledValue))
        {
            _ = bool.TryParse(enabledValue, out enabled);
        }

        // Parse min complexity
        var minComplexity = 3;
        if (analyzerConfigOptions.TryGetValue(MinComplexityKey, out var complexityValue) &&
            int.TryParse(complexityValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0)
        {
            minComplexity = parsed;
        }

        // Parse enabled monad types
        var enabledTypes = new HashSet<MonadType>
        {
            MonadType.Option,
            MonadType.Either,
            MonadType.Validation,
            MonadType.Try
        };

        if (analyzerConfigOptions.TryGetValue(EnabledMonadsKey, out var monadsValue))
        {
            enabledTypes.Clear();
            var monadNames = monadsValue.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var monadName in monadNames)
            {
                var trimmed = monadName.Trim().ToLowerInvariant();
                var monadType = trimmed switch
                {
                    "option" => MonadType.Option,
                    "either" => MonadType.Either,
                    "validation" => MonadType.Validation,
                    "try" => MonadType.Try,
                    _ => (MonadType?)null
                };

                if (monadType.HasValue)
                {
                    enabledTypes.Add(monadType.Value);
                }
            }
        }

        return new MonadDetectionOptions
        {
            Enabled = enabled,
            MinComplexity = minComplexity,
            EnabledTypes = enabledTypes
        };
    }

    /// <summary>
    /// Check if a specific monad type is enabled for detection.
    /// </summary>
    public bool IsMonadTypeEnabled(MonadType type) =>
        Enabled && EnabledTypes.Contains(type);
}
