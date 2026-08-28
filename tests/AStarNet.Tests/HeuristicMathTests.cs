using AStarNet.Heuristics;

namespace AStarNet.Tests;

/// <summary>
/// Verifies common heuristic distance calculations.
/// </summary>
public sealed class HeuristicMathTests
{
    /// <summary>
    /// Verifies Manhattan distance in two and three dimensions.
    /// </summary>
    [Fact]
    public void Manhattan_WithSignedDeltas_ReturnsAbsoluteAxisSums()
    {
        Assert.Equal(7, HeuristicMath.Manhattan2D(-3, 4));
        Assert.Equal(12, HeuristicMath.Manhattan3D(-3, 4, -5));
    }

    /// <summary>
    /// Verifies Euclidean distance in two and three dimensions.
    /// </summary>
    [Fact]
    public void Euclidean_WithKnownTriangles_ReturnsStraightLineDistances()
    {
        Assert.Equal(5, HeuristicMath.Euclidean2D(3, 4));
        Assert.Equal(7, HeuristicMath.Euclidean3D(2, 3, 6));
    }

    /// <summary>
    /// Verifies that Euclidean calculations avoid overflowing intermediate squares.
    /// </summary>
    [Fact]
    public void Euclidean_WithRepresentableLargeDistance_AvoidsIntermediateOverflow()
    {
        Assert.Equal(double.MaxValue, HeuristicMath.Euclidean2D(double.MaxValue, 0));
        Assert.Equal(double.MaxValue, HeuristicMath.Euclidean3D(double.MaxValue, 0, 0));
    }

    /// <summary>
    /// Verifies octile distance with axis and diagonal movement costs.
    /// </summary>
    [Fact]
    public void Octile_WithUnequalDeltas_CombinesDiagonalAndStraightMovement()
    {
        double expected = (3 * Math.Sqrt(2)) + 2;

        double distance = HeuristicMath.Octile(3, 5);

        Assert.Equal(expected, distance, precision: 12);
    }

    /// <summary>
    /// Verifies diagonal distance with one-axis, two-axis, and three-axis movement.
    /// </summary>
    [Fact]
    public void Diagonal3D_WithUnequalDeltas_CombinesAllMovementTypes()
    {
        double expected = (2 * Math.Sqrt(3)) + Math.Sqrt(2) + 2;

        double distance = HeuristicMath.Diagonal3D(2, 3, 5);

        Assert.Equal(expected, distance, precision: 12);
    }

    /// <summary>
    /// Verifies that non-finite inputs are rejected.
    /// </summary>
    [Fact]
    public void Distance_WhenAnInputIsNotFinite_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HeuristicMath.Euclidean2D(double.NaN, 1));
    }

    /// <summary>
    /// Verifies that a non-finite calculated distance is rejected.
    /// </summary>
    [Fact]
    public void Manhattan_WhenResultOverflows_Throws()
    {
        Assert.Throws<OverflowException>(() => HeuristicMath.Manhattan2D(double.MaxValue, double.MaxValue));
    }
}
