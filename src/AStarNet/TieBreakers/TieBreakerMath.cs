using System;

namespace AStarNet.TieBreakers;

/// <summary>
/// Provides common geometric calculations for tie-breaker providers.
/// </summary>
public static class TieBreakerMath
{
    #region Public methods

    /// <summary>
    /// Calculates a squared score proportional to a point's deviation from a line in two dimensions.
    /// </summary>
    /// <param name="startX">The X coordinate of the line's start.</param>
    /// <param name="startY">The Y coordinate of the line's start.</param>
    /// <param name="destinationX">The X coordinate of the line's destination.</param>
    /// <param name="destinationY">The Y coordinate of the line's destination.</param>
    /// <param name="candidateX">The X coordinate of the candidate point.</param>
    /// <param name="candidateY">The Y coordinate of the candidate point.</param>
    /// <returns>
    /// The squared cross-product magnitude. When candidates are measured against the same endpoints, lower values
    /// represent points closer to the line. The result is zero when the endpoints coincide.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is not finite.</exception>
    /// <exception cref="OverflowException">The calculated score is not finite.</exception>
    public static double SquaredLineDeviation2D(
        double startX,
        double startY,
        double destinationX,
        double destinationY,
        double candidateX,
        double candidateY)
    {
        TieBreakerMath.ValidateFinite(startX, nameof(startX));
        TieBreakerMath.ValidateFinite(startY, nameof(startY));
        TieBreakerMath.ValidateFinite(destinationX, nameof(destinationX));
        TieBreakerMath.ValidateFinite(destinationY, nameof(destinationY));
        TieBreakerMath.ValidateFinite(candidateX, nameof(candidateX));
        TieBreakerMath.ValidateFinite(candidateY, nameof(candidateY));

        double lineX = destinationX - startX;
        double lineY = destinationY - startY;
        double candidateOffsetX = candidateX - startX;
        double candidateOffsetY = candidateY - startY;
        double crossProduct = (candidateOffsetX * lineY) - (candidateOffsetY * lineX);
        double score = crossProduct * crossProduct;

        return TieBreakerMath.ValidateResult(score);
    }

    /// <summary>
    /// Calculates a squared score proportional to a point's deviation from a line in three dimensions.
    /// </summary>
    /// <param name="startX">The X coordinate of the line's start.</param>
    /// <param name="startY">The Y coordinate of the line's start.</param>
    /// <param name="startZ">The Z coordinate of the line's start.</param>
    /// <param name="destinationX">The X coordinate of the line's destination.</param>
    /// <param name="destinationY">The Y coordinate of the line's destination.</param>
    /// <param name="destinationZ">The Z coordinate of the line's destination.</param>
    /// <param name="candidateX">The X coordinate of the candidate point.</param>
    /// <param name="candidateY">The Y coordinate of the candidate point.</param>
    /// <param name="candidateZ">The Z coordinate of the candidate point.</param>
    /// <returns>
    /// The squared cross-product magnitude. When candidates are measured against the same endpoints, lower values
    /// represent points closer to the line. The result is zero when the endpoints coincide.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is not finite.</exception>
    /// <exception cref="OverflowException">The calculated score is not finite.</exception>
    public static double SquaredLineDeviation3D(
        double startX,
        double startY,
        double startZ,
        double destinationX,
        double destinationY,
        double destinationZ,
        double candidateX,
        double candidateY,
        double candidateZ)
    {
        TieBreakerMath.ValidateFinite(startX, nameof(startX));
        TieBreakerMath.ValidateFinite(startY, nameof(startY));
        TieBreakerMath.ValidateFinite(startZ, nameof(startZ));
        TieBreakerMath.ValidateFinite(destinationX, nameof(destinationX));
        TieBreakerMath.ValidateFinite(destinationY, nameof(destinationY));
        TieBreakerMath.ValidateFinite(destinationZ, nameof(destinationZ));
        TieBreakerMath.ValidateFinite(candidateX, nameof(candidateX));
        TieBreakerMath.ValidateFinite(candidateY, nameof(candidateY));
        TieBreakerMath.ValidateFinite(candidateZ, nameof(candidateZ));

        double lineX = destinationX - startX;
        double lineY = destinationY - startY;
        double lineZ = destinationZ - startZ;
        double candidateOffsetX = candidateX - startX;
        double candidateOffsetY = candidateY - startY;
        double candidateOffsetZ = candidateZ - startZ;
        double crossX = (candidateOffsetY * lineZ) - (candidateOffsetZ * lineY);
        double crossY = (candidateOffsetZ * lineX) - (candidateOffsetX * lineZ);
        double crossZ = (candidateOffsetX * lineY) - (candidateOffsetY * lineX);
        double score = (crossX * crossX) + (crossY * crossY) + (crossZ * crossZ);

        return TieBreakerMath.ValidateResult(score);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Validates that a numeric input is finite.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The public parameter name.</param>
    private static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite.");
    }

    /// <summary>
    /// Validates a calculated tie-breaker score.
    /// </summary>
    /// <param name="result">The calculated result.</param>
    /// <returns>The validated result.</returns>
    private static double ValidateResult(double result)
    {
        if (!double.IsFinite(result))
            throw new OverflowException("The calculated tie-breaker score is not finite.");

        return result;
    }

    #endregion
}
