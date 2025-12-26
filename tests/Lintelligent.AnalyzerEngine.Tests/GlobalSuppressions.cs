// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// Test naming convention: Test methods use underscores for readability (MethodName_Scenario_ExpectedOutcome)
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
    Justification = "Test methods use underscores for clarity and readability following common testing conventions",
    Scope = "namespaceanddescendants",
    Target = "~N:Lintelligent.AnalyzerEngine.Tests")]

// Test code doesn't require locale-specific string formatting - invariant culture is acceptable
[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider",
    Justification = "Test code doesn't require culture-specific formatting",
    Scope = "namespaceanddescendants",
    Target = "~N:Lintelligent.AnalyzerEngine.Tests")]