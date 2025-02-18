// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System.Collections.Generic;

namespace AStarNet.Comparers
{
    /// <summary>
    /// Compares two <see cref="Path{TContent}"/> instances based on their total cost.
    /// </summary>
    /// <typeparam name="TContent">The type of content associated with the path nodes.</typeparam>
    public class PathCostComparer<TContent> : IComparer<Path<TContent>>
    {
        /// <summary>
        /// Compares two <see cref="Path{TContent}"/> instances based on their total cost.
        /// </summary>
        /// <param name="x">The first path to compare.</param>
        /// <param name="y">The second path to compare.</param>
        /// <returns>
        /// A negative value if <paramref name="x"/> has a lower cost than <paramref name="y"/>.
        /// Zero if the costs are equal.
        /// A positive value if <paramref name="x"/> has a higher cost than <paramref name="y"/>.
        /// </returns>
        /// <remarks>
        /// If either <paramref name="x"/> or <paramref name="y"/> is <see langword="null"/>,
        /// <see langword="null"/> is treated as being less than a non-null path.
        /// </remarks>
        public int Compare(Path<TContent> x, Path<TContent> y)
        {
            if (x is null)
                return y is null ? 0 : -1;

            if (y is null)
                return 1;

            int costComparison = x.Cost.CompareTo(y.Cost);

            return costComparison;
        }
    }
}
