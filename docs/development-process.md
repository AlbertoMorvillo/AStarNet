# Development Process

AStar.net keeps its public API small and prioritizes correctness, measured performance, and readable code.

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

Tests check behavior available to library users, allowing the implementation to change without rewriting the tests.

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

AI tools are used to assist with design discussions, code review, implementation, testing, and documentation.
The project author directs and reviews this work and remains responsible for the code and releases. Changes are
validated through builds, automated tests, package inspection, and manual use of the console demo where appropriate.
