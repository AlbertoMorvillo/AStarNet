using AStarNet.Heuristics;

namespace AStarNet.ConsoleDemo.PathFinding.Heuristics;

/// <summary>
/// Estimates grid traversal costs using a selected distance function.
/// </summary>
internal sealed class GridHeuristic : IHeuristicProvider
{
    private readonly MatrixMap _map;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridHeuristic"/> class.
    /// </summary>
    /// <param name="map">The grid that defines node positions.</param>
    /// <param name="kind">The distance function to use.</param>
    public GridHeuristic(MatrixMap map, GridHeuristicKind kind)
    {
        ArgumentNullException.ThrowIfNull(map);

        this._map = map;
        this.Kind = kind;
    }

    /// <summary>
    /// Gets the distance function used by this provider.
    /// </summary>
    public GridHeuristicKind Kind { get; }

    /// <inheritdoc/>
    public double GetHeuristic(int fromNodeId, int toNodeId)
    {
        GridPosition from = this._map.GetPosition(fromNodeId);
        GridPosition to = this._map.GetPosition(toNodeId);
        double deltaX = to.X - from.X;
        double deltaY = to.Y - from.Y;

        return this.Kind switch
        {
            GridHeuristicKind.Octile => HeuristicMath.Octile(deltaX, deltaY),
            GridHeuristicKind.Euclidean => HeuristicMath.Euclidean2D(deltaX, deltaY),
            GridHeuristicKind.Manhattan => HeuristicMath.Manhattan2D(deltaX, deltaY),
            _ => throw new InvalidOperationException($"Unsupported grid heuristic '{this.Kind}'.")
        };
    }
}
