# AStar.net

AStar.net is a lightweight .NET 10 implementation of the A* pathfinding algorithm.

It provides integer node identifiers, directed weighted connections, cooperative cancellation, immutable path results,
and optional custom heuristics. Without a custom heuristic, searches behave like Dijkstra's algorithm.

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

## Documentation and examples

For the complete `INodeMap` contract, usage examples, cancellation, thread-safety guidance, and the interactive console
demo, visit the [AStar.net GitHub repository](https://github.com/AlbertoMorvillo/AStarNet).

## License

AStar.net is distributed under the MIT license.
