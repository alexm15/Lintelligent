using FluentAssertions;
using Lintelligent.AnalyzerEngine.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.WorkspaceAnalyzers.CodeDuplication;

/// <summary>
/// Tests for TokenHasher utility (Rabin-Karp rolling hash).
/// </summary>
public class TokenHasherTests
{
    [Fact]
    public void HashTokens_IdenticalCode_ProducesSameHash()
    {
        // Arrange
        var code1 = "void Method() { Console.WriteLine(\"Hello\"); }";
        var code2 = "void Method() { Console.WriteLine(\"Hello\"); }";
        
        var tree1 = CSharpSyntaxTree.ParseText(code1);
        var tree2 = CSharpSyntaxTree.ParseText(code2);
        
        var tokens1 = TokenHasher.ExtractTokens(tree1);
        var tokens2 = TokenHasher.ExtractTokens(tree2);
        
        // Act
        var hash1 = TokenHasher.HashTokens(tokens1);
        var hash2 = TokenHasher.HashTokens(tokens2);
        
        // Assert
        hash1.Should().Be(hash2, "identical code should produce identical hashes");
        hash1.Should().NotBe(0UL, "hash should not be zero for non-empty code");
    }
    
    [Fact]
    public void HashTokens_WhitespaceOnlyDifferences_ProducesSameHash()
    {
        // Arrange - same code with different whitespace/indentation
        var code1 = "void Method() { Console.WriteLine(\"Hello\"); }";
        var code2 = @"void Method()
{
    Console.WriteLine(""Hello"");
}";
        
        var tree1 = CSharpSyntaxTree.ParseText(code1);
        var tree2 = CSharpSyntaxTree.ParseText(code2);
        
        var tokens1 = TokenHasher.ExtractTokens(tree1);
        var tokens2 = TokenHasher.ExtractTokens(tree2);
        
        // Act
        var hash1 = TokenHasher.HashTokens(tokens1);
        var hash2 = TokenHasher.HashTokens(tokens2);
        
        // Assert
        hash1.Should().Be(hash2, "whitespace differences should be normalized");
    }
    
    [Fact]
    public void HashTokens_DifferentCode_ProducesDifferentHash()
    {
        // Arrange - genuinely different code structure
        var code1 = "void Method() { Console.WriteLine(\"Hello\"); }";
        var code2 = "int Calculate() { return 42; }";  // Different return type, body
        
        var tree1 = CSharpSyntaxTree.ParseText(code1);
        var tree2 = CSharpSyntaxTree.ParseText(code2);
        
        var tokens1 = TokenHasher.ExtractTokens(tree1);
        var tokens2 = TokenHasher.ExtractTokens(tree2);
        
        // Act
        var hash1 = TokenHasher.HashTokens(tokens1);
        var hash2 = TokenHasher.HashTokens(tokens2);
        
        // Assert
        hash1.Should().NotBe(hash2, "different code structure should produce different hashes");
    }
    
    [Fact]
    public void HashTokens_CommentsIgnored_ProducesSameHash()
    {
        // Arrange - same code with different comments
        var code1 = @"void Method() 
{
    // Comment 1
    Console.WriteLine(""Hello"");
}";
        var code2 = @"void Method() 
{
    // Different comment
    Console.WriteLine(""Hello"");
}";
        
        var tree1 = CSharpSyntaxTree.ParseText(code1);
        var tree2 = CSharpSyntaxTree.ParseText(code2);
        
        var tokens1 = TokenHasher.ExtractTokens(tree1);
        var tokens2 = TokenHasher.ExtractTokens(tree2);
        
        // Act
        var hash1 = TokenHasher.HashTokens(tokens1);
        var hash2 = TokenHasher.HashTokens(tokens2);
        
        // Assert
        hash1.Should().Be(hash2, "comments should be excluded from hash");
    }
    
    [Fact]
    public void ExtractTokens_ValidTree_ReturnsTokensInOrder()
    {
        // Arrange
        var code = "int x = 42;";
        var tree = CSharpSyntaxTree.ParseText(code);
        
        // Act
        var tokens = TokenHasher.ExtractTokens(tree).ToList();
        
        // Assert
        tokens.Should().NotBeEmpty();
        tokens.Select(t => t.Text).Should().ContainInOrder("int", "x", "=", "42", ";");
    }
    
    [Fact]
    public void ExtractTokens_NullTree_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => TokenHasher.ExtractTokens(null!);
        act.Should().Throw<ArgumentNullException>();
    }
    
    [Fact]
    public void HashTokens_NullTokens_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => TokenHasher.HashTokens(null!);
        act.Should().Throw<ArgumentNullException>();
    }
    
    [Fact]
    public void HashTokens_EmptyTokens_ReturnsZero()
    {
        // Arrange
        var emptyTokens = Enumerable.Empty<Microsoft.CodeAnalysis.SyntaxToken>();
        
        // Act
        var hash = TokenHasher.HashTokens(emptyTokens);
        
        // Assert
        hash.Should().Be(0UL, "empty token sequence should produce zero hash");
    }
    
    [Fact]
    public void HashTree_ConvenienceMethod_MatchesManualHash()
    {
        // Arrange
        var code = "void Method() { Console.WriteLine(\"Test\"); }";
        var tree = CSharpSyntaxTree.ParseText(code);
        
        var tokens = TokenHasher.ExtractTokens(tree);
        var manualHash = TokenHasher.HashTokens(tokens);
        
        // Act
        var convenienceHash = TokenHasher.HashTree(tree);
        
        // Assert
        convenienceHash.Should().Be(manualHash, "HashTree should match manual ExtractTokens + HashTokens");
    }
}
