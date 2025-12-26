using Lintelligent.AnalyzerEngine.Rules.Monad;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.Rules.Monad;

/// <summary>
///     Tests for short actionable message templates used in monad detection diagnostics.
/// </summary>
public class DiagnosticMessageTemplatesTests
{
    [Fact]
    public void GetOptionTemplate_IncludesRequiredContent()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetOptionTemplate("FindUser", "User", 3);

        // Assert - Short message format
        Assert.Contains("Option<User>", template);
        Assert.Contains("nullable", template);
        Assert.Contains("null", template);
    }

    [Fact]
    public void GetOptionTemplate_IncludesNullReferenceExceptionExplanation()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetOptionTemplate("FindUser", "User", 3);

        // Assert
        Assert.Contains("null reference exceptions", template);
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
        Assert.Contains("5", template);
        Assert.Contains("null operations", template);
    }

    [Fact]
    public void GetEitherTemplate_IncludesRequiredContent()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetEitherTemplate("ParseData", "FormatException");

        // Assert - Short message format
        Assert.Contains("Either<Error, T>", template);
        Assert.Contains("error handling", template);
    }

    [Fact]
    public void GetEitherTemplate_IncludesErrorAsValueExplanation()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetEitherTemplate("ParseData", "FormatException");

        // Assert
        Assert.Contains("explicit", template);
    }

    [Fact]
    public void GetEitherTemplate_IncludesRailwayOrientedProgrammingReference()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetEitherTemplate("ParseData", "FormatException");

        // Assert
        Assert.Contains("railway-oriented programming", template);
    }

    [Fact]
    public void GetEitherTemplate_IncludesErrorHandling()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetEitherTemplate("ParseData", "FormatException");

        // Assert - Short message doesn't include method/exception names, focuses on pattern
        Assert.Contains("Either<Error, T>", template);
        Assert.Contains("explicit error handling", template);
    }

    [Fact]
    public void GetValidationTemplate_IncludesRequiredContent()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetValidationTemplate("ValidateUser", 4);

        // Assert - Short message format
        Assert.Contains("Validation<Error, T>", template);
        Assert.Contains("accumulate errors", template);
    }

    [Fact]
    public void GetValidationTemplate_IncludesAccumulateAllErrorsExplanation()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetValidationTemplate("ValidateUser", 4);

        // Assert
        Assert.Contains("ALL errors", template);
    }

    [Fact]
    public void GetValidationTemplate_IncludesValidationCount()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetValidationTemplate("ValidateUser", 6);

        // Assert
        Assert.Contains("6", template);
        Assert.Contains("sequential validations", template);
    }

    [Fact]
    public void GetTryTemplate_IncludesRequiredContent()
    {
        // Act
        string template = DiagnosticMessageTemplates.GetTryTemplate("ReadFile", "IOException");

        // Assert - Short message format
        Assert.Contains("Try<T>", template);
        Assert.Contains("exception", template);
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
        Assert.Contains("captured as a value", template);
    }

    [Fact]
    public void AllTemplates_AreShortAndActionable()
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
