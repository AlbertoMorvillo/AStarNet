# Priority Queue Comparison

## Purpose

The comparison evaluated whether a four-way indexed heap could improve AStar.net's search performance. The current
.NET `PriorityQueue<TElement, TPriority>` inserts a new entry when a cheaper route is found and discards older entries
with greater scores when dequeued. An indexed heap updates the existing entry instead, avoiding those duplicates.

Both variants performed complete `FindPath` searches on identical graphs with the same costs, heuristics, and
tie-breaking rules. Only the queue implementation changed.

## Measurement Limits

Measurements used .NET 10 on Windows x64, with warmup and repeated runs. Some timings remained variable, so the
results indicate trends in the workloads examined rather than general performance guarantees. Collections forced
before measurement also limit conclusions about sustained throughput.

Equal-priority ordering can differ between queues. Some expansion counts differed slightly without a tie-breaker;
with the tested tie-breaker, they matched. The results apply to these complete searches and this heap implementation,
not to every graph or indexed heap.

## Node IDs

Using node IDs directly as array indices would require a bounded range. The tested heap preserved arbitrary IDs by
using a dictionary of heap positions, whose cost was included in the measurements.

## Results

Workloads included weighted 128 by 128 grids with three seeds and a graph designed to produce many improvements.
Four-neighbor grids used Dijkstra; eight-neighbor grids used A* with a Chebyshev heuristic.

| Workload | Indexed heap time compared with the framework queue |
| --- | --- |
| Four-neighbor grids, no tie-breaker | 33–35% slower |
| Four-neighbor grids, with tie-breaker | 15–17% slower |
| Eight-neighbor grids, no tie-breaker | 18–19% slower |
| Eight-neighbor grids, with tie-breaker | Approximately equal |
| Many improvements, no tie-breaker | About 15% faster |
| Many improvements, with tie-breaker | About 43% faster |

Ranges describe results across grid seeds, not confidence intervals. Allocated bytes per search were nearly equal
between implementations, and both variants passed the path-correctness checks used in the comparison.

## Decision

Based on these results, AStar.net keeps .NET's `PriorityQueue<TElement, TPriority>`. The indexed heap's advantage in
searches with many improvements did not justify its slower grid searches and additional implementation complexity.

See [Design Decisions](design-decisions.md) for the other implementation choices.
