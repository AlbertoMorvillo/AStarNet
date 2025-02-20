// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections;
using System.Collections.Generic;

namespace AStarNet.Utils
{
    /// <summary>
    /// Provides a base class for comparing <see cref="Path{TId}"/> instances according to specific criteria.
    /// </summary>
    /// <remarks>
    /// Use <see cref="ByCost"/> or <see cref="ByNodeCount"/> to obtain predefined comparers.
    /// </remarks>
    /// <typeparam name="TId">The type of the identifier for the nodes in the path.</typeparam>
    public abstract class PathComparer<TId> : IComparer, IEqualityComparer, IComparer<Path<TId>>, IEqualityComparer<Path<TId>> where TId : notnull
    {
        #region Properties

        /// <summary>
        /// Gets a <see cref="PathComparer{TId}"/> that compares paths based on their total cost.
        /// </summary>
        public static PathComparer<TId> ByCost => new PathCostComparer();

        /// <summary>
        /// Gets a <see cref="PathComparer{TId}"/> that compares paths based on their number of nodes.
        /// </summary>
        public static PathComparer<TId> ByNodeCount => new PathNodeCountComparer();

        #endregion

        #region IComparer and IEqualityComparer

        /// <inheritdoc />
        public int Compare(object? x, object? y)
        {
            if (x is Path<TId> xPath && y is Path<TId> yPath)
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

            throw new ArgumentException($"Both parameters must be of type {nameof(Path<TId>)}.", nameof(x));
        }

        /// <inheritdoc />
        public new bool Equals(object? x, object? y)
        {
            if (x is Path<TId> xPath && y is Path<TId> yPath)
            {
                return this.Equals(xPath, yPath);
            }

            return false;
        }

        /// <inheritdoc />
        public int GetHashCode(object obj)
        {
            if (obj is Path<TId> path)
            {
                return this.GetHashCode(path);
            }

            throw new ArgumentException($"Object must be of type {nameof(Path<TId>)}.", nameof(obj));
        }

        #endregion

        #region Abstract IComparer<T> and IEqualityComparer<T>

        /// <inheritdoc />
        public abstract int Compare(Path<TId>? x, Path<TId>? y);

        /// <inheritdoc />
        public abstract bool Equals(Path<TId>? x, Path<TId>? y);

        /// <inheritdoc />
        public abstract int GetHashCode(Path<TId> obj);

        #endregion

        #region Derived classes

        /// <summary>
        /// Compares two <see cref="Path{TId}"/> instances based on their total cost.
        /// </summary>
        private sealed class PathCostComparer: PathComparer<TId>
        {
            /// <inheritdoc />
            public override int Compare(Path<TId>? x, Path<TId>? y)
            {
                if (x is null)
                    return y is null ? 0 : -1;

                if (y is null)
                    return 1;

                int costComparison = x.Cost.CompareTo(y.Cost);

                return costComparison;
            }

            /// <inheritdoc />
            public override bool Equals(Path<TId>? x, Path<TId>? y)
            {
                if (x is null || y is null)
                    return x is null && y is null;

                return x.Cost.Equals(y.Cost);
            }

            /// <inheritdoc />
            public override int GetHashCode(Path<TId> obj)
            {
                ArgumentNullException.ThrowIfNull(obj);

                return obj.Cost.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two <see cref="Path{TId}"/> instances based on the number of nodes they contain.
        /// </summary>
        private sealed class PathNodeCountComparer : PathComparer<TId>
        {
            /// <inheritdoc />
            public override int Compare(Path<TId>? x, Path<TId>? y)
            {
                if (x is null)
                    return y is null ? 0 : -1;

                if (y is null)
                    return 1;

                int lengthComparison = x.Nodes.Count.CompareTo(y.Nodes.Count);

                return lengthComparison;
            }

            /// <inheritdoc />
            public override bool Equals(Path<TId>? x, Path<TId>? y)
            {
                if (x is null || y is null)
                    return x is null && y is null;

                return x.Nodes.Count.Equals(y.Nodes.Count);
            }

            /// <inheritdoc />
            public override int GetHashCode(Path<TId> obj)
            {
                ArgumentNullException.ThrowIfNull(obj);

                return obj.Nodes.Count.GetHashCode();
            }
        }

        #endregion
    }

}
