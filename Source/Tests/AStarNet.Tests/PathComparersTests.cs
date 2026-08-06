using AStarNet.Utils;

namespace AStarNet.Tests;

/// <summary>
/// Tests the built-in path ordering strategies.
/// </summary>
public sealed class PathComparersTests
{
    /// <summary>
    /// Verifies that cost ordering ignores node count.
    /// </summary>
    [Fact]
    public void ByCost_OrdersByTotalCost()
    {
        Path<string> cheapLong = TestPathFactory.Create(0, (1, 0.5), (2, 0.5));
        Path<string> expensiveShort = TestPathFactory.Create(0, (2, 2));

        Assert.True(PathComparers<string>.ByCost.Compare(cheapLong, expensiveShort) < 0);
        Assert.True(PathComparers<string>.ByCost.Compare(expensiveShort, cheapLong) > 0);
    }

    /// <summary>
    /// Verifies that node-count ordering ignores total cost.
    /// </summary>
    [Fact]
    public void ByNodeCount_OrdersByNumberOfNodes()
    {
        Path<string> cheapLong = TestPathFactory.Create(0, (1, 0.5), (2, 0.5));
        Path<string> expensiveShort = TestPathFactory.Create(0, (2, 2));

        Assert.True(PathComparers<string>.ByNodeCount.Compare(cheapLong, expensiveShort) > 0);
        Assert.True(PathComparers<string>.ByNodeCount.Compare(expensiveShort, cheapLong) < 0);
    }

    /// <summary>
    /// Verifies conventional null ordering for both built-in comparers.
    /// </summary>
    /// <param name="comparerName">The comparer to exercise.</param>
    [Theory]
    [InlineData("cost")]
    [InlineData("count")]
    public void Comparer_WhenAPathIsNull_UsesConventionalNullOrdering(string comparerName)
    {
        IComparer<Path<string>> comparer = comparerName == "cost"
            ? PathComparers<string>.ByCost
            : PathComparers<string>.ByNodeCount;
        Path<string> path = Path<string>.Empty;

        Assert.Equal(0, comparer.Compare(null, null));
        Assert.True(comparer.Compare(null, path) < 0);
        Assert.True(comparer.Compare(path, null) > 0);
    }
}
