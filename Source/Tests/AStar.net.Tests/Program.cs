// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using AStarNet;
using AStarNet.Maps;

namespace AStarNet.Tests;

/// <summary>
/// Runs dependency-free regression tests for the library.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Runs all regression tests.
    /// </summary>
    private static void Main()
    {
        Program.VerifyOptionalContent();
        Program.VerifyNodeIdentity();
        Program.VerifyConnectionValidation();
        Program.VerifyImprovedRouteAndTerminalDestination();
        Program.VerifySameStartAndDestination();
        Program.VerifyPathConcatenation();
        Program.VerifyCancellation();

        Console.WriteLine("All regression tests passed.");
    }

    /// <summary>
    /// Verifies that node content may be omitted.
    /// </summary>
    private static void VerifyOptionalContent()
    {
        PathNode<string> node = new(1);

        Program.Assert(node.Content is null, "Node content should be optional.");
    }

    /// <summary>
    /// Verifies that integer identifiers define node identity.
    /// </summary>
    private static void VerifyNodeIdentity()
    {
        PathNode<string> first = new(1, "first");
        PathNode<string> second = new(1, "second");

        Program.Assert(first == second, "Nodes with the same identifier should be equal.");
        Program.Assert(first.GetHashCode() == second.GetHashCode(), "Equal nodes should have equal hash codes.");
    }

    /// <summary>
    /// Verifies that invalid connection costs are rejected.
    /// </summary>
    private static void VerifyConnectionValidation()
    {
        PathNode<string> destination = new(1);

        Program.AssertThrows<ArgumentOutOfRangeException>(
            () => _ = new PathConnection<string>(destination, -1),
            "Negative connection costs should be rejected.");
        Program.AssertThrows<ArgumentOutOfRangeException>(
            () => _ = new PathConnection<string>(destination, double.NaN),
            "Non-finite connection costs should be rejected.");
    }

    /// <summary>
    /// Verifies route improvement and terminal destination support.
    /// </summary>
    private static void VerifyImprovedRouteAndTerminalDestination()
    {
        TestMap map = new();
        PathFinder<string> pathFinder = new(map);
        Path<string> path = pathFinder.FindPath(0, 3);
        int[] expectedIds = [0, 2, 1, 3];

        Program.Assert(path.Steps.Select(step => step.Node.Id).SequenceEqual(expectedIds), "The least expensive route was not selected.");
        Program.Assert(path.Cost == 3, "The path cost is incorrect.");
        Program.Assert(path.Steps[0].CostFromPrevious == 0, "The start step should have no traversal cost.");
        Program.Assert(path.Steps[2].CostFromPrevious == 1, "The step traversal cost is incorrect.");
        Program.Assert(path.Steps[3].CostFromStart == 3, "The accumulated step cost is incorrect.");
        Program.Assert(path.GetCostAtIndex(2) == 2, "The indexed accumulated cost is incorrect.");
    }

    /// <summary>
    /// Verifies a path whose start and destination are the same terminal node.
    /// </summary>
    private static void VerifySameStartAndDestination()
    {
        TestMap map = new();
        PathFinder<string> pathFinder = new(map);
        Path<string> path = pathFinder.FindPath(3, 3);

        Program.Assert(path.Count == 1 && path[0].Id == 3, "The path should contain the shared start and destination node.");
    }

    /// <summary>
    /// Verifies connected path concatenation and accumulated costs.
    /// </summary>
    private static void VerifyPathConcatenation()
    {
        TestMap map = new();
        PathFinder<string> pathFinder = new(map);
        Path<string> firstPath = pathFinder.FindPath(0, 1);
        Path<string> secondPath = pathFinder.FindPath(1, 3);
        Path<string> combinedPath = firstPath.Concat(secondPath);
        int[] expectedIds = [0, 2, 1, 3];

        Program.Assert(combinedPath.Steps.Select(step => step.Node.Id).SequenceEqual(expectedIds), "Connected paths were not concatenated correctly.");
        Program.Assert(combinedPath.Cost == 3, "The concatenated path cost is incorrect.");
        Program.Assert(combinedPath.Steps[^1].CostFromStart == 3, "The concatenated accumulated cost is incorrect.");

        Path<string> disconnectedPath = pathFinder.FindPath(2, 1);
        Program.AssertThrows<ArgumentException>(
            () => firstPath.Concat(disconnectedPath),
            "Disconnected paths should not be concatenated.");
    }

    /// <summary>
    /// Verifies cancellation propagation.
    /// </summary>
    private static void VerifyCancellation()
    {
        TestMap map = new();
        PathFinder<string> pathFinder = new(map);
        using CancellationTokenSource cancellationSource = new();
        cancellationSource.Cancel();

        try
        {
            pathFinder.FindPath(0, 3, cancellationSource.Token);
            throw new InvalidOperationException("Cancellation was not propagated.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Throws when a test condition is not satisfied.
    /// </summary>
    /// <param name="condition">The condition to verify.</param>
    /// <param name="message">The failure message.</param>
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Verifies that an action throws the expected exception.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="action">The action to invoke.</param>
    /// <param name="message">The failure message.</param>
    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Provides a deterministic graph for pathfinding tests.
    /// </summary>
    private sealed class TestMap : INodeMap<string>
    {
        private readonly Dictionary<int, (int Id, double Cost)[]> _connections = new()
        {
            [0] = [(1, 5), (2, 1)],
            [1] = [(3, 1)],
            [2] = [(1, 1)],
            [3] = []
        };

        /// <inheritdoc/>
        public PathNode<string>? GetNode(int id)
        {
            return this._connections.ContainsKey(id)
                ? new PathNode<string>(id, $"Node {id}")
                : null;
        }

        /// <inheritdoc/>
        public IEnumerable<PathConnection<string>> GetConnections(PathNode<string> node)
        {
            return this._connections[node.Id]
                .Select(connection => new PathConnection<string>(
                    new PathNode<string>(connection.Id, $"Node {connection.Id}"),
                    connection.Cost));
        }
    }
}
