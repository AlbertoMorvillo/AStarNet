namespace AStarNet.ConsoleDemo.PathFinding.Heuristics;

/// <summary>
/// Identifies a distance estimate available in the demonstration grid.
/// </summary>
internal enum GridHeuristicKind
{
    /// <summary>
    /// Uses octile distance for eight-directional movement.
    /// </summary>
    Octile,

    /// <summary>
    /// Uses straight-line Euclidean distance.
    /// </summary>
    Euclidean,

    /// <summary>
    /// Uses Manhattan distance.
    /// </summary>
    Manhattan
}
