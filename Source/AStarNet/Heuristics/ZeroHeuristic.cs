namespace AStarNet.Heuristics;

/// <summary>
/// Provides a zero heuristic, making A* behave like Dijkstra's algorithm.
/// </summary>
public sealed class ZeroHeuristic : IHeuristicProvider
{
    /// <inheritdoc/>
    public double GetHeuristic(int fromNodeId, int toNodeId)
    {
        return 0;
    }
}
