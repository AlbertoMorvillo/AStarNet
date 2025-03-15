// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections;
using System.Collections.Generic;

namespace AStarNet.Utils
{
    /// <summary>
    /// Provides a base class for comparing <see cref="Path{TId, TNode}"/> instances according to specific criteria.
    /// </summary>
    /// <remarks>
    /// Use <see cref="ByCost"/> or <see cref="ByNodeCount"/> to obtain predefined comparers.
    /// </remarks>
    /// <typeparam name="TId">The type of the identifier for the nodes in the path.</typeparam>
    /// <typeparam name="TNode">The type of the nodes in the path, implementing <see cref="IPathNode{TId}"/>.</typeparam>
    public abstract class PathComparer<TId, TNode> : IComparer, IEqualityComparer, IComparer<Path<TId, TNode>>, IEqualityComparer<Path<TId, TNode>>
        where TId : notnull, IEquatable<TId>
        where TNode : IPathNode<TId>
    {
        #region Properties

        /// <summary>
        /// Gets a <see cref="PathComparer{TId, TNode}"/> that compares paths based on their total cost.
        /// </summary>
        public static PathComparer<TId, TNode> ByCost => new PathCostComparer();

        /// <summary>
        /// Gets a <see cref="PathComparer{TId, TNode}"/> that compares paths based on their number of nodes.
        /// </summary>
        public static PathComparer<TId, TNode> ByNodeCount => new PathNodeCountComparer();

        #endregion

        #region IComparer and IEqualityComparer

        /// <inheritdoc />
        public int Compare(object? x, object? y)
        {
            if (x is Path<TId, TNode> xPath && y is Path<TId, TNode> yPath)
            {
                return this.Compare(xPath, yPath);
            }

            if (x is null && y is null)
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            throw new ArgumentException($"Both parameters must be of type {nameof(Path<TId, TNode>)}.", nameof(x));
        }

        /// <inheritdoc />
        public new bool Equals(object? x, object? y)
        {
            if (x is Path<TId, TNode> xPath && y is Path<TId, TNode> yPath)
            {
                return this.Equals(xPath, yPath);
            }

            return false;
        }

        /// <inheritdoc />
        public int GetHashCode(object obj)
        {
            if (obj is Path<TId, TNode> path)
            {
                return this.GetHashCode(path);
            }

            throw new ArgumentException($"Object must be of type {nameof(Path<TId, TNode>)}.", nameof(obj));
        }

        #endregion

        #region Abstract IComparer<T> and IEqualityComparer<T>

        /// <inheritdoc />
        public abstract int Compare(Path<TId, TNode>? x, Path<TId, TNode>? y);

        /// <inheritdoc />
        public abstract bool Equals(Path<TId, TNode>? x, Path<TId, TNode>? y);

        /// <inheritdoc />
        public abstract int GetHashCode(Path<TId, TNode> obj);

        #endregion

        #region Derived classes

        /// <summary>
        /// Compares two <see cref="Path{TId, TNode}"/> instances based on their total cost.
        /// </summary>
        private sealed class PathCostComparer : PathComparer<TId, TNode>
        {
            /// <inheritdoc />
            public override int Compare(Path<TId, TNode>? x, Path<TId, TNode>? y)
            {
                if (x is null)
                    return y is null ? 0 : -1;

                if (y is null)
                    return 1;

                int costComparison = x.Cost.CompareTo(y.Cost);

                return costComparison;
            }

            /// <inheritdoc />
            public override bool Equals(Path<TId, TNode>? x, Path<TId, TNode>? y)
            {
                if (x is null || y is null)
                    return x is null && y is null;

                return x.Cost.Equals(y.Cost);
            }

            /// <inheritdoc />
            public override int GetHashCode(Path<TId, TNode> obj)
            {
                ArgumentNullException.ThrowIfNull(obj);

                return obj.Cost.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two <see cref="Path{TId, TNode}"/> instances based on the number of nodes they contain.
        /// </summary>
        private sealed class PathNodeCountComparer : PathComparer<TId, TNode>
        {
            /// <inheritdoc />
            public override int Compare(Path<TId, TNode>? x, Path<TId, TNode>? y)
            {
                if (x is null)
                    return y is null ? 0 : -1;

                if (y is null)
                    return 1;

                int lengthComparison = x.Nodes.Count.CompareTo(y.Nodes.Count);

                return lengthComparison;
            }

            /// <inheritdoc />
            public override bool Equals(Path<TId, TNode>? x, Path<TId, TNode>? y)
            {
                if (x is null || y is null)
                    return x is null && y is null;

                return x.Nodes.Count.Equals(y.Nodes.Count);
            }

            /// <inheritdoc />
            public override int GetHashCode(Path<TId, TNode> obj)
            {
                ArgumentNullException.ThrowIfNull(obj);

                return obj.Nodes.Count.GetHashCode();
            }
        }

        #endregion
    }

}
