# AStar.net

AStar.net is a lightweight .NET 10 implementation of the A* pathfinding algorithm.

It provides integer node identifiers, directed weighted connections, cooperative cancellation, immutable path results,
optional custom heuristics, and optional candidate tie-breaking. Without a custom heuristic, every estimate is zero and
searches behave like Dijkstra's algorithm.

Paths can also be created directly from an ordered sequence of `(NodeId, CostFromPrevious)` tuples. The first cost
must be zero. The constructor calculates accumulated costs and creates immutable steps; it does not validate the
connections against a map. See [Paths and Steps](https://github.com/AlbertoMorvillo/AStarNet/wiki/Paths-and-Steps).

## Installation

```bash
dotnet add package AStar.net
```

## Getting started

Implement `INodeMap`, create a `PathFinder`, and request a path:

```csharp
INodeMap map = new MyNodeMap();
PathFinder pathFinder = new(map);
Path path = pathFinder.FindPath(startNodeId, destinationNodeId);
```

An unreachable destination returns `Path.Empty`. A successful result exposes its total cost, endpoints, and immutable
sequence of `PathStep` values.

Custom heuristics must return finite, non-negative estimates and must never overestimate the actual minimum remaining
cost when an optimal result is required.

During each `FindPath` call, the same `(fromNodeId, toNodeId)` pair must produce the same `double` estimate. The
pathfinder may cache and reuse estimates; the number and order of `GetHeuristic` calls are not part of the contract.

Maps may generate connections on demand and change between searches. Observable topology, connections, and costs
must remain consistent and stable throughout each search, including enumeration. Changing the observable map while
the current A* algorithm is running is unsupported and may produce invalid or nonoptimal results. Coordinate changes
with active searches or provide a stable snapshot for each search.

`HeuristicMath` provides Manhattan, Euclidean, octile, and three-dimensional diagonal calculations without requiring
the library to know how node identifiers map to coordinates.

An optional `ITieBreakerProvider` can order candidate nodes with equal A* scores and choose between equal-cost parent
alternatives without overriding the cost-based ordering.

`TieBreakerMath` provides squared line-deviation calculations for two- and three-dimensional tie-breakers. Scores can
be compared directly when candidates are measured against the same endpoints.

## Documentation and examples

For the complete `INodeMap` contract, usage examples, cancellation, thread-safety guidance, and the interactive console
demo, visit the [AStar.net GitHub repository](https://github.com/AlbertoMorvillo/AStarNet).

Version history and release notes are available on the
[GitHub Releases page](https://github.com/AlbertoMorvillo/AStarNet/releases).

## License

AStar.net is distributed under the MIT license.
