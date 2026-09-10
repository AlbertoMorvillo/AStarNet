# Architecture

AStar.net separates the pathfinding algorithm from the graph and application data supplied by its users. The library
works only with integer node identifiers, directed connections, traversal costs, and optional providers for heuristic
estimates and tie-breaking.

## Main Components

### `PathFinder`

`PathFinder` coordinates each search. An instance retains references to its node map and optional heuristic and
tie-breaker providers, but it does not retain graph data, search state, or previous results.

All mutable search data is created inside each `FindPath` call and discarded when that call finishes. A single
`PathFinder` instance can therefore be reused and can serve concurrent callers when its providers also support
concurrent access and remain stable during each search.

### `INodeMap`

`INodeMap` defines the graph seen by the algorithm. It reports whether the requested start and destination identifiers
exist and returns the outgoing connections for nodes visited by the search.

The provider owns the graph representation and any application-specific content. It may use arrays, dictionaries,
generated data, database-backed data, or another storage model without exposing that choice to AStar.net.

Maps may generate connections on demand and change between searches. During a search, the topology, connections,
and costs seen by the pathfinder must stay consistent and stable, including during enumeration. Changing them while
A* is running is unsupported and may produce invalid or nonoptimal results. Wait for active searches to finish
before making changes, or give each search a stable snapshot.

`GetConnections` returns `IEnumerable<PathConnection>`. The search enumerates arrays and exact `List<PathConnection>`
instances directly; other types use their own enumerators. It does not copy connections into a collection.

### `IHeuristicProvider`

An optional `IHeuristicProvider` estimates the remaining cost between two node identifiers. When no provider is
supplied, every estimate is treated as zero and the search behaves like Dijkstra's algorithm.

Within a single `FindPath` call, the same `(fromNodeId, toNodeId)` pair must return the same `double` value. The
pathfinder may cache and reuse estimates; the number and order of `GetHeuristic` calls are not part of the contract.

### `ITieBreakerProvider`

An optional `ITieBreakerProvider` resolves ties between candidates. It can influence which equal-cost path is
selected, but it cannot override score or path-cost differences.

### `Path`, `PathStep`, and `PathConnection`

`PathConnection` represents a directed connection and its traversal cost. `PathStep` records a node in a completed
path together with its cost from the previous node and accumulated cost from the start. `Path` exposes the immutable
ordered result.

## Search Lifecycle

A search follows these stages:

1. Validate the start and destination identifiers through the node map.
2. Create the per-call state dictionary and priority queue.
3. Add the start node with an accumulated cost of zero.
4. Repeatedly remove the candidate with the lowest A* score.
5. Request its outgoing connections and evaluate possible improvements.
6. Record the best known state for each discovered node.
7. Stop when the destination result is final or the queue is empty.
8. Reconstruct an immutable `Path` by following parent identifiers from the destination to the start.

The score used by the queue is:

```text
score = cost from start + heuristic estimate
```

## Search State and Priority Queue

The state dictionary stores the best route currently known for every discovered node. Each state contains the parent
identifier, the cost from that parent, the accumulated cost from the start, and the validated heuristic estimate.
`Score` is calculated as `CostFromStart + Heuristic`. Better routes and equal-cost parent replacements preserve the
original estimate. Estimates remain local to the search and are not carried into subsequent calls.

The priority queue is not indexed. When a better route to an already queued node is found, the improved entry is added
without removing the older one. When an entry is removed from the queue, its priority is compared with the current
state. An entry with a greater score is discarded; rounding can make old and new scores equal.

This keeps queue operations simple while preserving the best route in the state dictionary. The rationale and
alternatives are documented in [Design Decisions](design-decisions.md#priority-queue).
The [Priority Queue Comparison](priority-queue-comparison.md) records measurements from complete searches.

## Tie-Breaking Execution Path

The common case without a tie-breaker uses `PriorityQueue<int, double>` and compares only A* scores. When a tie-breaker
is configured, the queue uses an internal priority value and comparer that can also evaluate candidate node
identifiers.

The two paths are kept separate so that searches without tie-breaking do not carry richer priority values or pay for
additional comparisons and provider calls.

When tie-breaking is enabled, finding the destination does not always end the loop immediately. Other candidates with
the same score may still produce an equal-cost result preferred by the configured tie-breaker. Processing stops once
the next queued score is greater than the destination score.

## Path Reconstruction

The pathfinder counts the parent chain, then fills an exactly sized array of node IDs and incoming costs backwards.
The array is passed to the public Path constructor in start-to-destination order.

The constructor creates immutable steps, calculates accumulated costs, checks input costs and overflow, and computes
the hash in one pass. Search-state totals are not copied: they may lag behind parent changes when rounded scores tie.
Arrays and exact lists are enumerated directly with a result buffer of known size. Other sequences are consumed once
using their own enumerators and a growing buffer. Input collections are not retained.

Concatenation performs its own traversal of existing steps and uses a private constructor to store the completed
array, total cost, and hash. Both construction routes use the same cost accumulation and hashing logic.

## Validation Boundaries

AStar.net validates conditions it can determine reliably:

- start and destination identifiers must exist;
- connection costs must be finite and non-negative;
- accumulated costs and A* scores must remain finite;
- heuristic values must be finite and non-negative;
- provider methods must follow their documented null and collection contracts.

The library cannot independently verify whether a provider's graph is internally consistent or whether a heuristic is
admissible for that graph. Graph topology returned by the node map is treated as its source of truth.

## Cancellation

`FindPath` is synchronous. It checks the `CancellationToken` before provider calls and while processing nodes and
connections.
