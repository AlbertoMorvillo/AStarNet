// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.IO;

namespace AStarNet
{
    /// <summary>
    /// Defines a path node.
    /// </summary>
    /// <typeparam name="TContent">The type of the node content.</typeparam>
    public class PathNode<TContent> : INode<TContent>, IEquatable<PathNode<TContent>>
    {
        #region Properties

        /// <inheritdoc/>
        public Guid Id { get; }

        /// <summary>
        /// Gets the path to which this node belongs.
        /// </summary>
        public Path<TContent> Path { get; }

        /// <summary>
        /// Gets the zero-based index of this node within the path.
        /// </summary>
        public int IndexInPath { get; }

        /// <inheritdoc/>
        public double Cost { get; }

        /// <summary>
        /// Gets the accumulated cost from the start node to this node along the path.
        /// </summary>
        public double CostFromStart { get; }

        /// <inheritdoc/>
        public TContent Content { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PathNode{T}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the node, represented by a <see cref="Guid"/>.</param>
        /// <param name="content">The <typeparamref name="TContent"/> content of this node.</param>
        /// <param name="path">The path to which this node belongs.</param>
        /// <param name="indexInPath">The zero-based index of this node within the path.</param>
        /// <param name="cost">The cost of this node.</param>
        /// <param name="costFromStart">The accumulated cost from the start node to this node along the path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        public PathNode(Guid id, TContent content, Path<TContent> path, int indexInPath, double cost, double costFromStart)
        {
            ArgumentNullException.ThrowIfNull(path);

            this.Id = id;
            this.Content = content;
            this.Path = path;
            this.IndexInPath = indexInPath;
            this.Cost = cost;
            this.CostFromStart = costFromStart;
        }

        #endregion

        #region Equality

        /// <summary>
        /// Returns a value indicating whether this istance and a specific <see cref="PathNode{T}"/> rappresent the same node.
        /// </summary>
        /// <param name="other">Other <see cref="PathNode{T}"/> istance.</param>
        /// <returns>True if this and the other istance rappresent the same node.</returns>
        public bool Equals(PathNode<TContent> other)
        {
            if (other is null)
                return false;

            return this.Id.Equals(other.Id) &&
                this.Path.Equals(other.Path) &&
                this.IndexInPath.Equals(other.IndexInPath);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is null)
                return false;

            if (obj is not PathNode<TContent> other)
                return false;

            return this.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Path, this.IndexInPath);
        }

        #endregion
    }
}
