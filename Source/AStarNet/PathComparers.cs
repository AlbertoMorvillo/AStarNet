using System;
using System.Collections.Generic;

namespace AStarNet;

/// <summary>
/// Provides predefined comparers for ordering <see cref="Path"/> instances.
/// </summary>
public static class PathComparers
{
    #region Properties

    /// <summary>
    /// Gets the shared comparer that orders paths by total traversal cost, then node count. Remaining ties are resolved
    /// lexicographically by the traversal cost of each step and finally by each node identifier.
    /// </summary>
    /// <remarks>Completely equal paths compare as equal; otherwise, the ordering is deterministic.</remarks>
    public static IComparer<Path> ByCost { get; } = new CostComparer();

    /// <summary>
    /// Gets the shared comparer that orders paths by node count, then total traversal cost. Remaining ties are resolved
    /// lexicographically by the traversal cost of each step and finally by each node identifier.
    /// </summary>
    /// <remarks>Completely equal paths compare as equal; otherwise, the ordering is deterministic.</remarks>
    public static IComparer<Path> ByNodeCount { get; } = new NodeCountComparer();

    #endregion

    #region Private methods

    /// <summary>
    /// Resolves a comparison tie by examining individual connection costs and then node identifiers in path order.
    /// </summary>
    /// <param name="x">The first path.</param>
    /// <param name="y">The second path.</param>
    /// <returns>A value indicating the relative order of the paths.</returns>
    private static int CompareStepDetails(Path x, Path y)
    {
        for (int index = 0; index < x.Steps.Length; index++)
        {
            int comparison = x.Steps[index].CostFromPrevious.CompareTo(y.Steps[index].CostFromPrevious);

            if (comparison != 0)
                return comparison;
        }

        for (int index = 0; index < x.Steps.Length; index++)
        {
            int comparison = x.Steps[index].NodeId.CompareTo(y.Steps[index].NodeId);

            if (comparison != 0)
                return comparison;
        }

        return 0;
    }

    #endregion

    #region Nested types

    /// <summary>
    /// Compares paths by their total traversal cost.
    /// </summary>
    private sealed class CostComparer : IComparer<Path>
    {
        /// <inheritdoc/>
        public int Compare(Path? x, Path? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x is null)
                return -1;

            if (y is null)
                return 1;

            int comparison = x.Cost.CompareTo(y.Cost);

            if (comparison != 0)
                return comparison;

            comparison = x.Steps.Length.CompareTo(y.Steps.Length);

            return comparison != 0
                ? comparison
                : PathComparers.CompareStepDetails(x, y);
        }
    }

    /// <summary>
    /// Compares paths by their number of nodes.
    /// </summary>
    private sealed class NodeCountComparer : IComparer<Path>
    {
        /// <inheritdoc/>
        public int Compare(Path? x, Path? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x is null)
                return -1;

            if (y is null)
                return 1;

            int comparison = x.Steps.Length.CompareTo(y.Steps.Length);

            if (comparison != 0)
                return comparison;

            comparison = x.Cost.CompareTo(y.Cost);

            return comparison != 0
                ? comparison
                : PathComparers.CompareStepDetails(x, y);
        }
    }

    #endregion
}
