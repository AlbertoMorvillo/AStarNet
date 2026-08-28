namespace AStarNet.Tests;

/// <summary>
/// Tests construction and validation of path connections.
/// </summary>
public sealed class PathConnectionTests
{
    /// <summary>
    /// Verifies that construction preserves the destination identifier and cost.
    /// </summary>
    [Fact]
    public void Constructor_WithValidValues_PreservesValues()
    {
        PathConnection connection = new(3, 1.5);

        Assert.Equal(3, connection.DestinationNodeId);
        Assert.Equal(1.5, connection.Cost);
    }

    /// <summary>
    /// Verifies that invalid traversal costs are rejected immediately.
    /// </summary>
    /// <param name="cost">The invalid cost.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_WithInvalidCost_Throws(double cost)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PathConnection(3, cost));
    }
}
