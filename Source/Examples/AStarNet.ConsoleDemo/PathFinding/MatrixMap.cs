// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using AStarNet;
using AStarNet.Heuristics;
using AStarNet.Maps;
using System.Numerics;

namespace AStarNet.ConsoleDemo.PathFinding;

/// <summary>
/// Represents a two-dimensional matrix navigable using the A* algorithm.
/// </summary>
public class MatrixMap : INodeMap<Vector2?>, IHeuristicProvider<Vector2?>
{
    #region Fields

    private readonly Vector2[,] _vectorMatrix;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixMap"/> class.
    /// </summary>
    /// <param name="width">The width of the matrix.</param>
    /// <param name="height">The height of the matrix.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive or the matrix is too large.</exception>
    public MatrixMap(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _ = checked(width * height);

        this.Width = width;
        this.Height = height;
        this._vectorMatrix = new Vector2[width, height];
        this.WallBlocks = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                this._vectorMatrix[x, y] = new Vector2(x, y);
            }
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the width of the matrix.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the matrix.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the matrix that indicates which cells are blocked.
    /// </summary>
    public bool[,] WallBlocks { get; }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public IEnumerable<PathConnection<Vector2?>> GetConnections(PathNode<Vector2?> node)
    {
        Vector2 coordinates = MatrixMap.GetCoordinates(node);
        List<PathConnection<Vector2?>> connections = [];

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int newX = (int)coordinates.X + dx;
                int newY = (int)coordinates.Y + dy;

                if (!this.IsInsideMap(newX, newY) || this.WallBlocks[newX, newY])
                    continue;

                double cost = dx == 0 || dy == 0 ? 1.0 : Math.Sqrt(2);
                Vector2 childContent = this._vectorMatrix[newX, newY];
                int childId = this.GetNodeId(newX, newY);

                PathNode<Vector2?> childNode = new(childId, childContent);
                connections.Add(new PathConnection<Vector2?>(childNode, cost));
            }
        }

        return connections;
    }

    /// <inheritdoc/>
    public PathNode<Vector2?>? GetNode(int id)
    {
        if (id < 0 || id >= this.Width * this.Height)
            return null;

        int x = id % this.Width;
        int y = id / this.Width;

        if (this.WallBlocks[x, y])
            return null;

        return new PathNode<Vector2?>(id, this._vectorMatrix[x, y]);
    }

    /// <inheritdoc/>
    public double GetHeuristic(PathNode<Vector2?> from, PathNode<Vector2?> to)
    {
        Vector2 fromCoordinates = MatrixMap.GetCoordinates(from);
        Vector2 toCoordinates = MatrixMap.GetCoordinates(to);
        double dx = toCoordinates.X - fromCoordinates.X;
        double dy = toCoordinates.Y - fromCoordinates.Y;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>
    /// Gets the node identifier associated with the specified coordinates.
    /// </summary>
    /// <param name="coordinates">The matrix coordinates.</param>
    /// <returns>The corresponding node identifier.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="coordinates"/> is outside the matrix.</exception>
    public int GetNodeId(Vector2 coordinates)
    {
        int x = checked((int)coordinates.X);
        int y = checked((int)coordinates.Y);

        if (!this.IsInsideMap(x, y) || coordinates.X != x || coordinates.Y != y)
            throw new ArgumentOutOfRangeException(nameof(coordinates), "Coordinates are outside the matrix or are not integral.");

        return this.GetNodeId(x, y);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Gets the coordinates stored in a node.
    /// </summary>
    /// <param name="node">The node whose content is requested.</param>
    /// <returns>The node coordinates.</returns>
    /// <exception cref="ArgumentException">The node does not contain coordinates.</exception>
    private static Vector2 GetCoordinates(PathNode<Vector2?> node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node.Content
            ?? throw new ArgumentException("The node does not contain matrix coordinates.", nameof(node));
    }

    /// <summary>
    /// Gets the node identifier associated with integral coordinates.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <returns>The corresponding node identifier.</returns>
    private int GetNodeId(int x, int y)
    {
        return (y * this.Width) + x;
    }

    /// <summary>
    /// Determines whether the specified coordinates are inside the matrix.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <returns><see langword="true"/> when the coordinates are valid; otherwise, <see langword="false"/>.</returns>
    private bool IsInsideMap(int x, int y)
    {
        return x >= 0 && x < this.Width && y >= 0 && y < this.Height;
    }

    #endregion
}
