using AStarNet.Heuristics;

namespace AStarNet.Tests;

/// <summary>
/// Provides a delegate-backed heuristic for focused tests.
/// </summary>
internal sealed class DelegateHeuristic : IHeuristicProvider
{
    private readonly Func<int, int, double> _getHeuristic;

    /// <summary>
    /// Initializes a delegate-backed heuristic.
    /// </summary>
    /// <param name="getHeuristic">The heuristic calculation.</param>
    internal DelegateHeuristic(Func<int, int, double> getHeuristic)
    {
        this._getHeuristic = getHeuristic;
    }

    /// <inheritdoc/>
    public double GetHeuristic(int fromNodeId, int toNodeId)
    {
        return this._getHeuristic(fromNodeId, toNodeId);
    }
}
