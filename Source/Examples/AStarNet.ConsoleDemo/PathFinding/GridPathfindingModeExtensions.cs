namespace AStarNet.ConsoleDemo.PathFinding;

/// <summary>
/// Provides display information for demonstration pathfinding modes.
/// </summary>
internal static class GridPathfindingModeExtensions
{
    /// <summary>
    /// Gets the concise display name of a pathfinding mode.
    /// </summary>
    /// <param name="mode">The pathfinding mode.</param>
    /// <returns>The display name.</returns>
    public static string GetDisplayName(this GridPathfindingMode mode)
    {
        return mode switch
        {
            GridPathfindingMode.DijkstraWithLineTieBreaker => "Dijkstra (line)",
            GridPathfindingMode.OctileWithLineTieBreaker => "Octile (line)",
            GridPathfindingMode.EuclideanWithLineTieBreaker => "Euclid (line)",
            _ => mode.ToString()
        };
    }
}
