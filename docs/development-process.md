# Development Process

AStar.net is developed with an emphasis on correctness, meaningful performance, and a small public surface. Internal
implementation details may change when doing so improves the library without weakening its public contracts.

## Development Priorities

Changes are evaluated in this order:

1. Preserve correctness and explicit contracts.
2. Improve performance when the benefit is meaningful and measurable.
3. Prefer clear, maintainable code after the first two requirements are satisfied.

Micro-optimizations that add complexity without a practical benefit are avoided. Performance-sensitive alternatives
should be compared before replacing a simpler implementation.

## Public API Testing

Tests exercise the public API. Internal members are not exposed through `InternalsVisibleTo` or similar mechanisms for
the benefit of the test project.

This keeps internal implementation details free to change and ensures that tests describe behavior available to
library users. If an internal implementation cannot be observed through the public contract, it is not treated as a
separate testing surface.

## Validation

Validation is proportional to the change. Code and project changes normally require:

1. dependency restore when necessary;
2. a Release build of the complete solution;
3. execution of the complete test suite;
4. explicit package generation and inspection when packaging inputs or library output have changed;
5. manual demonstration checks when interactive behavior has changed.

The GitHub Validation workflow performs restore, build, and test on demand. It does not create or publish packages.

## Console Demo

The console demo is both an example and a manual integration check. Its first responsibility is to show clearly how
the public library is configured and used. Presentation helpers may organize console behavior, but library setup and
pathfinding calls should remain easy to locate and understand.

## AI-Assisted Development

AI-assisted tools have been used during the development of AStar.net to discuss design alternatives, review code,
identify edge cases, implement agreed changes, draft tests, and improve documentation.

They were used for three main reasons:

- to provide an additional critical perspective during design and review;
- to explore alternatives and their trade-offs before changing the implementation;
- to reduce repetitive work while maintaining consistency across code and documentation.

AI output was not accepted as an independent authority. Design choices were discussed and directed by the project
author, generated changes were reviewed, and resulting behavior was checked through builds, tests, package inspection,
and manual use of the console demo. Responsibility for the project and its published releases remains with the author.
