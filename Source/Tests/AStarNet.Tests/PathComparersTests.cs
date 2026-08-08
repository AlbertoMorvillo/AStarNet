namespace AStarNet.Tests;

/// <summary>
/// Tests the built-in path ordering strategies.
/// </summary>
public sealed class PathComparersTests
{
    /// <summary>
    /// Verifies that cost ordering prioritizes total cost over node count.
    /// </summary>
    [Fact]
    public void ByCost_OrdersByTotalCost()
    {
        Path cheapLong = TestPathFactory.Create(0, (1, 0.5), (2, 0.5));
        Path expensiveShort = TestPathFactory.Create(0, (2, 2));

        Assert.True(PathComparers.ByCost.Compare(cheapLong, expensiveShort) < 0);
        Assert.True(PathComparers.ByCost.Compare(expensiveShort, cheapLong) > 0);
    }

    /// <summary>
    /// Verifies that node-count ordering prioritizes node count over total cost.
    /// </summary>
    [Fact]
    public void ByNodeCount_OrdersByNumberOfNodes()
    {
        Path cheapLong = TestPathFactory.Create(0, (1, 0.5), (2, 0.5));
        Path expensiveShort = TestPathFactory.Create(0, (2, 2));

        Assert.True(PathComparers.ByNodeCount.Compare(cheapLong, expensiveShort) > 0);
        Assert.True(PathComparers.ByNodeCount.Compare(expensiveShort, cheapLong) < 0);
    }

    /// <summary>
    /// Verifies that cost ordering uses node count when total costs match.
    /// </summary>
    [Fact]
    public void ByCost_WhenCostsMatch_UsesNodeCount()
    {
        Path first = TestPathFactory.Create(0, (1, 1));
        Path second = TestPathFactory.Create(0, (1, 0.5), (2, 0.5));

        Assert.True(PathComparers.ByCost.Compare(first, second) < 0);
        Assert.True(PathComparers.ByCost.Compare(second, first) > 0);
    }

    /// <summary>
    /// Verifies that a zero-cost empty path precedes a zero-cost path containing one node.
    /// </summary>
    [Fact]
    public void ByCost_WhenEmptyAndSingleNodePathsHaveZeroCost_UsesNodeCount()
    {
        Path singleNode = TestPathFactory.Create(0);

        Assert.True(PathComparers.ByCost.Compare(Path.Empty, singleNode) < 0);
        Assert.True(PathComparers.ByCost.Compare(singleNode, Path.Empty) > 0);
    }

    /// <summary>
    /// Verifies that node-count ordering uses total cost when node counts match.
    /// </summary>
    [Fact]
    public void ByNodeCount_WhenNodeCountsMatch_UsesTotalCost()
    {
        Path first = TestPathFactory.Create(0, (1, 1));
        Path second = TestPathFactory.Create(2, (3, 5));

        Assert.True(PathComparers.ByNodeCount.Compare(first, second) < 0);
        Assert.True(PathComparers.ByNodeCount.Compare(second, first) > 0);
    }

    /// <summary>
    /// Verifies that both comparers inspect individual connection costs after their primary criteria match.
    /// </summary>
    /// <param name="comparerName">The comparer to exercise.</param>
    [Theory]
    [InlineData("cost")]
    [InlineData("count")]
    public void Comparer_WhenPrimaryCriteriaMatch_UsesIndividualConnectionCosts(string comparerName)
    {
        IComparer<Path> comparer = PathComparersTests.GetComparer(comparerName);
        Path first = TestPathFactory.Create(0, (1, 1), (2, 2));
        Path second = TestPathFactory.Create(0, (1, 2), (2, 1));

        Assert.True(comparer.Compare(first, second) < 0);
        Assert.True(comparer.Compare(second, first) > 0);
    }

    /// <summary>
    /// Verifies that both comparers use node identifiers as their final deterministic tie-breaker.
    /// </summary>
    /// <param name="comparerName">The comparer to exercise.</param>
    [Theory]
    [InlineData("cost")]
    [InlineData("count")]
    public void Comparer_WhenOnlyNodeIdsDiffer_UsesNodeIdentifiers(string comparerName)
    {
        IComparer<Path> comparer = PathComparersTests.GetComparer(comparerName);
        Path first = TestPathFactory.Create(0, (1, 1));
        Path second = TestPathFactory.Create(2, (3, 1));

        Assert.True(comparer.Compare(first, second) < 0);
        Assert.True(comparer.Compare(second, first) > 0);
    }

    /// <summary>
    /// Verifies that both comparers return zero for completely equal paths.
    /// </summary>
    /// <param name="comparerName">The comparer to exercise.</param>
    [Theory]
    [InlineData("cost")]
    [InlineData("count")]
    public void Comparer_WhenPathsAreEqual_ReturnsZero(string comparerName)
    {
        IComparer<Path> comparer = PathComparersTests.GetComparer(comparerName);
        Path first = TestPathFactory.Create(0, (1, 1), (2, 2));
        Path second = TestPathFactory.Create(0, (1, 1), (2, 2));

        Assert.Equal(first, second);
        Assert.Equal(0, comparer.Compare(first, second));
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
        IComparer<Path> comparer = PathComparersTests.GetComparer(comparerName);
        Path path = Path.Empty;

        Assert.Equal(0, comparer.Compare(null, null));
        Assert.True(comparer.Compare(null, path) < 0);
        Assert.True(comparer.Compare(path, null) > 0);
    }

    /// <summary>
    /// Gets the comparer identified by a test-data value.
    /// </summary>
    /// <param name="comparerName">The comparer identifier.</param>
    /// <returns>The requested path comparer.</returns>
    private static IComparer<Path> GetComparer(string comparerName)
    {
        return comparerName == "cost"
            ? PathComparers.ByCost
            : PathComparers.ByNodeCount;
    }
}
