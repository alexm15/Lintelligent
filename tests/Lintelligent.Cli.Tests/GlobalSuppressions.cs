// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// Test naming convention: Test methods use underscores for readability (MethodName_Scenario_ExpectedOutcome)
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
    Justification = "Test methods use underscores for clarity and readability following common testing conventions",
    Scope = "namespaceanddescendants",
    Target = "~N:Lintelligent.Cli.Tests")]

// Test helper classes don't need to be sealed
[assembly: SuppressMessage("Performance", "CA1852:Seal internal types",
    Justification = "Test helper classes may be inherited in future or left unsealed for testing flexibility",
    Scope = "namespaceanddescendants",
    Target = "~N:Lintelligent.Cli.Tests")]

// Test Dispose methods don't need GC.SuppressFinalize since test classes don't have finalizers
[assembly: SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize",
    Justification = "Test classes don't have finalizers, SuppressFinalize is unnecessary",
    Scope = "namespaceanddescendants",
    Target = "~N:Lintelligent.Cli.Tests")]