using AStarNet.TieBreakers;

namespace AStarNet.ConsoleDemo.PathFinding.TieBreakers;

/// <summary>
/// Prefers candidate nodes that remain closest to the line between the search endpoints.
/// </summary>
internal sealed class LineDeviationTieBreaker : ITieBreakerProvider
{
    private readonly MatrixMap _map;

    /// <summary>
    /// Initializes a new instance of the <see cref="LineDeviationTieBreaker"/> class.
    /// </summary>
    /// <param name="map">The grid that defines node positions.</param>
    public LineDeviationTieBreaker(MatrixMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        this._map = map;
    }

    /// <inheritdoc/>
    public int BreakTie(
        int startNodeId,
        int destinationNodeId,
        int leftCandidateNodeId,
        int rightCandidateNodeId)
    {
        GridPosition start = this._map.GetPosition(startNodeId);
        GridPosition destination = this._map.GetPosition(destinationNodeId);
        GridPosition leftCandidate = this._map.GetPosition(leftCandidateNodeId);
        GridPosition rightCandidate = this._map.GetPosition(rightCandidateNodeId);

        double leftDeviation = TieBreakerMath.SquaredLineDeviation2D(
            start.X,
            start.Y,
            destination.X,
            destination.Y,
            leftCandidate.X,
            leftCandidate.Y);
        double rightDeviation = TieBreakerMath.SquaredLineDeviation2D(
            start.X,
            start.Y,
            destination.X,
            destination.Y,
            rightCandidate.X,
            rightCandidate.Y);

        return leftDeviation.CompareTo(rightDeviation);
    }
}
