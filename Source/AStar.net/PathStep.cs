// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

namespace AStarNet;

/// <summary>
/// Represents one node in a computed path together with its traversal costs.
/// </summary>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public readonly struct PathStep<TContent>
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PathStep{TContent}"/> struct.
    /// </summary>
    /// <param name="node">The node represented by this step.</param>
    /// <param name="costFromPrevious">The traversal cost from the previous step.</param>
    /// <param name="costFromStart">The accumulated traversal cost from the start.</param>
    internal PathStep(PathNode<TContent> node, double costFromPrevious, double costFromStart)
    {
        this.Node = node;
        this.CostFromPrevious = costFromPrevious;
        this.CostFromStart = costFromStart;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the node represented by this step.
    /// </summary>
    public PathNode<TContent> Node { get; }

    /// <summary>
    /// Gets the traversal cost from the previous step, or zero for the first step.
    /// </summary>
    public double CostFromPrevious { get; }

    /// <summary>
    /// Gets the accumulated traversal cost from the start of the path.
    /// </summary>
    public double CostFromStart { get; }

    #endregion
}
