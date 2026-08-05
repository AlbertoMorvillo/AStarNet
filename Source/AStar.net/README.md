# AStar.net

AStar.net is an open-source .NET 10 library for calculating optimal paths with the A* algorithm.

## Key features

- Lightweight and map-topology agnostic.
- Synchronous and asynchronous pathfinding.
- Integer node identifiers with optional strongly typed content.
- Directed connections with independent traversal costs.
- Custom heuristic providers, with Dijkstra's algorithm used by default.

## Installation

```bash
dotnet add package AStar.net
```

## Usage

Implement `INodeMap<TContent>` and optionally `IHeuristicProvider<TContent>`:

```csharp
using AStarNet;
using AStarNet.Maps;
using System.Collections.Generic;
using System.Linq;

public sealed class MyNodeMap : INodeMap<string>
{
    private readonly Dictionary<int, int[]> _connections = new()
    {
        [0] = [1],
        [1] = [2],
        [2] = []
    };

    public PathNode<string>? GetNode(int id)
    {
        return this._connections.ContainsKey(id)
            ? new PathNode<string>(id, $"Node {id}")
            : null;
    }

    public IEnumerable<PathConnection<string>> GetConnections(PathNode<string> node)
    {
        return this._connections[node.Id]
            .Select(id => new PathConnection<string>(
                new PathNode<string>(id, $"Node {id}"),
                1));
    }
}

MyNodeMap map = new();
PathFinder<string> pathFinder = new(map);
Path<string> path = pathFinder.FindPath(0, 2);
```

Each result exposes `Steps`. A step contains its node, the cost from the previous node, and the accumulated cost from the start. Use `path[index]` to access a node directly.

The content argument is optional:

```csharp
PathNode<string> nodeWithoutContent = new(0);
```

## Links

- [Repository](https://github.com/AlbertoMorvillo/AStarNet)
- [NuGet package](https://www.nuget.org/packages/AStar.net)

## Licensing

The project is licensed under the MIT license.
