namespace AStarNet.ConsoleDemo.PathFinding;

/// <summary>
/// Identifies a pathfinding configuration available in the demonstration grid.
/// </summary>
internal enum GridPathfindingMode
{
    /// <summary>
    /// Uses zero heuristic estimates and no explicit tie-breaking.
    /// </summary>
    Dijkstra,

    /// <summary>
    /// Uses zero heuristic estimates with line-deviation tie-breaking.
    /// </summary>
    DijkstraWithLineTieBreaker,

    /// <summary>
    /// Uses octile distance.
    /// </summary>
    Octile,

    /// <summary>
    /// Uses octile distance with line-deviation tie-breaking.
    /// </summary>
    OctileWithLineTieBreaker,

    /// <summary>
    /// Uses Euclidean distance.
    /// </summary>
    Euclidean,

    /// <summary>
    /// Uses Euclidean distance with line-deviation tie-breaking.
    /// </summary>
    EuclideanWithLineTieBreaker,

    /// <summary>
    /// Uses Manhattan distance.
    /// </summary>
    Manhattan
}
