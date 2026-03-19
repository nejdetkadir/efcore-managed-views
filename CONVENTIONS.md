# Coding Conventions

## C# Style

- **Namespaces**: File-scoped (`namespace Foo;`)
- **Nullable**: Enabled project-wide
- **Access modifiers**: Explicit on all members
- **Classes**: Prefer `sealed` unless designed for inheritance
- **Fields**: Private fields use `_camelCase` prefix
- **Interfaces**: Prefixed with `I` (e.g., `IMyService`)
- **Braces**: Required on all control flow (enforced via .editorconfig)

## Parameter Validation

```csharp
ArgumentNullException.ThrowIfNull(parameter);
ArgumentException.ThrowIfNullOrWhiteSpace(stringParam);
```

## XML Documentation

All public types and members require XML documentation:

```csharp
/// <summary>
/// Discovers managed view definitions from embedded SQL files.
/// </summary>
public sealed class SqlFileViewDiscoveryService : IManagedViewDiscovery
```

## Test Naming

```
MethodUnderTest_Scenario_ExpectedResult
```

Examples:
- `Discover_WithEmbeddedSqlFiles_ReturnsDefinitions`
- `Hash_NullInput_ThrowsArgumentNullException`
- `Diff_NoChanges_ReturnsEmptyList`

## Dependency Injection

- Core services register via `ManagedViewOptionsExtension.ApplyServices`
- Provider services register via provider-specific `IDbContextOptionsExtension`
- Use `TryAddSingleton` / `TryAddScoped` to allow overrides
- Factory methods for scoped services that need `DbContext`

## Error Handling

Custom exception hierarchy rooted at `ManagedViewException`:
- `ManagedViewSqlParseException` - SQL parsing failures
- `ManagedViewProviderException` - Provider-specific errors
- `ManagedViewDependencyCycleException` - Circular view dependencies
- `ManagedViewDuplicateException` - Duplicate view names
- `ManagedViewNotFoundException` - View not found

## Commit Messages

Use conventional commits:
- `feat:` new feature
- `fix:` bug fix
- `docs:` documentation
- `test:` test changes
- `refactor:` code refactoring
