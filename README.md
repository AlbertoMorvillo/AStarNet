<p align="center">
  <img src="Assets/Images/Raster/AStarBanner.png" alt="AStar.net" width="640">
</p>

<p align="center">
  <a href="LICENSE">
    <img src="https://img.shields.io/github/license/AlbertoMorvillo/AStarNet" alt="License">
  </a>
  <img src="https://img.shields.io/badge/.NET-10-512BD4" alt=".NET 10">
  <a href="https://www.nuget.org/packages/AStar.net">
    <img src="https://img.shields.io/nuget/v/AStar.net" alt="NuGet version">
  </a>
  <a href="https://www.nuget.org/packages/AStar.net">
    <img src="https://img.shields.io/nuget/dt/AStar.net" alt="NuGet downloads">
  </a>
</p>

# AStar.net

AStar.net is an open-source .NET 10 library for calculating paths with the A* algorithm.

## Key features

- Lightweight and map-topology agnostic.
- Synchronous pathfinding with cooperative cancellation.
- Integer node identifiers, leaving application data under provider control.
- Directed connections with independent, non-negative traversal costs.
- Custom heuristic providers, with Dijkstra's algorithm used by default.
- Optional tie-breaker providers for ordering candidates with equal A* scores.
- Immutable path results with per-step and accumulated costs.
- Safe concurrent searches when the configured providers support concurrent reads.

## Installation

```bash
dotnet add package AStar.net
```

## Basic usage

Implement `INodeMap` and optionally `IHeuristicProvider`:

```csharp
using AStarNet;
using AStarNet.Heuristics;
using AStarNet.Maps;
using System.Collections.Generic;
using System.Linq;

public sealed class MyNodeMap : INodeMap
{
    private readonly Dictionary<int, int[]> _connections = new()
    {
        [0] = [1],
        [1] = [2],
        [2] = []
    };

    public bool ContainsNode(int nodeId)
    {
        return this._connections.ContainsKey(nodeId);
    }

    public IEnumerable<PathConnection>? GetConnections(int nodeId)
    {
        return this._connections.TryGetValue(nodeId, out int[]? destinationNodeIds)
            ? destinationNodeIds.Select(
                destinationNodeId => new PathConnection(destinationNodeId, 1))
            : null;
    }
}

MyNodeMap map = new();
PathFinder pathFinder = new(map);
Path path = pathFinder.FindPath(0, 2);
```

`GetConnections` must return:

- the outgoing connections for an existing node;
- an empty sequence when an existing node has no outgoing connections;
- `null` only when the supplied node ID does not exist.

## Reading a path

`Path.Empty` indicates that the destination is unreachable. A non-empty path exposes its endpoints, total cost, and
ordered steps:

```csharp
if (!path.IsEmpty)
{
    Console.WriteLine($"From {path.StartNodeId} to {path.EndNodeId}: {path.Cost}");

    foreach (PathStep step in path.Steps)
    {
        Console.WriteLine(
            $"Node {step.NodeId}: +{step.CostFromPrevious}, total {step.CostFromStart}");
    }
}
```

Paths can be ordered with `PathComparers.ByCost` or `PathComparers.ByNodeCount`, and connected paths can be combined
with `Path.Concat`.

## Heuristics and optimality

Without an `IHeuristicProvider`, AStar.net uses zero for every estimate and behaves like Dijkstra's algorithm. A
custom heuristic must be admissible—never greater than the actual minimum remaining cost—to preserve the optimality
guarantee.

Heuristic values must be finite and non-negative. Invalid values cause `FindPath` to throw `InvalidOperationException`.

`HeuristicMath` provides allocation-free Manhattan, Euclidean, and diagonal-distance calculations for two- and
three-dimensional providers:

```csharp
return HeuristicMath.Octile(
    destination.X - current.X,
    destination.Y - current.Y);
```

An optional `ITieBreakerProvider` can order candidate nodes whose A* scores are equal and choose between equal-cost
parent alternatives. It cannot override a score or path-cost difference, so it does not change which total path cost
is considered optimal.

`TieBreakerMath` provides squared line-deviation calculations in two and three dimensions. These scores avoid square
roots and divisions; when candidates use the same endpoints, lower values identify candidates closer to the endpoint
line.

## Cancellation

Long-running searches can be cancelled cooperatively:

```csharp
using CancellationTokenSource cancellationSource = new();
Path path = pathFinder.FindPath(0, 2, cancellationSource.Token);
```

## Thread safety

`PathFinder` keeps all mutable search state inside each `FindPath` invocation. The same instance can therefore serve
concurrent callers when its configured map, heuristic, and tie-breaker providers are also safe for concurrent use.

## Console demo

The repository includes an interactive grid demo:

```bash
dotnet run --project Source/Examples/AStarNet.ConsoleDemo/AStarNet.ConsoleDemo.csproj
```

Use the arrow keys to move the marker and follow the controls displayed beside the grid.

## Links

- [Documentation wiki](https://github.com/AlbertoMorvillo/AStarNet/wiki)
- [NuGet package](https://www.nuget.org/packages/AStar.net)
- [License](LICENSE)

## Licensing

The project is licensed under the MIT license.
