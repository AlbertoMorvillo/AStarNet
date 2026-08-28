using AStarNet.TieBreakers;

namespace AStarNet.Tests;

/// <summary>
/// Verifies common geometric tie-breaker calculations.
/// </summary>
public sealed class TieBreakerMathTests
{
    /// <summary>
    /// Verifies squared line deviation in two dimensions.
    /// </summary>
    [Fact]
    public void SquaredLineDeviation2D_WithOffsetCandidate_ReturnsSquaredCrossProduct()
    {
        double score = TieBreakerMath.SquaredLineDeviation2D(0, 0, 10, 0, 3, 2);

        Assert.Equal(400, score);
    }

    /// <summary>
    /// Verifies squared line deviation in three dimensions.
    /// </summary>
    [Fact]
    public void SquaredLineDeviation3D_WithOffsetCandidate_ReturnsSquaredCrossProductMagnitude()
    {
        double score = TieBreakerMath.SquaredLineDeviation3D(0, 0, 0, 10, 0, 0, 3, 2, 4);

        Assert.Equal(2000, score);
    }

    /// <summary>
    /// Verifies that a candidate on the endpoint line has no deviation.
    /// </summary>
    [Fact]
    public void SquaredLineDeviation_WhenCandidateIsOnLine_ReturnsZero()
    {
        double score2D = TieBreakerMath.SquaredLineDeviation2D(1, 1, 5, 5, 3, 3);
        double score3D = TieBreakerMath.SquaredLineDeviation3D(1, 1, 1, 5, 5, 5, 3, 3, 3);

        Assert.Equal(0, score2D);
        Assert.Equal(0, score3D);
    }

    /// <summary>
    /// Verifies that non-finite inputs are rejected.
    /// </summary>
    [Fact]
    public void SquaredLineDeviation_WhenAnInputIsNotFinite_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TieBreakerMath.SquaredLineDeviation2D(0, 0, double.PositiveInfinity, 1, 1, 1));
    }

    /// <summary>
    /// Verifies that a non-finite calculated score is rejected.
    /// </summary>
    [Fact]
    public void SquaredLineDeviation_WhenResultOverflows_Throws()
    {
        Assert.Throws<OverflowException>(
            () => TieBreakerMath.SquaredLineDeviation2D(0, 0, double.MaxValue, 1, 1, double.MaxValue));
    }
}
