using FluentAssertions;
using Lintelligent.AnalyzerEngine.Configuration;
using Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.WorkspaceAnalyzers.CodeDuplication;

public class SubBlockDuplicationTests
{
    [Fact]
    public void FindDuplicates_ValidationLogicInTwoMethods_DetectsDuplication()
    {
        // Arrange - Email validation duplicated in CreateUser and UpdateEmail
        var code = """
            public class UserService
            {
                public void CreateUser(string email, string name)
                {
                    // Setup
                    var user = new User { Name = name };
                    
                    // DUPLICATED validation block
                    if (string.IsNullOrEmpty(email))
                        throw new ArgumentException("Email required");
                    if (!email.Contains("@"))
                        throw new ArgumentException("Invalid email");
                    if (email.Length > 255)
                        throw new ArgumentException("Email too long");
                    
                    // Continue with user creation
                    user.Email = email;
                    SaveUser(user);
                }
                
                public void UpdateEmail(int userId, string email)
                {
                    // Different setup
                    var user = GetUser(userId);
                    
                    // SAME validation block (exact duplicate)
                    if (string.IsNullOrEmpty(email))
                        throw new ArgumentException("Email required");
                    if (!email.Contains("@"))
                        throw new ArgumentException("Invalid email");
                    if (email.Length > 255)
                        throw new ArgumentException("Email too long");
                    
                    // Different finalization
                    user.Email = email;
                    UpdateUser(user);
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code, path: "UserService.cs");
        var options = new DuplicationOptions(minLines: 3, minTokens: 10);

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(new[] { tree }, options).ToList();

        // Assert
        var validationDup = groups.FirstOrDefault(g => 
            g.Instances.Count == 2 && 
            g.Instances.Any(i => i.ProjectName.Contains("CreateUser")) &&
            g.Instances.Any(i => i.ProjectName.Contains("UpdateEmail")) &&
            g.LineCount == 6); // 6 statements: 3 if + 3 throw
            
        validationDup.Should().NotBeNull("because the validation block (6 statements) appears in both methods");
        validationDup!.LineCount.Should().Be(6, "because the validation is 6 statements (3 if + 3 throw)");
    }

    [Fact]
    public void FindDuplicates_ErrorHandlingInDifferentMethods_DetectsDuplication()
    {
        // Arrange - Same error handling in GetUser and GetOrder
        var code = """
            public class ApiClient
            {
                public User GetUser(int id)
                {
                    try
                    {
                        return _client.Get($"/users/{id}");
                    }
                    catch (HttpException ex)
                    {
                        // DUPLICATED error handling
                        _logger.LogError(ex, "API call failed");
                        if (ex.StatusCode == 404) 
                            return null;
                        if (ex.StatusCode >= 500)
                            throw new ServiceUnavailableException();
                        throw new ApiException("Request failed", ex);
                    }
                }
                
                public Order GetOrder(int id)
                {
                    try
                    {
                        return _client.Get($"/orders/{id}");
                    }
                    catch (HttpException ex)
                    {
                        // SAME error handling (exact duplicate)
                        _logger.LogError(ex, "API call failed");
                        if (ex.StatusCode == 404) 
                            return null;
                        if (ex.StatusCode >= 500)
                            throw new ServiceUnavailableException();
                        throw new ApiException("Request failed", ex);
                    }
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code, path: "ApiClient.cs");
        var options = new DuplicationOptions(minLines: 4, minTokens: 15);

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(new[] { tree }, options).ToList();

        // Assert - The catch block body (statements inside catch) should be detected
        var errorHandlingDup = groups.FirstOrDefault(g => 
            g.Instances.Count == 2 && 
            g.Instances.Any(i => i.SourceText.Contains("_logger.LogError")));
            
        errorHandlingDup.Should().NotBeNull("because the error handling statements are duplicated in both catch blocks");
        errorHandlingDup!.Instances.Should().HaveCount(2, "because it appears in GetUser and GetOrder");
    }

    [Fact]
    public void FindDuplicates_TransactionPatternAcrossMethods_DetectsDuplication()
    {
        // Arrange - Transaction boilerplate in SaveOrder, UpdateOrder, DeleteOrder
        var code = """
            public class OrderRepository
            {
                public void SaveOrder(Order order)
                {
                    // DUPLICATED transaction pattern
                    var transaction = _db.BeginTransaction();
                    try
                    {
                        _db.Orders.Add(order);
                        _db.SaveChanges();
                        transaction.Commit();
                        _logger.LogInfo("Transaction completed");
                    }
                    catch
                    {
                        transaction.Rollback();
                        _logger.LogError("Transaction failed");
                        throw;
                    }
                }
                
                public void UpdateOrder(Order order)
                {
                    // SAME transaction pattern
                    var transaction = _db.BeginTransaction();
                    try
                    {
                        _db.Orders.Update(order);
                        _db.SaveChanges();
                        transaction.Commit();
                        _logger.LogInfo("Transaction completed");
                    }
                    catch
                    {
                        transaction.Rollback();
                        _logger.LogError("Transaction failed");
                        throw;
                    }
                }
                
                public void DeleteOrder(int id)
                {
                    // SAME transaction pattern again
                    var transaction = _db.BeginTransaction();
                    try
                    {
                        _db.Orders.Remove(id);
                        _db.SaveChanges();
                        transaction.Commit();
                        _logger.LogInfo("Transaction completed");
                    }
                    catch
                    {
                        transaction.Rollback();
                        _logger.LogError("Transaction failed");
                        throw;
                    }
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code, path: "OrderRepository.cs");
        var options = new DuplicationOptions(minLines: 3, minTokens: 10);

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(new[] { tree }, options).ToList();

        // Assert - Should find duplicated catch block with rollback pattern
        var transactionDup = groups.FirstOrDefault(g => 
            g.Instances.Count == 3 && 
            g.Instances.Any(i => i.SourceText.Contains("Rollback")));
            
        transactionDup.Should().NotBeNull("because the rollback pattern appears in all three methods' catch blocks");
        transactionDup!.Instances.Should().HaveCount(3);
    }

    [Fact]
    public void FindDuplicates_NoSubBlockDuplication_ReturnsEmpty()
    {
        // Arrange - Methods with unique code
        var code = """
            public class Calculator
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }
                
                public int Multiply(int a, int b)
                {
                    return a * b;
                }
                
                public int Divide(int a, int b)
                {
                    if (b == 0) throw new DivideByZeroException();
                    return a / b;
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code, path: "Calculator.cs");
        var options = new DuplicationOptions(minLines: 3, minTokens: 10);

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(new[] { tree }, options).ToList();

        // Assert - May find some duplications from overlapping sequences, but not the whole methods
        groups.Should().BeEmpty("because all methods have unique implementations");
    }
    
    [Fact]
    public void FindDuplicates_ShortSequence_NotDetected()
    {
        // Arrange - Only 2-statement "duplications" (below threshold)
        var code = """
            public class Service
            {
                public void Method1()
                {
                    var x = 1;
                    var y = 2;
                }
                
                public void Method2()
                {
                    var x = 1;
                    var y = 2;
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code, path: "Service.cs");
        var options = new DuplicationOptions(minLines: 3, minTokens: 10);

        // Act
        var groups = ExactDuplicationFinder.FindDuplicates(new[] { tree }, options).ToList();

        // Assert - Should not detect because we need minimum 3 statements
        groups.Should().BeEmpty("because the duplicated sequence is only 2 statements (below minimum of 3)");
    }
}
