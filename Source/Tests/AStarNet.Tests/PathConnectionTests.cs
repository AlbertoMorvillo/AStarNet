namespace AStarNet.Tests;

/// <summary>
/// Tests connection construction and its numeric invariants.
/// </summary>
public sealed class PathConnectionTests
{
    /// <summary>
    /// Verifies that a valid connection retains its destination and cost.
    /// </summary>
    [Fact]
    public void Constructor_WhenArgumentsAreValid_StoresArguments()
    {
        PathNode<string> destination = new(3);

        PathConnection<string> connection = new(destination, 1.5);

        Assert.Same(destination, connection.Destination);
        Assert.Equal(1.5, connection.Cost);
    }

    /// <summary>
    /// Verifies that a missing destination is rejected.
    /// </summary>
    [Fact]
    public void Constructor_WhenDestinationIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PathConnection<string>(null!, 1));
    }

    /// <summary>
    /// Verifies that invalid connection costs are rejected.
    /// </summary>
    /// <param name="cost">The invalid cost.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_WhenCostIsInvalid_Throws(double cost)
    {
        PathNode<string> destination = new(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => new PathConnection<string>(destination, cost));
    }
}
