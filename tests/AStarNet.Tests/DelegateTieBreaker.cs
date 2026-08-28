using AStarNet.TieBreakers;

namespace AStarNet.Tests;

/// <summary>
/// Provides delegate-backed tie-breaking for focused tests.
/// </summary>
internal sealed class DelegateTieBreaker : ITieBreakerProvider
{
    private readonly Func<int, int, int, int, int> _breakTie;

    /// <summary>
    /// Initializes a delegate-backed tie-breaker provider.
    /// </summary>
    /// <param name="breakTie">The tie-breaking operation.</param>
    internal DelegateTieBreaker(Func<int, int, int, int, int> breakTie)
    {
        this._breakTie = breakTie;
    }

    /// <inheritdoc/>
    public int BreakTie(
        int startNodeId,
        int destinationNodeId,
        int leftCandidateNodeId,
        int rightCandidateNodeId)
    {
        return this._breakTie(
            startNodeId,
            destinationNodeId,
            leftCandidateNodeId,
            rightCandidateNodeId);
    }
}
