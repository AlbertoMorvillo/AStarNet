using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AStarNet.Comparers
{
    /// <summary>
    /// Provides a base class for comparing <see cref="Path{TContent}"/> instances according to specific criteria.
    /// </summary>
    /// <remarks>
    /// Use <see cref="ByCost"/> or <see cref="ByNodeCount"/> to obtain predefined comparers.
    /// </remarks>
    /// <typeparam name="TContent">
    /// The type of content associated with each node in the path.
    /// </typeparam>
    public abstract class PathComparer<TContent> : IComparer, IEqualityComparer, IComparer<Path<TContent>>, IEqualityComparer<Path<TContent>>
    {
        #region Properties

        /// <summary>
        /// Gets a <see cref="PathComparer{TContent}"/> that compares paths based on their total cost.
        /// </summary>
        public static PathComparer<TContent> ByCost => new PathCostComparer();

        /// <summary>
        /// Gets a <see cref="PathComparer{TContent}"/> that compares paths based on their number of nodes.
        /// </summary>
        public static PathComparer<TContent> ByNodeCount => new PathNodeCountComparer();

        #endregion

        #region IComparer and IEqualityComparer

        /// <inheritdoc />
        public int Compare(object? x, object? y)
        {
            if (x is Path<TContent> xPath && y is Path<TContent> yPath)
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

            throw new ArgumentException($"Both parameters must be of type {nameof(Path<TContent>)}.", nameof(x));
        }

        /// <inheritdoc />
        public new bool Equals(object? x, object? y)
        {
            if (x is Path<TContent> xPath && y is Path<TContent> yPath)
            {
                return this.Equals(xPath, yPath);
            }

            return false;
        }

        /// <inheritdoc />
        public int GetHashCode(object obj)
        {
            if (obj is Path<TContent> path)
            {
                return this.GetHashCode(path);
            }

            throw new ArgumentException($"Object must be of type {nameof(Path<TContent>)}.", nameof(obj));
        }

        #endregion

        #region Abstract IComparer<T> and IEqualityComparer<T>
        /// <inheritdoc />
        public abstract int Compare(Path<TContent>? x, Path<TContent>? y);

        /// <inheritdoc />
        public abstract bool Equals(Path<TContent>? x, Path<TContent>? y);

        /// <inheritdoc />
        public abstract int GetHashCode(Path<TContent> obj);

        #endregion

        #region Derived classes

        /// <summary>
        /// Compares two <see cref="Path{TContent}"/> instances based on their total cost.
        /// </summary>
        private sealed class PathCostComparer: PathComparer<TContent>
        {
            /// <inheritdoc />
            public override int Compare(Path<TContent>? x, Path<TContent>? y)
            {
                if (x is null)
                    return y is null ? 0 : -1;

                if (y is null)
                    return 1;

                int costComparison = x.Cost.CompareTo(y.Cost);

                return costComparison;
            }

            /// <inheritdoc />
            public override bool Equals(Path<TContent>? x, Path<TContent>? y)
            {
                if (x is null || y is null)
                    return x is null && y is null;

                return x.Cost.Equals(y.Cost);
            }

            /// <inheritdoc />
            public override int GetHashCode(Path<TContent> obj)
            {
                ArgumentNullException.ThrowIfNull(obj);

                return obj.Cost.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two <see cref="Path{TContent}"/> instances based on the number of nodes they contain.
        /// </summary>
        private sealed class PathNodeCountComparer : PathComparer<TContent>
        {
            /// <inheritdoc />
            public override int Compare(Path<TContent>? x, Path<TContent>? y)
            {
                if (x is null)
                    return y is null ? 0 : -1;

                if (y is null)
                    return 1;

                int lengthComparison = x.Nodes.Count.CompareTo(y.Nodes.Count);

                return lengthComparison;
            }

            /// <inheritdoc />
            public override bool Equals(Path<TContent>? x, Path<TContent>? y)
            {
                if (x is null || y is null)
                    return x is null && y is null;

                return x.Nodes.Count.Equals(y.Nodes.Count);
            }

            /// <inheritdoc />
            public override int GetHashCode(Path<TContent> obj)
            {
                ArgumentNullException.ThrowIfNull(obj);

                return obj.Nodes.Count.GetHashCode();
            }
        }

        #endregion
    }

}
