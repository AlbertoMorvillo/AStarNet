# AGENTS.md

## Project overview

This is a C# project targeting .NET 10.

Before making changes, inspect the existing project structure, architecture, and code conventions.

## Working rules

- Read the relevant existing code before making changes.
- Make only the changes required by the user's request.
- Preserve the current architecture, naming conventions, and coding style.
- Do not add or update external NuGet packages unless explicitly requested.
- Do not modify generated files.
- Do not delete or rename public types or members unless explicitly requested.
- Prefer small, focused changes over large refactors.
- Avoid unrelated cleanup or refactoring.

## Code style

- Use file-scoped namespaces.
- Respect the project's existing nullable reference type configuration.
- Use explicit and descriptive names.
- Write code comments and XML documentation in English.
- Use imperative or neutral wording in comments.
- Add XML documentation to all classes, structs, enums, properties, constructors, and methods.
- Avoid comments that merely repeat what the code already expresses.
- Avoid unnecessary abstractions.
- Follow the formatting and organization used by existing files.

- Prioritize correctness first, then meaningful performance improvements, then aesthetic elegance.
- When multiple correct solutions are available, prefer the one with lower computational and memory overhead, provided that the improvement is meaningful and does not significantly reduce readability.
- After performance requirements are satisfied, make the implementation as clear and readable as possible.
- Avoid premature or speculative micro-optimizations that make the code harder to understand without a meaningful benefit.
- Avoid overly complex inline expressions.
- Split long expressions, deeply nested calls, and complex conditions into clearly named intermediate variables or helper methods.
- Always qualify instance members with `this.`.
- Prefer explicit type declarations over `var` when the type is short and immediately readable.
- Use `var` only when the resulting type remains obvious from the assignment expression.
- Do not use `var` for primitive types or other simple types.

- Choose between `class`, `record`, `struct`, and `record struct` according to the type's semantics, not for brevity.
- Use a `record` when the type primarily represents immutable data and value-based equality is desired.
- Use a `class` when the type has mutable state, significant behavior, lifecycle, or reference identity.
- Use a `struct` only for small, immutable value types when value semantics and reduced allocation are meaningful.
- Before converting an existing type between `class`, `record`, `struct`, or `record struct`, explain the reason and ask for explicit confirmation.
- Do not use primary constructors.
- Use traditional constructor declarations with an explicit constructor body.

## Regions

When a class is large enough to benefit from regions, use the following order:

1. Constants
2. Nested types
3. Fields
4. Events
5. Constructors
6. Properties
7. Public methods
8. Protected methods
9. Private methods

Do not add empty regions.

## Validation

After making changes:

1. Restore dependencies when necessary.
2. Build the entire solution.
3. Run the relevant existing tests.
4. Report any build warnings or failing tests.
5. Summarize every modified file.
