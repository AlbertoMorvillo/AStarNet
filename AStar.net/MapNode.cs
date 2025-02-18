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
    public class MapNode<TContent> : INode<TContent>, IComparable, IComparable<MapNode<TContent>>, IEquatable<MapNode<TContent>>
    {
        #region Properties

        /// <inheritdoc/>
        public Guid Id { get; }

        /// <summary>
        /// Gets the <see cref="MapNode{TContent}"/> used during the search to record the parent of successor nodes.
        /// </summary>
        public MapNode<TContent>? Parent { get; }

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
        public TContent Content { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MapNode{TContent}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the node, represented by a <see cref="Guid"/>.</param>
        /// <param name="content">The <typeparamref name="TContent"/> content of this node.</param>
        /// <param name="parent">The parent <see cref="MapNode{TContent}"/>.</param>
        /// <param name="cost">The cost of this node.</param>
        /// <param name="heuristicDistance">The heuristic distance from the destination node.</param>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
        public MapNode(Guid id, TContent content, MapNode<TContent>? parent, double cost, double heuristicDistance)
        {
            ArgumentNullException.ThrowIfNull(content);

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
        /// Compares this node with another one based on their score values.
        /// </summary>
        /// <param name="other">The other <see cref="MapNode{TContent}"/> to compare with the current node.</param>
        /// <returns>
        /// A negative value if this node's score is less than the other node's score.
        /// Zero if the scores are equal.
        /// A positive value if this node's score is greater than the other node's score.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
        public int CompareTo(MapNode<TContent>? other)
        {
            ArgumentNullException.ThrowIfNull(other);

            int scoreComparison = this.Score.CompareTo(other.Score);

            return scoreComparison;
        }

        /// <inheritdoc/>
        public int CompareTo(object? obj)
        {
            if (obj is null)
                return 1;

            if (obj is not MapNode<TContent> other)
            {
                throw new ArgumentException($"Object must be of type {nameof(MapNode<TContent>)}.", nameof(obj));
            }

            return this.CompareTo(other);
        }

        /// <summary>
        /// Compares two <see cref="MapNode{TContent}"/> instances.
        /// </summary>
        /// <param name="x">The first node to compare.</param>
        /// <param name="y">The second node to compare.</param>
        /// <returns>
        /// A negative value if <paramref name="x"/> is less than <paramref name="y"/>.
        /// Zero if they are equal.
        /// A positive value if <paramref name="x"/> is greater than <paramref name="y"/>.
        /// </returns>
        public static int Compare(MapNode<TContent>? x, MapNode<TContent>? y)
        {
            if (x is null)
                return y is null ? 0 : -1;

            if (y is null)
                return 1;

            return x.CompareTo(y);
        }

        /// <summary>
        /// Returns a value indicating whether this istance and a specific <see cref="MapNode{TContent}"/> rappresent the same node.
        /// </summary>
        /// <param name="other">The other <see cref="MapNode{TContent}"/> to compare with the current node.</param>
        /// <returns>True if this and the other istance rappresent the same node.</returns>
        public bool Equals(MapNode<TContent>? other)
        {
            if (other is null)
                return false;

            return this.Id.Equals(other.Id);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is null)
                return false;

            if (obj is not MapNode<TContent> other)
                return false;

            return this.Equals(other);
        }

        /// <summary>
        /// Determines whether two <see cref="MapNode{TContent}"/> instances are equal.
        /// </summary>
        /// <param name="x">The first node to compare.</param>
        /// <param name="y">The second node to compare.</param>
        /// <returns>
        /// <see langword="true"/> if both nodes are equal; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool Equals(MapNode<TContent>? x, MapNode<TContent>? y)
        {
            if (x is null || y is null)
                return x is null && y is null;

            return x.Equals(y);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id);
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
