namespace AStarNet.Tests;

/// <summary>
/// Creates paths directly for testing path behavior independently from pathfinding.
/// </summary>
internal static class TestPathFactory
{
    /// <summary>
    /// Creates a path from a start identifier and ordered destination-cost pairs.
    /// </summary>
    /// <param name="startId">The start-node identifier.</param>
    /// <param name="connections">The ordered destination identifiers and costs.</param>
    /// <returns>The created path.</returns>
    internal static Path Create(int startId, params (int DestinationId, double Cost)[] connections)
    {
        (int NodeId, double CostFromPrevious)[] steps = new (int NodeId, double CostFromPrevious)[connections.Length + 1];
        steps[0] = (startId, 0);

        for (int index = 0; index < connections.Length; index++)
        {
            (int destinationId, double cost) = connections[index];
            steps[index + 1] = (destinationId, cost);
        }

        return new Path(steps);
    }
}
