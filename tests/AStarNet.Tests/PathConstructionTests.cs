namespace AStarNet.Tests;

/// <summary>
/// Tests construction from node identifiers and incoming costs.
/// </summary>
public sealed class PathConstructionTests
{
    /// <summary>
    /// Verifies equal values and hashes across construction routes and independence from the input array.
    /// </summary>
    [Fact]
    public void Constructor_WithEquivalentInputs_ProducesEqualPaths()
    {
        (int NodeId, double CostFromPrevious)[] steps = [(10, 0), (20, 1.25), (30, 2.75)];
        Path fromArray = new(steps);
        Path fromList = new(new List<(int NodeId, double CostFromPrevious)>(steps));
        Path fromIterator = new(steps.Select(step => step));
        Path concatenated = new Path([(10, 0), (20, 1.25)]).Concat(new Path([(20, 0), (30, 2.75)]));
        TestGraph graph = new([10, 20, 30], (10, 20, 1.25), (20, 30, 2.75));
        Path fromSearch = new PathFinder(graph).FindPath(10, 30, TestContext.Current.CancellationToken);

        foreach (Path path in new[] { fromList, fromIterator, concatenated, fromSearch })
        {
            Assert.Equal(fromArray, path);
            Assert.Equal(fromArray.GetHashCode(), path.GetHashCode());
        }

        steps[1] = (99, 100);
        Assert.Equal(20, fromArray.Steps[1].NodeId);
        Assert.Equal(4, fromArray.Cost);
    }

    /// <summary>
    /// Verifies that empty construction agrees with the shared empty result.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyInput_ProducesEmptyPath()
    {
        Path path = new(Array.Empty<(int NodeId, double CostFromPrevious)>());
        Assert.Equal(Path.Empty, path);
        Assert.Equal(Path.Empty.GetHashCode(), path.GetHashCode());
    }

    /// <summary>
    /// Verifies that invalid input costs are rejected.
    /// </summary>
    /// <param name="cost">The invalid incoming cost.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WithInvalidCost_Throws(double cost)
    {
        Assert.Throws<ArgumentException>(() => new Path([(0, 0), (1, cost)]));
    }

    /// <summary>
    /// Verifies null input, the initial cost, and overflow checks.
    /// </summary>
    [Fact]
    public void Constructor_WithInvalidSequence_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Path(null!));
        Assert.Throws<ArgumentException>(() => new Path([(0, 1)]));
        Assert.Throws<InvalidOperationException>(() =>
            new Path([(0, 0), (1, double.MaxValue), (2, double.MaxValue)]));
    }

    /// <summary>
    /// Verifies that list subclasses retain their custom enumeration order.
    /// </summary>
    [Fact]
    public void Constructor_WithDerivedList_UsesCustomEnumerator()
    {
        ReversedSteps steps = new() { (20, 2), (10, 0) };
        Path path = new(steps);
        Assert.Equal([10, 20], path.Steps.Select(step => step.NodeId));
        Assert.Equal(2, path.Cost);
    }

    /// <summary>
    /// Enumerates its stored tuples in reverse order.
    /// </summary>
    private sealed class ReversedSteps : List<(int NodeId, double CostFromPrevious)>,
        IEnumerable<(int NodeId, double CostFromPrevious)>
    {
        /// <inheritdoc/>
        IEnumerator<(int NodeId, double CostFromPrevious)>
            IEnumerable<(int NodeId, double CostFromPrevious)>.GetEnumerator()
        {
            for (int index = this.Count - 1; index >= 0; index--)
                yield return this[index];
        }

        /// <inheritdoc/>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            ((IEnumerable<(int NodeId, double CostFromPrevious)>)this).GetEnumerator();
    }
}
