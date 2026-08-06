using System;

namespace AStarNet;

/// <summary>
/// Represents a directed connection to a destination node and its traversal cost.
/// </summary>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public readonly struct PathConnection<TContent>
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PathConnection{TContent}"/> struct.
    /// </summary>
    /// <param name="destination">The destination node.</param>
    /// <param name="cost">The non-negative, finite traversal cost.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cost"/> is negative, infinite, or not a number.</exception>
    public PathConnection(PathNode<TContent> destination, double cost)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!double.IsFinite(cost) || cost < 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "Connection cost must be finite and non-negative.");

        this.Destination = destination;
        this.Cost = cost;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the destination node.
    /// </summary>
    public PathNode<TContent> Destination { get; }

    /// <summary>
    /// Gets the traversal cost of the connection.
    /// </summary>
    public double Cost { get; }

    #endregion
}
