// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet
{
    /// <summary>
    ///Defines a noode used in the A* algorithm to perform the path search.
    /// </summary>
    /// <typeparam name="TContent">The type of the node content.</typeparam>
    public class MapNode<TContent> : INode<TContent>, IEquatable<MapNode<TContent>>, IComparable<MapNode<TContent>>
    {
        #region Properties

        /// <inheritdoc/>
        public Guid Id { get; }

        /// <summary>
        /// Gets the <see cref="MapNode{T}"/> used during the search to record the parent of successor nodes.
        /// </summary>
        public MapNode<TContent> Parent { get; }

        /// <inheritdoc/>
        public double Cost { get; }

        /// <summary>
        /// Gets the accumulated cost from the start node to this node along the path.
        /// </summary>
        public double CostFromStart { get; }

        /// <summary>
        /// Gets the heuristic estimate of distance to goal (H).
        /// </summary>
        public double HeuristicDistance { get; }

        /// <summary>
        /// Gets the accumulated cost of predecessors and self and heuristic (F).
        /// </summary>
        public double Score { get; }

        /// <inheritdoc/>
        public TContent Content { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MapNode{T}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the node, represented by a <see cref="Guid"/>.</param>
        /// <param name="content">The <typeparamref name="TContent"/> content of this node.</param>
        /// <param name="parent">The parent <see cref="MapNode{T}"/>.</param>
        /// <param name="cost">The cost of this node.</param>
        /// <param name="heuristicDistance">The heuristic distance from the destination node.</param>
        public MapNode(Guid id, TContent content, MapNode<TContent> parent, double cost, double heuristicDistance)
        {
            double parentCostFromStart = parent is null ? 0 : parent.CostFromStart;

            this.Id = id;
            this.Content = content;
            this.Parent = parent;
            this.Cost = cost;
            this.CostFromStart = parentCostFromStart + this.Cost;
            this.HeuristicDistance = heuristicDistance;
            this.Score = this.CostFromStart + heuristicDistance;
        }

        #endregion

        #region Equality and comparison

        /// <summary>
        /// Returns a value indicating whether this istance and a specific <see cref="MapNode{T}"/> rappresent the same node.
        /// </summary>
        /// <param name="other">Other <see cref="MapNode{T}"/> istance.</param>
        /// <returns>True if this and the other istance rappresent the same node.</returns>
        public bool Equals(MapNode<TContent> other)
        {
            if (other is null)
                return false;

            return this.Id.Equals(other.Id);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is null)
                return false;

            if (obj is not MapNode<TContent> other)
                return false;

            return this.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id);
        }

        /// <summary>
        /// Compares first the score then the cost from start of this node with another one.
        /// </summary>
        /// <param name="other">Other <see cref="MapNode{T}"/> to compare.</param>
        /// <returns>
        /// <para>Less than zero: This node as the score less than other node or the score equal and the current cost less than other node.</para>
        /// <para>Zero: This node has the score and the current cost equal to other node.</para>
        /// <para>Greater than zero: This node as the score greater than other node or the score equal ant the current cost greater than other node.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
        public int CompareTo(MapNode<TContent> other)
        {
            ArgumentNullException.ThrowIfNull(other);

            int scoreCompare = this.Score.CompareTo(other.Score);

            if (scoreCompare != 0)
            {
                // Scores not equals: return the scores comparison
                return scoreCompare;
            }

            // Scores equals: return the path cost comparison
            return this.CostFromStart.CompareTo(other.CostFromStart);
        }

        /// <inheritdoc/>
        public static bool operator ==(MapNode<TContent> left, MapNode<TContent> right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(MapNode<TContent> left, MapNode<TContent> right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static bool operator <(MapNode<TContent> left, MapNode<TContent> right)
        {
            return left is null ? right is not null : left.CompareTo(right) < 0;
        }

        /// <inheritdoc/>
        public static bool operator <=(MapNode<TContent> left, MapNode<TContent> right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        /// <inheritdoc/>
        public static bool operator >(MapNode<TContent> left, MapNode<TContent> right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

        /// <inheritdoc/>
        public static bool operator >=(MapNode<TContent> left, MapNode<TContent> right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        #endregion
    }
}
