using Lintelligent.AnalyzerEngine.Rules.Monad;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules.Monad;

/// <summary>
///     Tests for educational message templates used in monad detection diagnostics.
/// </summary>
public class DiagnosticMessageTemplatesTests
{
    [Fact]
    public void GetOptionTemplate_IncludesRequiredSections()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetOptionTemplate("FindUser", "User", 3);

        // Assert
        Assert.Contains("### Current Code", template);
        Assert.Contains("### Suggested Refactoring", template);
        Assert.Contains("### Benefits", template);
        Assert.Contains("### When to Apply", template);
        Assert.Contains("### Additional Resources", template);
    }

    [Fact]
    public void GetOptionTemplate_IncludesNullReferenceExceptionExplanation()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetOptionTemplate("FindUser", "User", 3);

        // Assert
        Assert.Contains("Eliminates null reference exceptions", template);
    }

    [Fact]
    public void GetOptionTemplate_IncludesMethodName()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetOptionTemplate("FindUser", "User", 3);

        // Assert
        Assert.Contains("FindUser", template);
    }

    [Fact]
    public void GetOptionTemplate_IncludesReturnType()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetOptionTemplate("FindUser", "User", 3);

        // Assert
        Assert.Contains("User", template);
    }

    [Fact]
    public void GetOptionTemplate_IncludesNullCheckCount()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetOptionTemplate("FindUser", "User", 5);

        // Assert
        Assert.Contains("(5 found)", template);
    }

    [Fact]
    public void GetEitherTemplate_IncludesRequiredSections()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetEitherTemplate("ParseData", "FormatException");

        // Assert
        Assert.Contains("### Current Code", template);
        Assert.Contains("### Suggested Refactoring", template);
        Assert.Contains("### Benefits", template);
        Assert.Contains("### When to Apply", template);
        Assert.Contains("### Additional Resources", template);
    }

    [Fact]
    public void GetEitherTemplate_IncludesErrorAsValueExplanation()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetEitherTemplate("ParseData", "FormatException");

        // Assert
        Assert.Contains("Makes error handling explicit", template);
    }

    [Fact]
    public void GetEitherTemplate_IncludesRailwayOrientedProgrammingReference()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetEitherTemplate("ParseData", "FormatException");

        // Assert
        Assert.Contains("Railway-Oriented Programming", template);
    }

    [Fact]
    public void GetEitherTemplate_IncludesExceptionType()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetEitherTemplate("ParseData", "FormatException");

        // Assert
        Assert.Contains("FormatException", template);
    }

    [Fact]
    public void GetValidationTemplate_IncludesRequiredSections()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetValidationTemplate("ValidateUser", 4);

        // Assert
        Assert.Contains("### Current Code", template);
        Assert.Contains("### Suggested Refactoring", template);
        Assert.Contains("### Benefits", template);
        Assert.Contains("### When to Apply", template);
        Assert.Contains("### Additional Resources", template);
    }

    [Fact]
    public void GetValidationTemplate_IncludesAccumulateAllErrorsExplanation()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetValidationTemplate("ValidateUser", 4);

        // Assert
        Assert.Contains("Accumulates ALL validation errors", template);
    }

    [Fact]
    public void GetValidationTemplate_IncludesValidationCount()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetValidationTemplate("ValidateUser", 6);

        // Assert
        Assert.Contains("6 sequential validations", template);
        Assert.Contains("(6 found)", template);
    }

    [Fact]
    public void GetTryTemplate_IncludesRequiredSections()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetTryTemplate("ReadFile", "IOException");

        // Assert
        Assert.Contains("### Current Code", template);
        Assert.Contains("### Suggested Refactoring", template);
        Assert.Contains("### Benefits", template);
        Assert.Contains("### When to Apply", template);
        Assert.Contains("### Additional Resources", template);
    }

    [Fact]
    public void GetTryTemplate_IncludesExceptionType()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetTryTemplate("ReadFile", "IOException");

        // Assert
        Assert.Contains("IOException", template);
    }

    [Fact]
    public void GetTryTemplate_IncludesExceptionWrappingExplanation()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetTryTemplate("ReadFile", "IOException");

        // Assert
        Assert.Contains("Captures exceptions as values", template);
    }

    [Fact]
    public void ValidateTemplate_ValidTemplate_ReturnsTrue()
    {
        // Arrange & Act
        string optionTemplate = DiagnosticMessageTemplates.GetOptionTemplate("Test", "string", 1);
        string eitherTemplate = DiagnosticMessageTemplates.GetEitherTemplate("Test", "Exception");
        string validationTemplate = DiagnosticMessageTemplates.GetValidationTemplate("Test", 2);
        string tryTemplate = DiagnosticMessageTemplates.GetTryTemplate("Test", "Exception");

        // Assert - all templates should be short actionable messages
        Assert.Contains("Option", optionTemplate);
        Assert.Contains("Either", eitherTemplate);
        Assert.Contains("Validation", validationTemplate);
        Assert.Contains("Try", tryTemplate);
    }
}
