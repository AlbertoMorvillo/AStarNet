// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet
{
    /// <summary>
    /// Defines a path node.
    /// </summary>
    /// <typeparam name="TId">The type of the node identifier.</typeparam>
    public class PathNode<TId> : IPathNode<TId> where TId : notnull
    {
        #region Properties

        /// <inheritdoc/>
        public TId Id { get; }

        /// <inheritdoc/>
        public double Cost { get; }

        /// <inheritdoc/>
        public object? Content { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PathNode{TId}"/> class.
        /// </summary>
        /// <param name="id">The identifier of the node.</param>
        /// <param name="cost">The cost of this node.</param>
        /// <param name="content">The optional content of this node.</param>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
        public PathNode(TId id, double cost, object? content = null)
        {
            ArgumentNullException.ThrowIfNull(id);

            this.Id = id;
            this.Content = content;
            this.Cost = cost;
        }

        #endregion

        #region Public methods

        #region Equality

        /// <summary>
        /// Returns a value indicating whether this istance and a specific <see cref="IPathNode{TId}"/> rappresent the same node.
        /// </summary>
        /// <param name="other">The other <see cref="IPathNode{TId}"/> to compare with the current node.</param>
        /// <returns>True if this and the other istance rappresent the same node.</returns>
        public bool Equals(IPathNode<TId>? other)
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

            if (obj is not PathNode<TId> other)
                return false;

            return this.Equals(other);
        }

        /// <summary>
        /// Determines whether two <see cref="PathNode{TId}"/> instances are equal.
        /// </summary>
        /// <param name="x">The first node to compare.</param>
        /// <param name="y">The second node to compare.</param>
        /// <returns>
        /// <see langword="true"/> if both nodes are equal; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool Equals(PathNode<TId>? x, PathNode<TId>? y)
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
        public static bool operator ==(PathNode<TId> left, PathNode<TId>? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(PathNode<TId> left, PathNode<TId>? right)
        {
            return !(left == right);
        }

        #endregion

        #endregion
    }
}
