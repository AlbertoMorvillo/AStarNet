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
        int[] nodeIds = [startId, .. connections.Select(connection => connection.DestinationId)];
        (int From, int To, double Cost)[] edges = new (int From, int To, double Cost)[connections.Length];
        int currentNodeId = startId;

        for (int index = 0; index < connections.Length; index++)
        {
            (int destinationId, double cost) = connections[index];
            edges[index] = (currentNodeId, destinationId, cost);
            currentNodeId = destinationId;
        }

        TestGraph graph = new(nodeIds.Distinct(), edges);
        PathFinder pathFinder = new(graph);
        return pathFinder.FindPath(startId, currentNodeId);
    }
}
