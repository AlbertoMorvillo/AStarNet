# Code Style

AStar.net uses a consistent, explicit coding style intended to keep behavior and intent easy to recognize during
maintenance and review. Implementations must preserve observable behavior and documented contracts. Among correct
alternatives, prefer meaningfully lower computational or memory overhead. When performance is equivalent, prefer the
clearest implementation.

## Naming and Language

Names should describe their purpose without requiring surrounding implementation details to explain them. Variables,
parameters, and helper methods use explicit, descriptive names rather than abbreviations. Terminology follows .NET
API conventions.

Code comments, XML documentation, README files, and internal documentation use clear, neutral American English. Use
.NET spelling such as `canceled`, `recognized`, `behavior`, and `color`.

## Types and Files

Use file-scoped namespaces. Place each top-level class, struct, record, interface, enum, or delegate in its own file.
Nested types can remain in the file of their containing type.

Respect the nullable reference type configuration of each project. Do not expose implementation details solely to
make them accessible to tests.

Do not use primary constructors. Use traditional constructor declarations with explicit constructor bodies so that
initialization, validation, and field assignment remain easy to identify.

Changing an existing type between `class`, `record class`, `struct`, or `record struct` requires an explicit design
decision because it can change equality, copying, allocation, and compatibility behavior.

## Member Access and Local Variables

Qualify instance members with `this.`. This makes instance state distinguishable from local variables and parameters:

```csharp
this._nodeIds = identifiers;
return this._connections.TryGetValue(nodeId, out PathConnection[]? connections)
    ? connections
    : null;
```

Prefer explicit types when the type is short and immediately readable:

```csharp
double tentativeCost = currentCost + connection.Cost;
PathConnection connection = connections[index];
```

Use `var` only when the resulting type is already obvious from the assignment expression. Do not use `var` for
primitive types or other simple types.

Prefer concrete types for local variables. Use interfaces at API boundaries when abstraction provides a meaningful
benefit. Prefer tuple deconstruction when tuple elements are consumed immediately as separate values; retain a named
tuple variable when the tuple is passed around or treated as one value.

## Layout and Expressions

Avoid overly complex inline expressions. Split long expressions, deeply nested calls, and complex conditions into
intermediate variables or focused helper methods with descriptive names.

Avoid abstractions and comments that only repeat what the code already states. Comments should document contracts,
constraints, intent, or non-obvious trade-offs.

## XML Documentation

Add XML documentation to classes, structs, enums, properties, constructors, and methods. Describe observable behavior,
parameters, return values, and relevant exceptions without restating the member signature.

## Collection Expressions

Use a collection expression only when it preserves the intended behavior and improves performance. It can also be used
when it is certain not to reduce performance and it makes the code meaningfully easier to read. For example:

```csharp
int[] nodeIds = [1, 2, 3];
List<PathConnection> connections = [];
```

Never accept a performance regression to make the code shorter, more modern, or more aesthetically pleasing,
regardless of how small the regression is. When performance equivalence is uncertain, keep the construction explicit.

Keep the construction explicit when the code depends on any of the following details:

- a specific collection type;
- a custom comparer or initial capacity;
- eager or deferred enumeration;
- enumeration order or number of enumerations;
- mutability or instance identity;
- behavior provided by a particular constructor.

Tests should also construct arrays, lists, and iterators explicitly when the distinction is part of what the test
checks.

Be careful when the target type is an interface such as `IEnumerable<T>`. In that case, the compiler can choose the
concrete representation. Use a collection expression only when that representation does not matter.

Treat analyzer suggestions as prompts for review, not as required changes. Apply a suggestion only after confirming
that it preserves the intended behavior and does not introduce any performance or memory regression.

## Regions

Use regions only when a class is large enough to benefit from them. When regions are useful, use this order and omit
empty regions:

1. Constants
2. Fields
3. Constructors and finalizers
4. Properties and indexers
5. Events
6. Operators and conversions
7. Public methods
8. Internal methods
9. Protected methods
10. Private methods
11. Nested types

Place static fields before instance fields. Within a method section, place static methods before instance methods when
that ordering improves readability; do not create separate regions solely to distinguish them.

## Automated Suggestions

Analyzer and IDE suggestions support review but do not replace design judgment. Apply a suggested simplification only
when it preserves the intended concrete type, enumeration behavior, allocation characteristics, and test purpose.
Project conventions take precedence over generic style suggestions, including suggestions to introduce primary
constructors.
