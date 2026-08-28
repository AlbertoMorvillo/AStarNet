using System;

namespace AStarNet;

/// <summary>
/// Represents a directed connection to a destination node and its traversal cost.
/// </summary>
public readonly struct PathConnection
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PathConnection"/> struct.
    /// </summary>
    /// <param name="destinationNodeId">The destination-node identifier.</param>
    /// <param name="cost">The non-negative, finite traversal cost.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cost"/> is negative, infinite, or not a number.</exception>
    public PathConnection(int destinationNodeId, double cost)
    {
        if (!double.IsFinite(cost) || cost < 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "Connection cost must be finite and non-negative.");

        this.DestinationNodeId = destinationNodeId;
        this.Cost = cost;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the destination-node identifier.
    /// </summary>
    public int DestinationNodeId { get; }

    /// <summary>
    /// Gets the traversal cost of the connection.
    /// </summary>
    public double Cost { get; }

    #endregion
}
