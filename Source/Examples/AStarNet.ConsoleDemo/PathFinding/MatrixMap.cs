using AStarNet.Heuristics;
using AStarNet.Maps;

namespace AStarNet.ConsoleDemo.PathFinding;

/// <summary>
/// Provides a navigable two-dimensional grid and its matching A* heuristic.
/// </summary>
internal sealed class MatrixMap : INodeMap, IHeuristicProvider
{
    private readonly bool[,] _walls;
    private int _wallCount;

    /// <summary>
    /// Initializes a grid with the specified dimensions.
    /// </summary>
    /// <param name="width">The number of columns.</param>
    /// <param name="height">The number of rows.</param>
    public MatrixMap(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        _ = checked(width * height);

        this.Width = width;
        this.Height = height;
        this._walls = new bool[width, height];
    }

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets a value that changes whenever the wall layout changes.
    /// </summary>
    public long Version { get; private set; }

    /// <inheritdoc/>
    public bool ContainsNode(int nodeId)
    {
        if (nodeId < 0 || nodeId >= this.Width * this.Height)
            return false;

        return !this.IsWall(this.GetPosition(nodeId));
    }

    /// <inheritdoc/>
    public IEnumerable<PathConnection>? GetConnections(int nodeId)
    {
        if (!this.ContainsNode(nodeId))
            return null;

        return this.EnumerateConnections(nodeId);
    }

    /// <inheritdoc/>
    public double GetHeuristic(int fromNodeId, int toNodeId)
    {
        GridPosition from = this.GetPosition(fromNodeId);
        GridPosition to = this.GetPosition(toNodeId);
        int distanceX = Math.Abs(to.X - from.X);
        int distanceY = Math.Abs(to.Y - from.Y);
        int diagonalSteps = Math.Min(distanceX, distanceY);
        int straightSteps = Math.Max(distanceX, distanceY) - diagonalSteps;

        return (diagonalSteps * Math.Sqrt(2)) + straightSteps;
    }

    /// <summary>
    /// Gets the node identifier associated with a position.
    /// </summary>
    /// <param name="position">The grid position.</param>
    /// <returns>The corresponding node identifier.</returns>
    public int GetNodeId(GridPosition position)
    {
        if (!this.IsInside(position))
            throw new ArgumentOutOfRangeException(nameof(position), "The position is outside the grid.");

        return (position.Y * this.Width) + position.X;
    }

    /// <summary>
    /// Determines whether a position contains a wall.
    /// </summary>
    /// <param name="position">The grid position.</param>
    /// <returns><see langword="true"/> when the position is blocked; otherwise, <see langword="false"/>.</returns>
    public bool IsWall(GridPosition position)
    {
        return this.IsInside(position) && this._walls[position.X, position.Y];
    }

    /// <summary>
    /// Sets the wall state of a position.
    /// </summary>
    /// <param name="position">The grid position.</param>
    /// <param name="isWall">The new wall state.</param>
    public void SetWall(GridPosition position, bool isWall)
    {
        if (!this.IsInside(position))
            throw new ArgumentOutOfRangeException(nameof(position), "The position is outside the grid.");

        bool currentValue = this._walls[position.X, position.Y];
        if (currentValue == isWall)
            return;

        this._walls[position.X, position.Y] = isWall;
        this._wallCount += isWall ? 1 : -1;
        this.Version++;
    }

    /// <summary>
    /// Removes every wall from the grid.
    /// </summary>
    public void ClearWalls()
    {
        if (this._wallCount == 0)
            return;

        Array.Clear(this._walls);
        this._wallCount = 0;
        this.Version++;
    }

    /// <summary>
    /// Enumerates the outgoing connections of an existing node.
    /// </summary>
    /// <param name="nodeId">The existing node identifier.</param>
    /// <returns>The outgoing connections.</returns>
    private IEnumerable<PathConnection> EnumerateConnections(int nodeId)
    {
        GridPosition origin = this.GetPosition(nodeId);

        for (int deltaX = -1; deltaX <= 1; deltaX++)
        {
            for (int deltaY = -1; deltaY <= 1; deltaY++)
            {
                if (deltaX == 0 && deltaY == 0)
                    continue;

                GridPosition destination = new(origin.X + deltaX, origin.Y + deltaY);
                if (!this.IsInside(destination) || this.IsWall(destination))
                    continue;

                bool isDiagonal = deltaX != 0 && deltaY != 0;
                double cost = isDiagonal ? Math.Sqrt(2) : 1;
                int destinationId = this.GetNodeId(destination);
                yield return new PathConnection(destinationId, cost);
            }
        }
    }

    /// <summary>
    /// Gets the position associated with a node identifier.
    /// </summary>
    /// <param name="id">The node identifier.</param>
    /// <returns>The corresponding grid position.</returns>
    private GridPosition GetPosition(int id)
    {
        return new GridPosition(id % this.Width, id / this.Width);
    }

    /// <summary>
    /// Determines whether a position is inside the grid.
    /// </summary>
    /// <param name="position">The position to inspect.</param>
    /// <returns><see langword="true"/> when the position is inside the grid; otherwise, <see langword="false"/>.</returns>
    private bool IsInside(GridPosition position)
    {
        return position.X >= 0 && position.X < this.Width && position.Y >= 0 && position.Y < this.Height;
    }
}
