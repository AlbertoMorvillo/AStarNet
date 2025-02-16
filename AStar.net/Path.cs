// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;

namespace AStarNet
{
    /// <summary>
    /// Contains the node sequence which defines the path, sorted from start to destination node.
    /// </summary>
    /// <typeparam name="TContent">The type of the node content.</typeparam>
    public class Path<TContent> : IComparable<Path<TContent>>, IEquatable<Path<TContent>>
    {
        #region Properties

        /// <summary>
        /// Gets the identifier for this path.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the nodes in this path.
        /// </summary>
        public IReadOnlyList<PathNode<TContent>> Nodes { get; }

        /// <summary>
        /// Gets the cost of this path.
        /// </summary>
        public double Cost { get; }

        /// <summary>
        /// Gets or sets the generic tag of this path.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Returns an empty path.
        /// </summary>
        public static Path<TContent> Empty { get; } = new Path<TContent>(Guid.Empty);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Path{T}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the path, represented by a <see cref="Guid"/>.</param>
        public Path(Guid id)
        {
            this.Id = id;
            this.Nodes = [];
            this.Cost = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Path{T}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the path, represented by a <see cref="Guid"/>.</param>
        /// <param name="nodes">An ordered collection of <see cref="INode{T}"/> representing the nodes in this path, from start to destination.</param>
        /// <exception cref="ArgumentNullException"><paramref name="nodes"/> is <see langword="null"/>.</exception>
        public Path(Guid id, IEnumerable<INode<TContent>> nodes)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            List<PathNode<TContent>> pathNodes = [];
            int i = 0;
            double costFromStart = 0.0;

            this.Id = id;

            foreach(INode<TContent> node in nodes)
            {
                costFromStart += node.Cost;
                PathNode<TContent> pathNode = new(node.Id, node.Content, this, i, node.Cost, costFromStart);
                pathNodes.Add(pathNode);

                i++;
            }

            this.Nodes = [.. pathNodes];
            this.Cost = this.Nodes.Count > 0 ? this.Nodes[^1].CostFromStart : 0;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Creates a new <see cref="Path{T}"/> representing the concatenation of this path and the specified path.
        /// </summary>
        /// <param name="other">The path to append to this path.</param>
        /// <returns>A new <see cref="Path{T}"/> representing the combined path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
        public Path<TContent> Concat(Path<TContent> other)
        {
            ArgumentNullException.ThrowIfNull(other);

            IEnumerable<PathNode<TContent>> combineNodes = this.Nodes.Concat(other.Nodes);

            Path<TContent> newPath = new(Guid.NewGuid(), combineNodes);
            return newPath;
        }

        /// <summary>
        /// Creates a new <see cref="Path{T}"/> representing the concatenation of two specified paths.
        /// </summary>
        /// <param name="path1">The first path.</param>
        /// <param name="path2">The second path to append to the first path.</param>
        /// <returns>A new <see cref="Path{T}"/> representing the combined path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path1"/> or <paramref name="path2"/> is <see langword="null"/>.</exception>
        public static Path<TContent> Concat(Path<TContent> path1, Path<TContent> path2)
        {
            return Path<TContent>.Concat((IEnumerable<Path<TContent>>)[path1, path2]);
        }

        /// <summary>
        /// Creates a new <see cref="Path{T}"/> representing the concatenation of multiple specified paths.
        /// </summary>
        /// <param name="paths">An array containing the paths to concatenate.</param>
        /// <returns>A new <see cref="Path{T}"/> representing the combined path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/>.</exception>
        public static Path<TContent> Concat(params Path<TContent>[] paths)
        {
            return Path<TContent>.Concat((IEnumerable<Path<TContent>>)paths);
        }

        /// <summary>
        /// Creates a new <see cref="Path{T}"/> representing the concatenation of a sequence of paths.
        /// </summary>
        /// <param name="paths">The sequence of paths to concatenate.</param>
        /// <returns>A new <see cref="Path{T}"/> representing the combined path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/>.</exception>
        public static Path<TContent> Concat(IEnumerable<Path<TContent>> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);

            IEnumerable<PathNode<TContent>> combinedNodes = paths.SelectMany(p => p.Nodes);

            Path<TContent> newPath = new(Guid.NewGuid(), combinedNodes);
            return newPath;
        }

        #endregion

        #region Equality and comparison

        /// <summary>
        /// Returns a value indicating whether this istance and a specific <see cref="Path{T}"/> rappresent the same path.
        /// </summary>
        /// <param name="other">Other <see cref="Path{T}"/> istance.</param>
        /// <returns>True if this and the other istance rappresent the same path.</returns>
        public bool Equals(Path<TContent> other)
        {
            if (other is null)
                return false;

            if (this.CompareTo(other) != 0)
                return false;

            return this.Nodes.SequenceEqual(other.Nodes);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is null)
                return false;

            if (obj is not Path<TContent> other)
                return false;

            return this.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(this.Cost);

            foreach (PathNode<TContent> node in this.Nodes)
            {
                hash.Add(node.Id);
            }

            return hash.ToHashCode();
        }

        /// <summary>
        /// Compares first the cost then the length of this path with another one.
        /// </summary>
        /// <param name="other">Other <see cref="Path{T}"/> to compare.</param>
        /// <returns>
        /// <para>Less than zero: This path has the cost less than other path or the cost equal and the length less than other path.</para>
        /// <para>Zero: This path has the cost and the length equal to other node.</para>
        /// <para>Greater than zero: This path has the cost greater than other node or the cost equal and the length greater than the other path.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
        public int CompareTo(Path<TContent> other)
        {
            ArgumentNullException.ThrowIfNull(other);

            int lengthCompare = this.Nodes.Count.CompareTo(other.Nodes.Count);

            if (lengthCompare != 0)
            {
                // Length not equals: return the path length comparison
                return lengthCompare;
            }

            // Length equals: return the path cost comparison
            return this.Cost.CompareTo(other.Cost);
        }

        /// <inheritdoc/>
        public static bool operator ==(Path<TContent> left, Path<TContent> right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Path<TContent> left, Path<TContent> right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static bool operator <(Path<TContent> left, Path<TContent> right)
        {
            return left is null ? right is not null : left.CompareTo(right) < 0;
        }

        /// <inheritdoc/>
        public static bool operator <=(Path<TContent> left, Path<TContent> right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        /// <inheritdoc/>
        public static bool operator >(Path<TContent> left, Path<TContent> right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

        /// <inheritdoc/>
        public static bool operator >=(Path<TContent> left, Path<TContent> right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        #endregion
    }
}
