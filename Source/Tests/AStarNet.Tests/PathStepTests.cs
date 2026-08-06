namespace AStarNet.Tests;

/// <summary>
/// Tests the complete value semantics of path steps.
/// </summary>
public sealed class PathStepTests
{
    /// <summary>
    /// Verifies equality when every semantic component matches.
    /// </summary>
    [Fact]
    public void Equality_WhenAllComponentsMatch_ReturnsTrue()
    {
        PathStep<string> left = PathStepTests.CreateFinalStep(4, 2, 5);
        PathStep<string> right = PathStepTests.CreateFinalStep(4, 2, 5);

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that every semantic component participates in equality.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="costFromPrevious">The cost from the previous step.</param>
    /// <param name="costFromStart">The accumulated cost.</param>
    [Theory]
    [InlineData(5, 2, 5)]
    [InlineData(4, 3, 5)]
    [InlineData(4, 2, 6)]
    public void Equality_WhenAComponentDiffers_ReturnsFalse(
        int nodeId,
        double costFromPrevious,
        double costFromStart)
    {
        PathStep<string> baseline = PathStepTests.CreateFinalStep(4, 2, 5);
        PathStep<string> different = PathStepTests.CreateFinalStep(nodeId, costFromPrevious, costFromStart);

        Assert.NotEqual(baseline, different);
        Assert.False(baseline == different);
        Assert.True(baseline != different);
    }

    /// <summary>
    /// Creates the final step of a path through the public pathfinding API.
    /// </summary>
    /// <param name="nodeId">The final node identifier.</param>
    /// <param name="costFromPrevious">The cost of the final connection.</param>
    /// <param name="costFromStart">The total path cost.</param>
    /// <returns>The final path step.</returns>
    private static PathStep<string> CreateFinalStep(
        int nodeId,
        double costFromPrevious,
        double costFromStart)
    {
        double precedingCost = costFromStart - costFromPrevious;
        Path<string> path = TestPathFactory.Create(0, (1, precedingCost), (nodeId, costFromPrevious));
        return path.Steps[^1];
    }
}
