// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace AStarNet
{
    /// <summary>
    /// Contains the node sequence which defines the path, sorted from start to destination node.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier for the nodes in the path.</typeparam>
    /// <typeparam name="TNode">The type of the nodes in the path, implementing <see cref="IPathNode{TId}"/>.</typeparam>
    public class Path<TId, TNode> : IComparable, IComparable<Path<TId, TNode>>, IEquatable<Path<TId, TNode>>
        where TId : notnull, IEquatable<TId>
        where TNode : IPathNode<TId>
    {
        #region Fields

        /// <summary>
        /// Stores the precomputed hash code for the current path instance.
        /// </summary>
        /// <remarks>
        /// The hash code is computed once during object construction and remains immutable throughout the lifetime of the instance.
        /// This improves performance when the hash code is accessed multiple times.
        /// </remarks>
        protected readonly int _precomputedHashCode;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the identifier for this path.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the nodes in this path.
        /// </summary>
        public IReadOnlyList<TNode> Nodes { get; }

        /// <summary>
        /// Gets the cost of this path.
        /// </summary>
        public double Cost { get; }

        /// <summary>
        /// Gets or sets the generic tag of this path.
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// Returns true if the path contains no nodes.
        /// </summary>
        public bool IsEmpty => this.Nodes.Count == 0;

        /// <summary>
        /// Returns an empty path.
        /// </summary>
        public static Path<TId, TNode> Empty { get; } = new Path<TId, TNode>(Guid.Empty);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Path{TId, TNode}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the path, represented by a <see cref="Guid"/>.</param>
        public Path(Guid id)
        {
            // Ensures this.Nodes is an ImmutableArray.
            ImmutableArray<TNode> nodeArray = [];

            this.Id = id;
            this.Nodes = nodeArray;
            this.Cost = 0;

            this._precomputedHashCode = this.GenerateHashCode();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Path{TId, TNode}"/> class.
        /// </summary>
        /// <param name="id">A unique identifier for this path, represented as a <see cref="Guid"/>.</param>
        /// <param name="nodes">An ordered collection of nodes forming the path from the starting point to the destination.</param>
        /// <exception cref="ArgumentNullException"><paramref name="nodes"/> is <see langword="null"/>.</exception>
        public Path(Guid id, IEnumerable<TNode> nodes)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            // Ensures this.Nodes is an ImmutableArray.
            ImmutableArray<TNode> nodeArray = [.. nodes];

            this.Id = id;
            this.Nodes = nodeArray;
            this.Cost = this.Nodes.Count > 0 ? this.GetCostAtIndex(this.Nodes.Count - 1) : 0;

            this._precomputedHashCode = this.GenerateHashCode();
        }

        #endregion

        #region Public methods

        #region Others

        /// <summary>
        /// Calculates the total accumulated cost from the start of the path to the node at the specified index.
        /// </summary>
        /// <param name="index">The index of the node in the path.</param>
        /// <returns>The total cost from the start of the path to the specified node.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to the number of nodes in the path.</exception>
        public double GetCostAtIndex(int index)
        {
            if (index < 0 || index >= this.Nodes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            double resultCost = 0;

            for (int i = 0; i <= index; i++)
            {
                resultCost += this.Nodes[i].Cost;
            }

            return resultCost;
        }

        #endregion

        #region Concat

        /// <summary>
        /// Creates a new <see cref="Path{TId, TNode}"/> representing the concatenation of this path and the specified path.
        /// </summary>
        /// <param name="other">The path to append to this path.</param>
        /// <returns>A new <see cref="Path{TId, TNode}"/> representing the combined path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
        public Path<TId, TNode> Concat(Path<TId, TNode> other)
        {
            ArgumentNullException.ThrowIfNull(other);

            IEnumerable<TNode> combineNodes = this.Nodes.Concat(other.Nodes);

            Path<TId, TNode> newPath = new(Guid.NewGuid(), combineNodes);
            return newPath;
        }

        /// <summary>
        /// Creates a new <see cref="Path{TId, TNode}"/> representing the concatenation of two specified paths.
        /// </summary>
        /// <param name="path1">The first path.</param>
        /// <param name="path2">The second path to append to the first path.</param>
        /// <returns>A new <see cref="Path{TId, TNode}"/> representing the combined path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path1"/> or <paramref name="path2"/> is <see langword="null"/>.</exception>
        public static Path<TId, TNode> Concat(Path<TId, TNode> path1, Path<TId, TNode> path2)
        {
            return Path<TId, TNode>.Concat((IEnumerable<Path<TId, TNode>>)[path1, path2]);
        }

        /// <summary>
        /// Creates a new <see cref="Path{TId, TNode}"/> representing the concatenation of multiple specified paths.
        /// </summary>
        /// <param name="paths">An array containing the paths to concatenate.</param>
        /// <returns>A new <see cref="Path{TId, TNode}"/> representing the combined path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/>.</exception>
        public static Path<TId, TNode> Concat(params Path<TId, TNode>[] paths)
        {
            return Path<TId, TNode>.Concat((IEnumerable<Path<TId, TNode>>)paths);
        }

        /// <summary>
        /// Creates a new <see cref="Path{TId, TNode}"/> representing the concatenation of a sequence of paths.
        /// </summary>
        /// <param name="paths">The sequence of paths to concatenate.</param>
        /// <returns>A new <see cref="Path{TId, TNode}"/> representing the combined path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/>.</exception>
        public static Path<TId, TNode> Concat(IEnumerable<Path<TId, TNode>> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);

            IEnumerable<TNode> combinedNodes = paths.SelectMany(p => p.Nodes);

            Path<TId, TNode> newPath = new(Guid.NewGuid(), combinedNodes);
            return newPath;
        }

        #endregion

        #region Equality and comparison

        /// <summary>
        /// Compares first the cost then the number of nodes of this path with another one.
        /// </summary>
        /// <param name="other">The other <see cref="Path{TId, TNode}"/> to compare with the current path.</param>
        /// <returns>
        /// <para>Less than zero: This path has the cost less than other path or the cost equal and fewer nodes than other path.</para>
        /// <para>Zero: This path has the cost and the length equal to other node.</para>
        /// <para>Greater than zero: This path has the cost greater than other node or the cost equal and more nodes than the other path.</para>
        /// </returns>
        public int CompareTo(Path<TId, TNode>? other)
        {
            if (other is null)
                return 1;

            int costCompare = this.Cost.CompareTo(other.Cost);

            if (costCompare != 0)
            {
                // Cost not equals: return the path cost comparison.
                return costCompare;
            }

            // Cost equals: return the path node count comparison.
            return this.Nodes.Count.CompareTo(other.Nodes.Count);
        }

        /// <inheritdoc/>
        public int CompareTo(object? obj)
        {
            if (obj is null)
                return 1;

            if (obj is not Path<TId, TNode> other)
            {
                throw new ArgumentException($"Object must be of type {nameof(Path<TId, TNode>)}.", nameof(obj));
            }

            return this.CompareTo(other);
        }

        /// <summary>
        /// Compares two <see cref="Path{TId, TNode}"/> instances.
        /// </summary>
        /// <param name="x">The first path to compare.</param>
        /// <param name="y">The second path to compare.</param>
        /// <returns>
        /// A negative value if <paramref name="x"/> is less than <paramref name="y"/>.
        /// Zero if they are equal.
        /// A positive value if <paramref name="x"/> is greater than <paramref name="y"/>.
        /// </returns>
        public static int Compare(Path<TId, TNode>? x, Path<TId, TNode>? y)
        {
            if (x is null)
                return y is null ? 0 : -1;

            if (y is null)
                return 1;

            return x.CompareTo(y);
        }

        /// <summary>
        /// Returns a value indicating whether this istance and a specific <see cref="Path{TId, TNode}"/> rappresent the same path.
        /// </summary>
        /// <param name="other">The other <see cref="Path{TId, TNode}"/> compare with the current path.</param>
        /// <returns>True if this and the other istance rappresent the same path.</returns>
        public bool Equals(Path<TId, TNode>? other)
        {
            if (other is null)
                return false;

            if (this.CompareTo(other) != 0)
                return false;

            return this.Nodes.SequenceEqual(other.Nodes);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is null)
                return false;

            if (obj is not Path<TId, TNode> other)
                return false;

            return this.Equals(other);
        }

        /// <summary>
        /// Determines whether two <see cref="Path{TId, TNode}"/> instances are equal.
        /// </summary>
        /// <param name="x">The first path to compare.</param>
        /// <param name="y">The second path to compare.</param>
        /// <returns>
        /// <see langword="true"/> if both paths are equal; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool Equals(Path<TId, TNode>? x, Path<TId, TNode>? y)
        {
            if (x is null || y is null)
                return x is null && y is null;

            return x.Equals(y);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this._precomputedHashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(Path<TId, TNode> left, Path<TId, TNode>? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Path<TId, TNode> left, Path<TId, TNode>? right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static bool operator <(Path<TId, TNode>? left, Path<TId, TNode>? right)
        {
            return left is null ? right is not null : left.CompareTo(right) < 0;
        }

        /// <inheritdoc/>
        public static bool operator <=(Path<TId, TNode>? left, Path<TId, TNode>? right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        /// <inheritdoc/>
        public static bool operator >(Path<TId, TNode>? left, Path<TId, TNode>? right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

        /// <inheritdoc/>
        public static bool operator >=(Path<TId, TNode>? left, Path<TId, TNode>? right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        #endregion

        #endregion

        #region Protected methods

        /// <summary>
        /// Generates the hash code for the current path instance by combining the total cost and the identifiers of the nodes.
        /// </summary>
        /// <returns>The computed hash code as an <see cref="int"/>.</returns>
        /// <remarks>
        /// The hash is computed using the total path cost and the node identifiers, preserving their order.
        /// This method is intended to be used within the constructor and stored, as the path is immutable.
        /// </remarks>
        protected int GenerateHashCode()
        {
            HashCode hash = new();
            hash.Add(this.Cost);

            foreach (var node in this.Nodes)
            {
                hash.Add(node.Id);
            }

            return hash.ToHashCode();
        }

        #endregion
    }
}
