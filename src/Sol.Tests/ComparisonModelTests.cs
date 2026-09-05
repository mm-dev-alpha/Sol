using Sol.Models;
using Xunit;

namespace Sol.Tests;

public class ComparisonModelTests
{
    [Fact]
    public void GroupDiffSummary_CalculatesCountsAndDifferencesCorrectly()
    {
        var summary = new GroupDiffSummary(
            OnlyInA: ["Group1", "Group2"],
            InBoth: ["Shared1"],
            OnlyInB: ["Group3"]
        );

        Assert.Equal(2, summary.UniqueToACount);
        Assert.Equal(1, summary.SharedCount);
        Assert.Equal(1, summary.UniqueToBCount);
        Assert.Equal(4, summary.TotalGroupCount);
        Assert.True(summary.HasDifferences);
    }

    [Fact]
    public void GroupDiffSummary_IdenticalGroups_HasDifferencesIsFalse()
    {
        var summary = new GroupDiffSummary([], ["Shared1"], []);
        Assert.False(summary.HasDifferences);
        Assert.Equal(0, summary.UniqueToACount);
        Assert.Equal(0, summary.UniqueToBCount);
    }
}
