using System;

namespace AStarNet.Heuristics;

/// <summary>
/// Provides common distance calculations for heuristic providers.
/// </summary>
public static class HeuristicMath
{
    #region Constants

    private const double SquareRootOfTwo = 1.4142135623730951;
    private const double SquareRootOfThree = 1.7320508075688772;

    #endregion

    #region Public methods

    /// <summary>
    /// Calculates Manhattan distance in two dimensions.
    /// </summary>
    /// <param name="deltaX">The signed or absolute difference on the X axis.</param>
    /// <param name="deltaY">The signed or absolute difference on the Y axis.</param>
    /// <returns>The sum of the absolute axis differences.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is not finite.</exception>
    /// <exception cref="OverflowException">The calculated distance is not finite.</exception>
    public static double Manhattan2D(double deltaX, double deltaY)
    {
        HeuristicMath.ValidateFinite(deltaX, nameof(deltaX));
        HeuristicMath.ValidateFinite(deltaY, nameof(deltaY));

        double distance = Math.Abs(deltaX) + Math.Abs(deltaY);
        return HeuristicMath.ValidateResult(distance);
    }

    /// <summary>
    /// Calculates Manhattan distance in three dimensions.
    /// </summary>
    /// <param name="deltaX">The signed or absolute difference on the X axis.</param>
    /// <param name="deltaY">The signed or absolute difference on the Y axis.</param>
    /// <param name="deltaZ">The signed or absolute difference on the Z axis.</param>
    /// <returns>The sum of the absolute axis differences.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is not finite.</exception>
    /// <exception cref="OverflowException">The calculated distance is not finite.</exception>
    public static double Manhattan3D(double deltaX, double deltaY, double deltaZ)
    {
        HeuristicMath.ValidateFinite(deltaX, nameof(deltaX));
        HeuristicMath.ValidateFinite(deltaY, nameof(deltaY));
        HeuristicMath.ValidateFinite(deltaZ, nameof(deltaZ));

        double distance = Math.Abs(deltaX) + Math.Abs(deltaY) + Math.Abs(deltaZ);
        return HeuristicMath.ValidateResult(distance);
    }

    /// <summary>
    /// Calculates Euclidean distance in two dimensions.
    /// </summary>
    /// <param name="deltaX">The signed or absolute difference on the X axis.</param>
    /// <param name="deltaY">The signed or absolute difference on the Y axis.</param>
    /// <returns>The straight-line distance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is not finite.</exception>
    /// <exception cref="OverflowException">The calculated distance is not finite.</exception>
    public static double Euclidean2D(double deltaX, double deltaY)
    {
        HeuristicMath.ValidateFinite(deltaX, nameof(deltaX));
        HeuristicMath.ValidateFinite(deltaY, nameof(deltaY));

        double absoluteX = Math.Abs(deltaX);
        double absoluteY = Math.Abs(deltaY);
        double maximum = Math.Max(absoluteX, absoluteY);

        if (maximum == 0)
            return 0;

        double scaledX = absoluteX / maximum;
        double scaledY = absoluteY / maximum;
        double distance = maximum * Math.Sqrt((scaledX * scaledX) + (scaledY * scaledY));
        return HeuristicMath.ValidateResult(distance);
    }

    /// <summary>
    /// Calculates Euclidean distance in three dimensions.
    /// </summary>
    /// <param name="deltaX">The signed or absolute difference on the X axis.</param>
    /// <param name="deltaY">The signed or absolute difference on the Y axis.</param>
    /// <param name="deltaZ">The signed or absolute difference on the Z axis.</param>
    /// <returns>The straight-line distance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is not finite.</exception>
    /// <exception cref="OverflowException">The calculated distance is not finite.</exception>
    public static double Euclidean3D(double deltaX, double deltaY, double deltaZ)
    {
        HeuristicMath.ValidateFinite(deltaX, nameof(deltaX));
        HeuristicMath.ValidateFinite(deltaY, nameof(deltaY));
        HeuristicMath.ValidateFinite(deltaZ, nameof(deltaZ));

        double absoluteX = Math.Abs(deltaX);
        double absoluteY = Math.Abs(deltaY);
        double absoluteZ = Math.Abs(deltaZ);
        double maximum = Math.Max(absoluteX, Math.Max(absoluteY, absoluteZ));

        if (maximum == 0)
            return 0;

        double scaledX = absoluteX / maximum;
        double scaledY = absoluteY / maximum;
        double scaledZ = absoluteZ / maximum;
        double distance = maximum * Math.Sqrt(
            (scaledX * scaledX) + (scaledY * scaledY) + (scaledZ * scaledZ));
        return HeuristicMath.ValidateResult(distance);
    }

    /// <summary>
    /// Calculates octile distance in two dimensions for movement along axes and diagonals.
    /// </summary>
    /// <param name="deltaX">The signed or absolute difference on the X axis.</param>
    /// <param name="deltaY">The signed or absolute difference on the Y axis.</param>
    /// <returns>The distance using axis cost 1 and diagonal cost √2.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is not finite.</exception>
    /// <exception cref="OverflowException">The calculated distance is not finite.</exception>
    public static double Octile(double deltaX, double deltaY)
    {
        HeuristicMath.ValidateFinite(deltaX, nameof(deltaX));
        HeuristicMath.ValidateFinite(deltaY, nameof(deltaY));

        double absoluteX = Math.Abs(deltaX);
        double absoluteY = Math.Abs(deltaY);
        double diagonalDistance = Math.Min(absoluteX, absoluteY);
        double straightDistance = Math.Max(absoluteX, absoluteY) - diagonalDistance;
        double distance = (diagonalDistance * SquareRootOfTwo) + straightDistance;

        return HeuristicMath.ValidateResult(distance);
    }

    /// <summary>
    /// Calculates diagonal distance in three dimensions for movement along one, two, or three axes at a time.
    /// </summary>
    /// <param name="deltaX">The signed or absolute difference on the X axis.</param>
    /// <param name="deltaY">The signed or absolute difference on the Y axis.</param>
    /// <param name="deltaZ">The signed or absolute difference on the Z axis.</param>
    /// <returns>The distance using costs 1, √2, and √3 for movement along one, two, and three axes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is not finite.</exception>
    /// <exception cref="OverflowException">The calculated distance is not finite.</exception>
    public static double Diagonal3D(double deltaX, double deltaY, double deltaZ)
    {
        HeuristicMath.ValidateFinite(deltaX, nameof(deltaX));
        HeuristicMath.ValidateFinite(deltaY, nameof(deltaY));
        HeuristicMath.ValidateFinite(deltaZ, nameof(deltaZ));

        double absoluteX = Math.Abs(deltaX);
        double absoluteY = Math.Abs(deltaY);
        double absoluteZ = Math.Abs(deltaZ);
        double minimum = absoluteX;
        double middle = absoluteY;
        double maximum = absoluteZ;

        if (minimum > middle)
            (minimum, middle) = (middle, minimum);

        if (middle > maximum)
            (middle, maximum) = (maximum, middle);

        if (minimum > middle)
            (minimum, middle) = (middle, minimum);

        double distance =
            (minimum * SquareRootOfThree) +
            ((middle - minimum) * SquareRootOfTwo) +
            (maximum - middle);

        return HeuristicMath.ValidateResult(distance);
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
    /// Validates a calculated distance.
    /// </summary>
    /// <param name="result">The calculated result.</param>
    /// <returns>The validated result.</returns>
    private static double ValidateResult(double result)
    {
        if (!double.IsFinite(result))
            throw new OverflowException("The calculated heuristic distance is not finite.");

        return result;
    }

    #endregion
}
