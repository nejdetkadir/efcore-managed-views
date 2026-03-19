# New Database Provider

Create a new database provider for EntityFrameworkCore.ManagedViews.

## Context

- Read docs/PROVIDERS.md for the interface contract
- See EntityFrameworkCore.ManagedViews.PostgreSQL for reference implementation
- Provider must implement IManagedViewProvider

## Steps

1. Create new project: EntityFrameworkCore.ManagedViews.{DatabaseName}
2. Implement IManagedViewProvider with all 7 methods
3. Create IDbContextOptionsExtension for service registration
4. Create DbContextOptionsBuilder extension method (Use{Database}ManagedViews)
5. Optionally extend IMigrationsSqlGenerator
6. Add unit tests for DDL generation
7. Add integration tests with Testcontainers
8. Update README.md packages table
