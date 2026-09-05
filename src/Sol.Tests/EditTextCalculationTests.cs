using Sol.Models;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class EditTextCalculationTests
{
    [Fact]
    public void EvaluateExpressions_BasicMath_ReturnsCorrectOutputs()
    {
        using var service = new EditTextService();
        string input = "10 + 20\r\n100 - 45\r\n12 * 4\r\n50 / 2";

        CalculationResult result = service.EvaluateExpressions(input);

        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(4, result.EvaluatedCount);
        Assert.Equal("30", result.LineOutputs[0]);
        Assert.Equal("55", result.LineOutputs[1]);
        Assert.Equal("48", result.LineOutputs[2]);
        Assert.Equal("25", result.LineOutputs[3]);
        Assert.Equal(158, result.Sum);
        Assert.Equal(39.5, result.Average);
    }

    [Fact]
    public void EvaluateExpressions_VariablesAndContinuations_WorksCorrectly()
    {
        using var service = new EditTextService();
        string input = "x = 10\r\ny = 25\r\ntotal = x + y\r\ntax = total * 0.2";

        CalculationResult result = service.EvaluateExpressions(input);

        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(4, result.EvaluatedCount);
        Assert.Equal("10", result.LineOutputs[0]);
        Assert.Equal("25", result.LineOutputs[1]);
        Assert.Equal("35", result.LineOutputs[2]);
        Assert.Equal("7", result.LineOutputs[3]);
        Assert.Equal(77, result.Sum);
    }

    [Fact]
    public void EvaluateExpressions_CommentsAndEmptyLines_ArePreserved()
    {
        using var service = new EditTextService();
        string input = "5 * 5\r\n// This is a comment\r\n# Another comment\r\n\r\n10 * 10";

        CalculationResult result = service.EvaluateExpressions(input);

        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(2, result.EvaluatedCount);
        Assert.Equal(5, result.LineOutputs.Count);
        Assert.Equal("25", result.LineOutputs[0]);
        Assert.Equal("", result.LineOutputs[1]);
        Assert.Equal("", result.LineOutputs[2]);
        Assert.Equal("", result.LineOutputs[3]);
        Assert.Equal("100", result.LineOutputs[4]);
        Assert.Equal(125, result.Sum);
        Assert.Equal(62.5, result.Average);
    }

    [Fact]
    public void EvaluateExpressions_SyntaxErrors_ReportsErrorCountWithoutThrowing()
    {
        using var service = new EditTextService();
        string input = "10 + 20\r\nthis is invalid syntax !!!\r\n5 * 2";

        CalculationResult result = service.EvaluateExpressions(input);

        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(2, result.EvaluatedCount);
        Assert.Equal("30", result.LineOutputs[0]);
        Assert.Equal("Error", result.LineOutputs[1]);
        Assert.Equal("10", result.LineOutputs[2]);
        Assert.Equal(40, result.Sum);
    }
}
