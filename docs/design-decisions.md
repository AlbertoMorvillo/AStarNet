# Design Decisions

AStar.net is designed to keep pathfinding fast, predictable, and easy to integrate. The library provides the algorithm
and its essential contracts, while the application remains responsible for the graph and its domain data.

The following sections explain the choices that have a visible effect on the API or implementation.

## Node Identifiers

Node IDs are `int` values because they are simple, fast, and friendly to array-backed maps. More elaborate identifiers
would add complexity without improving pathfinding. A `Guid`, for example, is considerably more complex and provides
global uniqueness, while AStar.net only needs an ID to be unique within its node map.

Applications that use other identifiers can map them to integers in their provider.

## Reusable `PathFinder` Instances

`PathFinder` is an instance because it can retain its map, heuristic, and tie-breaker between searches. Applications
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
obsolete queue entries to be recognised and ignored when they are dequeued.

Two custom indexed priority queues were also tested, one based on a binary heap and the other on a quaternary heap.
Both could locate a queued node and update its priority directly, avoiding obsolete entries. To do so, however, they
needed an additional index that mapped every node to its current position in the heap. That index also had to be
updated whenever nodes moved within the heap.

Neither heap produced a meaningful overall advantage in the tests. The framework priority queue performed well while
avoiding the additional index and leaving considerably less custom code to maintain, making it the best compromise
for the library.

## Optional Tie-Breaking

Tie-breaking requires extra comparisons and provider calls. Searches with and without it therefore use separate
internal loops, so the default case does not pay for work it does not need.

## Optional Heuristic

When no heuristic provider is configured, the heuristic is zero and A* behaves as Dijkstra's algorithm. The value is
used directly rather than calling a dedicated zero-heuristic object for every connection.

## Trust the Map, Validate the Values

The node map defines the graph, so the library accepts the connections it returns without asking the same provider to
confirm every destination again. Start and destination are checked before the search because they come directly from
the caller.

Data still has to respect the library's contracts. Connection costs and heuristic estimates must be finite and
non-negative, calculated costs and priorities must remain finite, and every visited node must return a connection
collection. Invalid values stop the search with an exception, as do exceptions raised directly by a provider.

The library can enforce these concrete limits, but it cannot decide whether the map omitted a connection or whether a
heuristic is suitable for the meaning of a particular graph.

## Immutable Paths

Completed paths are immutable so their steps, cost, equality, and ordering cannot be changed after calculation. To
obtain a different path, calculate it again; to edit one, copy its steps into an application-owned collection.

## Synchronous Search and Cancellation

`FindPath` is synchronous and can therefore be run directly or inside a task chosen by the application. It accepts a
`CancellationToken`, checked at every graph-exploration iteration, so a long-running search can be stopped promptly.
