using FluentAssertions;
using Lintelligent.AnalyzerEngine.WorkspaceAnalyzers.CodeDuplication;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.AnalyzerEngine.Tests.WorkspaceAnalyzers.CodeDuplication;

public class SimilarityDetectorTests
{
    [Fact]
    public void ASTNormalizer_IdenticalControlFlow_DifferentVariables_95PercentSimilar()
    {
        // Arrange - Two code blocks with identical structure but different variable names
        var code1 = """
                    int CalculateTotal(int price, int quantity)
                    {
                        int subtotal = price * quantity;
                        if (subtotal > 100)
                        {
                            subtotal = subtotal - 10;
                        }
                        return subtotal;
                    }
                    """;

        var code2 = """
                    int ComputeSum(int cost, int amount)
                    {
                        int total = cost * amount;
                        if (total > 100)
                        {
                            total = total - 10;
                        }
                        return total;
                    }
                    """;

        SyntaxTree tree1 = CSharpSyntaxTree.ParseText(code1);
        SyntaxTree tree2 = CSharpSyntaxTree.ParseText(code2);

        var normalizer = new AstNormalizer();

        // Act
        SyntaxNode normalized1 = normalizer.NormalizeIdentifiers(tree1.GetRoot());
        SyntaxNode normalized2 = normalizer.NormalizeIdentifiers(tree2.GetRoot());
        var similarity = SimilarityDetector.CalculateSimilarity(normalized1, normalized2);

        // Assert
        similarity.Should().BeGreaterOrEqualTo(95.0, "because the control flow is identical");
    }

    [Fact]
    public void ASTNormalizer_ReorderedStatements_IdentifiedAsSimilar()
    {
        // Arrange - Two code blocks with similar logic but different statement order
        var code1 = """
                    void ProcessData(int x, int y)
                    {
                        int sum = x + y;
                        int product = x * y;
                        Console.WriteLine(sum);
                        Console.WriteLine(product);
                    }
                    """;

        var code2 = """
                    void HandleData(int a, int b)
                    {
                        int product = a * b;
                        int sum = a + b;
                        Console.WriteLine(sum);
                        Console.WriteLine(product);
                    }
                    """;

        SyntaxTree tree1 = CSharpSyntaxTree.ParseText(code1);
        SyntaxTree tree2 = CSharpSyntaxTree.ParseText(code2);

        var normalizer = new AstNormalizer();

        // Act
        SyntaxNode normalized1 = normalizer.NormalizeIdentifiers(tree1.GetRoot());
        SyntaxNode normalized2 = normalizer.NormalizeIdentifiers(tree2.GetRoot());
        var similarity = SimilarityDetector.CalculateSimilarity(normalized1, normalized2);

        // Assert
        similarity.Should().BeGreaterThan(70.0, "because the statements are similar despite reordering");
    }

    [Fact]
    public void SimilarityDetector_85PercentThreshold_OnlyMeetingBlocksReported()
    {
        // Arrange - Three code blocks with varying similarity
        var highSimilarityCode = """
                                 int Calculate(int x)
                                 {
                                     int result = x * 2;
                                     if (result > 10)
                                         result = result - 5;
                                     return result;
                                 }
                                 """;

        var mediumSimilarityCode = """
                                   int Compute(int y)
                                   {
                                       int output = y * 2;
                                       if (output > 10)
                                           output = output - 5;
                                       return output;
                                   }
                                   """;

        var lowSimilarityCode = """
                                int Transform(int z)
                                {
                                    return z * 3 + 100;
                                }
                                """;

        SyntaxTree tree1 = CSharpSyntaxTree.ParseText(highSimilarityCode);
        SyntaxTree tree2 = CSharpSyntaxTree.ParseText(mediumSimilarityCode);
        SyntaxTree tree3 = CSharpSyntaxTree.ParseText(lowSimilarityCode);

        var normalizer = new AstNormalizer();

        // Act
        SyntaxNode normalized1 = normalizer.NormalizeIdentifiers(tree1.GetRoot());
        SyntaxNode normalized2 = normalizer.NormalizeIdentifiers(tree2.GetRoot());
        SyntaxNode normalized3 = normalizer.NormalizeIdentifiers(tree3.GetRoot());

        var similarity12 = SimilarityDetector.CalculateSimilarity(normalized1, normalized2);
        var similarity13 = SimilarityDetector.CalculateSimilarity(normalized1, normalized3);

        // Assert
        similarity12.Should().BeGreaterOrEqualTo(85.0, "because code 1 and 2 are highly similar");
        similarity13.Should().BeLessThan(85.0, "because code 1 and 3 are structurally different");
    }
}