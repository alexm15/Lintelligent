using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Lintelligent.Analyzers;
using Lintelligent.CodeFixes;

namespace Lintelligent.CodeFixes.Tests;

public class NullableToOptionCodeFixTests
{
    [Fact]
    public async Task CodeFix_SimpleNullableReturn_ConvertsToOption()
    {
        const string testCode = @"
using System;

class TestClass
{
    string? FindUser(int id)
    {
        if (id == 0) return null;
        if (id < 0) return null;
        return ""user"";
    }
}";

        const string fixedCode = @"
using System;
using LanguageExt;

class TestClass
{
    Option<string> FindUser(int id)
    {
        if (id == 0) return Option<string>.None;
        if (id < 0) return Option<string>.None;
        return Option<string>.Some(""user"");
    }
}";

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFix_MultipleNullChecks_ConvertsAllReturns()
    {
        const string testCode = @"
class TestClass
{
    int? Calculate(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;
        if (input.Length < 5) return null;
        if (input.Length > 100) return null;
        return input.Length;
    }
}";

        const string fixedCode = @"using LanguageExt;

class TestClass
{
    Option<int> Calculate(string input)
    {
        if (string.IsNullOrEmpty(input)) return Option<int>.None;
        if (input.Length < 5) return Option<int>.None;
        if (input.Length > 100) return Option<int>.None;
        return Option<int>.Some(input.Length);
    }
}";

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFix_PreservesExistingUsings_DoesNotDuplicate()
    {
        const string testCode = @"
using LanguageExt;
using System;

class TestClass
{
    string? GetValue()
    {
        return null;
    }
}";

        const string fixedCode = @"
using LanguageExt;
using System;

class TestClass
{
    Option<string> GetValue()
    {
        return Option<string>.None;
    }
}";

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    private static async Task VerifyCodeFixAsync(string source, string fixedSource)
    {
        var test = new CSharpCodeFixTest<LintelligentDiagnosticAnalyzer, NullableToOptionCodeFixProvider, XUnitVerifier>
        {
            TestState =
            {
                Sources = { source }
            },
            FixedState =
            {
                Sources = { fixedSource }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await test.RunAsync();
    }
}
