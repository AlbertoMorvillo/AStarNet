namespace AStarNet.TieBreakers;

/// <summary>
/// Resolves ties between candidate nodes during pathfinding.
/// </summary>
/// <remarks>
/// The pathfinder invokes the provider when two queued candidates have the same A* score or when equal-cost routes
/// offer different parent nodes. Implementations must provide a stable, antisymmetric, and transitive ordering for the
/// duration of each search.
/// </remarks>
public interface ITieBreakerProvider
{
    /// <summary>
    /// Resolves a tie between two candidate nodes.
    /// </summary>
    /// <param name="startNodeId">The identifier of the search's start node.</param>
    /// <param name="destinationNodeId">The identifier of the search's destination node.</param>
    /// <param name="leftCandidateNodeId">The identifier of the first candidate node.</param>
    /// <param name="rightCandidateNodeId">The identifier of the second candidate node.</param>
    /// <returns>
    /// A negative value when the left candidate has priority, zero when neither candidate is preferred, or a positive
    /// value when the right candidate has priority.
    /// </returns>
    int BreakTie(
        int startNodeId,
        int destinationNodeId,
        int leftCandidateNodeId,
        int rightCandidateNodeId);
}
