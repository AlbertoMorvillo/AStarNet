using System;

namespace AStarNet;

/// <summary>
/// Represents one node in a computed path together with its traversal costs.
/// </summary>
public readonly struct PathStep : IEquatable<PathStep>
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PathStep"/> struct.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="costFromPrevious">The traversal cost from the previous step.</param>
    /// <param name="costFromStart">The accumulated traversal cost from the start.</param>
    internal PathStep(int nodeId, double costFromPrevious, double costFromStart)
    {
        this.NodeId = nodeId;
        this.CostFromPrevious = costFromPrevious;
        this.CostFromStart = costFromStart;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the node identifier represented by this step.
    /// </summary>
    public int NodeId { get; }

    /// <summary>
    /// Gets the traversal cost from the previous step, or zero for the first step.
    /// </summary>
    public double CostFromPrevious { get; }

    /// <summary>
    /// Gets the accumulated traversal cost from the start of the path.
    /// </summary>
    public double CostFromStart { get; }

    #endregion

    #region Operators

    /// <inheritdoc/>
    public static bool operator ==(PathStep left, PathStep right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(PathStep left, PathStep right)
    {
        return !left.Equals(right);
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public bool Equals(PathStep other)
    {
        return this.NodeId == other.NodeId
            && this.CostFromPrevious.Equals(other.CostFromPrevious)
            && this.CostFromStart.Equals(other.CostFromStart);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PathStep other && this.Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.NodeId, this.CostFromPrevious, this.CostFromStart);
    }

    #endregion
}
