# Design Decisions

This page explains the main choices behind AStar.net's API and implementation.

## Node Identifiers

Node IDs use `int` for compact storage. IDs only need to be unique within their map,
so globally unique identifiers such as `Guid` are unnecessary. Negative IDs and zero are valid; IDs do not need to
form a consecutive range. The pathfinder stores search states in a dictionary rather than indexing an array by ID.

Applications that use other identifiers can map them to integers in their provider.

## Reusable `PathFinder` Instances

`PathFinder` uses an instance-based API to retain its map, heuristic, and tie-breaker between searches. Applications
that want a shared instance can keep one themselves. A static API would instead require the providers to be passed to
every call.

Only provider references are retained. Every search creates its own queue and internal state, so a `PathFinder` can be
reused without carrying results or search data from previous calls. Concurrent calls are safe when the configured
providers are also safe for concurrent use.

## Provider-Owned Content

AStar.net only needs node IDs. Coordinates, names, game objects, and other application data remain with the provider
that owns the graph. This keeps the library independent from content it neither reads nor manages.

## Costs on Connections

Traversal cost belongs to a connection because it describes movement between two nodes. This allows opposite
directions to have different costs and the same node to be reached through connections with different costs.

## Priority Queue

AStar.net uses .NET's `PriorityQueue<TElement, TPriority>`. To improve performance, an entry is not removed when a
better route to the same node is found. An internal state dictionary records the best route currently known and allows
obsolete queue entries to be recognized and ignored when they are dequeued.

A four-way indexed heap was compared with the framework queue in complete searches. It was faster in a graph with
many priority updates, but often slower on weighted grids, with little difference in allocations. AStar.net keeps the
framework queue rather than adding custom heap code and a separate node-to-position index for that trade-off.
See [Priority Queue Comparison](priority-queue-comparison.md) for the results and their limits.

## Connection Enumeration

`INodeMap.GetConnections` returns `IEnumerable<PathConnection>` to support both stored connections and on-demand
generation with `yield return`. Arrays and exact `List<PathConnection>` instances are enumerated directly to reduce
execution time and allocations. Other types use their own enumerators, preserving custom behavior and exceptions.
This also applies to list subclasses.

The connection-handling code is repeated in these loops. Keep the branches consistent when changing search behavior.

## Optional Tie-Breaking

Tie-breaking requires extra comparisons and provider calls. Searches with and without it therefore use separate
internal loops, so the default case does not pay for work it does not need.

## Optional Heuristic

When no heuristic provider is configured, the heuristic is zero and A* behaves as Dijkstra's algorithm.
No provider call is needed in this case.

Each search state stores the heuristic estimate and calculates `Score` as `CostFromStart + Heuristic`. This avoids
repeated provider calls when a cheaper route is found, without increasing state size. Recovering the estimate by
subtracting the cost from a stored score could lose precision.

Providers must return the same `double` value for the same node pair throughout each search. The pathfinder may cache
and reuse estimates, and the number and order of heuristic calls are not contractual. Estimates are not shared across
searches.

## Trust the Map, Validate the Values

The node map defines the graph, so the library accepts the connections it returns without asking the same provider to
confirm every destination again. Start and destination are checked before the search because they come directly from
the caller.

Data still has to respect the library's contracts. Connection costs and heuristic estimates must be finite and
non-negative, calculated costs and priorities must remain finite, and every visited node must return a connection
sequence. Invalid values stop the search with an exception, as do exceptions raised directly by a provider.

The library can enforce these concrete limits, but it cannot decide whether the map omitted a connection or whether a
heuristic is suitable for the meaning of a particular graph.

## Immutable Paths

Paths can be constructed from an ordered sequence of (NodeId, CostFromPrevious) tuples. The constructor calculates
accumulated costs and the hash while creating immutable steps. No lazy cache or separate validation pass is needed.
The first incoming cost must be zero; all costs must be finite and non-negative, and their running total must remain
finite. Path does not check connections against a map.

Concat reads path steps directly, checks shared endpoints, and skips duplicate boundary nodes. It recalculates totals
and the hash without revalidating incoming costs from existing paths. A private constructor stores the completed
result. No intermediate tuple collection or iterator is needed. When only one non-empty path is supplied, it is reused.

Concatenating two paths allocates the exact result capacity. Concatenating an arbitrary sequence consumes it once and
grows the result buffer as needed. Completed paths retain no input collections and cannot be changed after construction.

## Synchronous Search and Cancellation

`FindPath` is synchronous and can therefore be run directly or inside a task chosen by the application. It accepts a
`CancellationToken`, checked before provider calls and while processing nodes and connections.
