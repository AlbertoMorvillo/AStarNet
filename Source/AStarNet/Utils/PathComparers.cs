using System.Collections.Generic;

namespace AStarNet.Utils;

/// <summary>
/// Provides predefined comparers for ordering <see cref="Path{TContent}"/> instances.
/// </summary>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public static class PathComparers<TContent>
{
    #region Properties

    /// <summary>
    /// Gets the shared comparer that orders paths by total traversal cost.
    /// </summary>
    public static IComparer<Path<TContent>> ByCost { get; } = new CostComparer();

    /// <summary>
    /// Gets the shared comparer that orders paths by number of nodes.
    /// </summary>
    public static IComparer<Path<TContent>> ByNodeCount { get; } = new NodeCountComparer();

    #endregion

    #region Nested types

    /// <summary>
    /// Compares paths by their total traversal cost.
    /// </summary>
    private sealed class CostComparer : IComparer<Path<TContent>>
    {
        /// <inheritdoc/>
        public int Compare(Path<TContent>? x, Path<TContent>? y)
        {
            if (x is null)
                return y is null ? 0 : -1;

            return y is null ? 1 : x.Cost.CompareTo(y.Cost);
        }
    }

    /// <summary>
    /// Compares paths by their number of nodes.
    /// </summary>
    private sealed class NodeCountComparer : IComparer<Path<TContent>>
    {
        /// <inheritdoc/>
        public int Compare(Path<TContent>? x, Path<TContent>? y)
        {
            if (x is null)
                return y is null ? 0 : -1;

            return y is null ? 1 : x.Count.CompareTo(y.Count);
        }
    }

    #endregion
}
