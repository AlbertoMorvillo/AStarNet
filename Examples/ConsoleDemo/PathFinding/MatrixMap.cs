// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using AStarNet;
using AStarNet.Heuristics;
using AStarNet.Maps;
using System.Numerics;

namespace ConsoleDemo.PathFinding
{
    /// <summary>
    /// Represents a two-dimensional matrix navigable using the A* algorithm.
    /// </summary>
    public class MatrixMap : INodeMap<Vector2>, IHeuristicProvider<Vector2>
    {
        private readonly Vector2[,] _vectorMatrix;

        /// <summary>
        /// Gets the width of the matrix.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height of the matrix.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets a two-dimensional array indicating which cells in the matrix are wall blocks.
        /// A value of true indicates that the cell is blocked.
        /// </summary>
        public bool[,] WallBlocks { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixMap"/> class.
        /// </summary>
        /// <param name="width">The width of the matrix.</param>
        /// <param name="height">The height of the matrix.</param>
        public MatrixMap(int width, int height)
        {
            this._vectorMatrix = new Vector2[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    this._vectorMatrix[x, y] = new Vector2(x, y);
                }
            }

            this.Width = width;
            this.Height = height;
            this.WallBlocks = new bool[width, height];
        }

        #region INodeMap

        /// <inheritdoc/>
        public IEnumerable<IPathNode<Vector2>> GetChildNodes(IPathNode<Vector2> currentNode)
        {
            List<PathNode<Vector2>> childNodes = [];

            // Getting adjacent cells (orthogonal and diagonal) from the matrix
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    // Skip the current cell itself
                    if (dx == 0 && dy == 0)
                        continue;

                    int newX = (int)currentNode.Id.X + dx;
                    int newY = (int)currentNode.Id.Y + dy;

                    // Check boundaries
                    if (newX >= 0 && newX < this.Width && newY >= 0 && newY < this.Height)
                    {
                        // It is a wall block, ignore it.
                        if (this.WallBlocks[newX, newY])
                            continue;

                        // Determine the movement cost: 1 for orthogonal, √2 for diagonal.
                        double cost = dx == 0 || dy == 0 ? 1.0 : Math.Sqrt(2);

                        Vector2 childId = this._vectorMatrix[newX, newY];
                        PathNode<Vector2> child = new(childId, cost, null);

                        childNodes.Add(child);
                    }
                }
            }

            return childNodes;
        }

        /// <inheritdoc/>
        public IPathNode<Vector2>? GetNode(Vector2 id)
        {
            // Check for out of bounds
            if (id.X < 0 || id.X >= this.Width)
                return null;

            if (id.Y < 0 || id.Y >= this.Height)
                return null;

            int intX = (int)id.X;
            int intY = (int)id.Y;

            // It is a wall block, ignore it.
            if (this.WallBlocks[intX, intY])
                return null;

            Vector2 item = this._vectorMatrix[intX, intY];

            // No movement, so no cost.
            return new PathNode<Vector2>(item, 0, null);
        }

        #endregion

        #region IHeuristicProvider

        /// <summary>
        /// Computes the heuristic estimate (Euclidean distance) between the start and destination nodes,
        /// allowing for diagonal movement. This realistic estimate helps the A* algorithm efficiently
        /// find the optimal path while reducing unnecessary exploration.
        /// </summary>
        /// <param name="from">The starting node (<see cref="IPathNode{TId}"/> with TId set to Vector2).</param>
        /// <param name="to">The destination node (<see cref="IPathNode{TId}"/> with TId set to Vector2).</param>
        /// <returns>The estimated cost to reach the destination node from the starting node.</returns>
        public double GetHeuristic(IPathNode<Vector2> from, IPathNode<Vector2> to)
        {
            double dx = to.Id.X - from.Id.X;
            double dy = to.Id.Y - from.Id.Y;

            // Return the Euclidean distance.
            double euclideanDistance = Math.Sqrt((dx * dx) + (dy * dy));
            return euclideanDistance;
        }

        #endregion
    }
}
