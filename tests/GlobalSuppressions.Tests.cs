using System.Diagnostics.CodeAnalysis;

// Test code suppressions - these are acceptable in test context
[assembly:
    SuppressMessage("Performance",
        "MA0002:Use an overload that has a IEqualityComparer<string> or IComparer<string> parameter",
        Justification = "Test code - culture-specific comparison not needed")]
[assembly:
    SuppressMessage("Performance", "S6608:Indexing at should be used instead of the Enumerable extension method",
        Justification = "Test code - LINQ methods improve readability")]
[assembly:
    SuppressMessage("Usage", "MA0011:Use an overload that has a IFormatProvider parameter",
        Justification = "Test code - culture-specific formatting not needed")]
[assembly:
    SuppressMessage("Performance", "MA0006:Use string.Equals instead of Equals operator",
        Justification = "Test code - == operator improves readability")]
[assembly:
    SuppressMessage("Performance", "MA0051:Method is too long",
        Justification = "Test methods can be longer for comprehensive test scenarios")]
[assembly:
    SuppressMessage("Major Code Smell", "S1215:GC.Collect should not be called",
        Justification = "Performance tests explicitly test memory behavior")]
[assembly:
    SuppressMessage("Major Code Smell", "S1481:Unused local variables should be removed",
        Justification = "Test code - variables used for testing enumeration behavior")]
[assembly:
    SuppressMessage("Blocker Code Smell", "S1144:Unused private types or members should be removed",
        Justification = "Test helper methods may appear unused")]
[assembly:
    SuppressMessage("Usage", "MA0004:Use Task.ConfigureAwait(false)",
        Justification = "Test code - synchronization context not relevant")]
[assembly:
    SuppressMessage("Performance", "MA0074:Use an overload that has a StringComparison parameter",
        Justification = "Test code - ordinal comparison not critical")]