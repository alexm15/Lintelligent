// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// Test naming convention: Test methods use underscores for readability (MethodName_Scenario_ExpectedOutcome)
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
    Justification = "Test methods use underscores for clarity and readability following common testing conventions",
    Scope = "namespaceanddescendants",
    Target = "~N:Lintelligent.Analyzers.Tests")]

// Test code doesn't require locale-specific string formatting - invariant culture is acceptable
[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider",
    Justification = "Test code doesn't require culture-specific formatting",
    Scope = "namespaceanddescendants",
    Target = "~N:Lintelligent.Analyzers.Tests")]

// Test helper code uses generic Exception for simplicity
[assembly: SuppressMessage("Design", "CA2201:Do not raise reserved exception types",
    Justification = "Test helper code uses generic Exception for demonstration purposes",
    Scope = "namespaceanddescendants",
    Target = "~N:Lintelligent.Analyzers.Tests")]

// Test code performance optimizations (CA1860) are less critical than readability
[assembly: SuppressMessage("Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method",
    Justification = "Test code prioritizes readability over micro-optimizations",
    Scope = "namespaceanddescendants",
    Target = "~N:Lintelligent.Analyzers.Tests")]
