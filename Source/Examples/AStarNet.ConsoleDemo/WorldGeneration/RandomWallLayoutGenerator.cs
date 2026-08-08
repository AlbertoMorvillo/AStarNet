using AStarNet.ConsoleDemo.PathFinding;

namespace AStarNet.ConsoleDemo.WorldGeneration;

/// <summary>
/// Generates connected wall segments for the demonstration grid.
/// </summary>
internal static class RandomWallLayoutGenerator
{
    private const int MinimumObstacleCount = 6;
    private const int MaximumObstacleCount = 9;
    private const int MinimumSegmentsPerObstacle = 1;
    private const int MaximumSegmentsPerObstacle = 2;
    private const int MinimumWallSegmentLength = 3;
    private const int MaximumWallSegmentLength = 8;

    /// <summary>
    /// Adds a generated wall layout to a map.
    /// </summary>
    /// <param name="map">The map that receives the generated walls.</param>
    /// <param name="seed">The seed controlling the generated wall arrangement.</param>
    /// <remarks>
    /// Reusing a seed preserves the arrangement when the same runtime implementation and map dimensions are used.
    /// Existing walls are not removed before generation.
    /// </remarks>
    public static void Generate(MatrixMap map, int seed)
    {
        ArgumentNullException.ThrowIfNull(map);

        Random random = new(seed);
        int obstacleCount = random.Next(MinimumObstacleCount, MaximumObstacleCount + 1);

        for (int obstacleIndex = 0; obstacleIndex < obstacleCount; obstacleIndex++)
        {
            GridPosition position = new(
                random.Next(1, map.Width - 1),
                random.Next(1, map.Height - 1));
            bool isHorizontal = random.Next(2) == 0;
            int segmentCount = random.Next(MinimumSegmentsPerObstacle, MaximumSegmentsPerObstacle + 1);

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                position = RandomWallLayoutGenerator.DrawSegment(map, random, position, isHorizontal);
                isHorizontal = !isHorizontal;
            }
        }
    }

    /// <summary>
    /// Draws one bounded wall segment and returns its final position.
    /// </summary>
    /// <param name="map">The map that receives the wall segment.</param>
    /// <param name="random">The deterministic random source.</param>
    /// <param name="origin">The first position in the segment.</param>
    /// <param name="isHorizontal">Whether the segment extends horizontally.</param>
    /// <returns>The final position in the generated segment.</returns>
    private static GridPosition DrawSegment(
        MatrixMap map,
        Random random,
        GridPosition origin,
        bool isHorizontal)
    {
        int negativeCapacity = isHorizontal ? origin.X - 1 : origin.Y - 1;
        int positiveCapacity = isHorizontal
            ? map.Width - origin.X - 2
            : map.Height - origin.Y - 2;
        int direction = random.Next(2) == 0 ? -1 : 1;

        if ((direction < 0 && negativeCapacity < MinimumWallSegmentLength - 1) ||
            (direction > 0 && positiveCapacity < MinimumWallSegmentLength - 1))
            direction = -direction;

        int capacity = direction < 0 ? negativeCapacity : positiveCapacity;
        int maximumLength = Math.Min(capacity + 1, MaximumWallSegmentLength);
        int length = random.Next(MinimumWallSegmentLength, maximumLength + 1);
        int deltaX = isHorizontal ? direction : 0;
        int deltaY = isHorizontal ? 0 : direction;

        for (int offset = 0; offset < length; offset++)
        {
            GridPosition position = new(
                origin.X + (deltaX * offset),
                origin.Y + (deltaY * offset));
            map.SetWall(position, isWall: true);
        }

        return new GridPosition(
            origin.X + (deltaX * (length - 1)),
            origin.Y + (deltaY * (length - 1)));
    }
}
