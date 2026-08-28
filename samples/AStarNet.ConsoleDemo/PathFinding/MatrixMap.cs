using AStarNet.Maps;

namespace AStarNet.ConsoleDemo.PathFinding;

/// <summary>
/// Provides a navigable two-dimensional grid.
/// </summary>
internal sealed class MatrixMap : INodeMap
{
    private readonly int _nodeCount;
    private readonly bool[,] _walls;
    private int _wallCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixMap"/> class with the specified dimensions.
    /// </summary>
    /// <param name="width">The number of columns.</param>
    /// <param name="height">The number of rows.</param>
    public MatrixMap(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        int nodeCount = checked(width * height);

        this.Width = width;
        this.Height = height;
        this._nodeCount = nodeCount;
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
        return this.TryGetTraversablePosition(nodeId, out _, out _);
    }

    /// <inheritdoc/>
    public IEnumerable<PathConnection>? GetConnections(int nodeId)
    {
        if (!this.TryGetTraversablePosition(nodeId, out int originX, out int originY))
            return null;

        return this.EnumerateConnections(originX, originY);
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
    /// Gets the grid position associated with a node identifier.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <returns>The corresponding grid position.</returns>
    public GridPosition GetPosition(int nodeId)
    {
        if (nodeId < 0 || nodeId >= this._nodeCount)
            throw new ArgumentOutOfRangeException(nameof(nodeId), "The node identifier is outside the grid.");

        return new GridPosition(nodeId % this.Width, nodeId / this.Width);
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
    /// <param name="originX">The X coordinate of the existing node.</param>
    /// <param name="originY">The Y coordinate of the existing node.</param>
    /// <returns>The outgoing connections.</returns>
    private IEnumerable<PathConnection> EnumerateConnections(int originX, int originY)
    {
        for (int deltaX = -1; deltaX <= 1; deltaX++)
        {
            for (int deltaY = -1; deltaY <= 1; deltaY++)
            {
                if (deltaX == 0 && deltaY == 0)
                    continue;

                int destinationX = originX + deltaX;
                int destinationY = originY + deltaY;

                if (destinationX < 0 ||
                    destinationX >= this.Width ||
                    destinationY < 0 ||
                    destinationY >= this.Height ||
                    this._walls[destinationX, destinationY])
                {
                    continue;
                }

                bool isDiagonal = deltaX != 0 && deltaY != 0;
                double cost = isDiagonal ? Math.Sqrt(2) : 1;
                int destinationId = (destinationY * this.Width) + destinationX;
                yield return new PathConnection(destinationId, cost);
            }
        }
    }

    /// <summary>
    /// Resolves a traversable node identifier to its grid coordinates.
    /// </summary>
    /// <param name="nodeId">The node identifier to resolve.</param>
    /// <param name="x">The resolved X coordinate.</param>
    /// <param name="y">The resolved Y coordinate.</param>
    /// <returns><see langword="true"/> when the identifier represents a traversable node; otherwise, <see langword="false"/>.</returns>
    private bool TryGetTraversablePosition(int nodeId, out int x, out int y)
    {
        if (nodeId < 0 || nodeId >= this._nodeCount)
        {
            x = 0;
            y = 0;
            return false;
        }

        x = nodeId % this.Width;
        y = nodeId / this.Width;
        return !this._walls[x, y];
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
