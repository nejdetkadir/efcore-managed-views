# Source Code Agent Instructions

## Package Layout

- `EntityFrameworkCore.ManagedViews/` - Core package (database-agnostic)
- `EntityFrameworkCore.ManagedViews.PostgreSQL/` - PostgreSQL provider

## Adding Code

- All new public types must have XML documentation comments
- Use `sealed` unless inheritance is explicitly needed
- Register new services in `ManagedViewOptionsExtension.ApplyServices`
- Follow existing namespace conventions (e.g., Discovery/, Hashing/, Diffing/)

## Key Files

- `Abstractions/` - All interfaces live here
- `Configuration/ManagedViewOptionsExtension.cs` - Service registration into EF Core DI
- `Extensions/DbContextExtensions.cs` - Runtime extension methods
- `Extensions/ModelBuilderExtensions.cs` - Design-time fluent API

## Provider Pattern

To add database support, implement `IManagedViewProvider` and register via a custom `IDbContextOptionsExtension`. See the PostgreSQL package and docs/PROVIDERS.md for the pattern.
