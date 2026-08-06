namespace AStarNet.Heuristics;

/// <summary>
/// Provides an admissible heuristic that always returns zero, making pathfinding behave like Dijkstra's algorithm
/// and preserving the optimality guarantee.
/// </summary>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public sealed class ZeroHeuristic<TContent> : IHeuristicProvider<TContent>
{
    /// <inheritdoc/>
    public double GetHeuristic(PathNode<TContent> from, PathNode<TContent> to)
    {
        return 0;
    }
}
