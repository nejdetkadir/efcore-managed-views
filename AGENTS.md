# AGENTS.md

## Project Overview

EntityFrameworkCore.ManagedViews is a .NET library that brings code-first view management to Entity Framework Core. It lets developers define, version, track, and migrate database views (including PostgreSQL materialized views) using the same workflow as tables.

## Tech Stack

- Language: C# (.NET 10)
- Framework: Entity Framework Core 10.x
- Database: PostgreSQL (via Npgsql.EntityFrameworkCore.PostgreSQL 10.x)
- Test Framework: xUnit, FluentAssertions, NSubstitute
- Integration Testing: Testcontainers for PostgreSQL
- Build: MSBuild / dotnet CLI
- CI: GitHub Actions

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build entire solution
dotnet build

# Build in Release mode
dotnet build --configuration Release
```

## Test Commands

```bash
# Run all tests
dotnet test

# Unit tests only (no database required)
dotnet test tests/EntityFrameworkCore.ManagedViews.Tests
dotnet test tests/EntityFrameworkCore.ManagedViews.PostgreSQL.Tests

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/EntityFrameworkCore.ManagedViews.IntegrationTests

# Functional tests
dotnet test tests/EntityFrameworkCore.ManagedViews.FunctionalTests

# With code coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage-results
```

## Code Style and Conventions

- Target: net10.0
- Nullable reference types: enabled
- File-scoped namespaces: required (enforced by .editorconfig)
- Warnings treated as errors: TreatWarningsAsErrors is true
- All .NET analyzers enabled at latest-all level
- Private fields: prefixed with underscore (_fieldName)
- Interfaces: prefixed with I (IMyInterface)
- Prefer sealed classes unless inheritance is intended
- Use ArgumentNullException.ThrowIfNull() for parameter validation
- Use ArgumentException.ThrowIfNullOrWhiteSpace() for string validation
- All public types and members must have XML documentation comments
- Test naming: MethodUnderTest_Scenario_ExpectedResult
- Use [Fact] for single cases, [Theory] with [InlineData] for parameterized
- Use FluentAssertions for all assertions
- Use NSubstitute for mocking

## Project Structure

```
ManagedViews/
src/
  EntityFrameworkCore.ManagedViews/           # Core package (database-agnostic)
    Abstractions/                             # Interfaces
    Configuration/                            # Options, builder, DI extension
    DependencyResolution/                     # Topological sort resolver
    Diffing/                                  # Snapshot differ
    Discovery/                                # SQL file + fluent API discovery
    Exceptions/                               # Custom exception hierarchy
    Extensions/                               # DbContext, ModelBuilder extensions
    Hashing/                                  # SHA256 hasher, SQL normalizer
    Migrations/                               # Design-time services, operations
    Models/                                   # DTOs (definition, diff, drift)
    Snapshot/                                 # JSON snapshot store
    Tracking/                                 # History repository
  EntityFrameworkCore.ManagedViews.PostgreSQL/ # PostgreSQL provider package
    PostgreSqlManagedViewProvider.cs           # DDL generation
    PostgreSqlManagedViewMigrationsSqlGenerator.cs
    Extensions/                               # UseNpgsqlManagedViews()
tests/
  EntityFrameworkCore.ManagedViews.Tests/              # Core unit tests
  EntityFrameworkCore.ManagedViews.PostgreSQL.Tests/    # PostgreSQL unit tests
  EntityFrameworkCore.ManagedViews.IntegrationTests/    # DB integration tests
  EntityFrameworkCore.ManagedViews.FunctionalTests/     # E2E pipeline tests
samples/
  Sample.ECommerce/                           # Example application
docs/                                         # Architecture, provider guides
```

## Architecture

The library has two packages:

1. **EntityFrameworkCore.ManagedViews** (core): Database-agnostic logic for discovery, hashing, diffing, dependency resolution, snapshots, and tracking.
2. **EntityFrameworkCore.ManagedViews.PostgreSQL**: PostgreSQL-specific DDL generation and migration SQL generator.

### Key Interfaces

| Interface | Purpose | Implementation |
|-----------|---------|----------------|
| IManagedViewDiscovery | Discovers view definitions | SqlFileViewDiscoveryService, FluentApiViewDiscoveryService |
| IManagedViewHasher | Hashes view SQL for change detection | Sha256ViewHasher |
| IManagedViewDiffer | Compares current vs snapshot | ManagedViewDiffer |
| IManagedViewDependencyResolver | Orders views by dependency | TopologicalSortResolver |
| IManagedViewSnapshotStore | Persists design-time snapshots | JsonSnapshotStore |
| IManagedViewHistoryRepository | Runtime tracking table | ManagedViewHistoryRepository |
| IManagedViewProvider | Database-specific DDL | PostgreSqlManagedViewProvider |

### Pipelines

Design-time: Discovery -> Hashing -> Diffing -> Dependency Resolution -> Migration Ops -> Snapshot Update

Runtime: Discovery -> Hashing -> History Comparison -> Drift Report

## Working with the Codebase

### Adding a new feature

1. Define or extend the interface in Abstractions/
2. Implement in the appropriate namespace
3. Register in ManagedViewOptionsExtension.ApplyServices or DI extensions
4. Add unit tests with 100% coverage
5. Update XML docs on all public members

### Adding a database provider

See docs/PROVIDERS.md for the full guide. Implement IManagedViewProvider and register via IDbContextOptionsExtension.

### Common patterns

- Options are configured via ManagedViewOptions and the builder pattern
- Services register through IDbContextOptionsExtension.ApplyServices into EF Core internal DI
- Factory methods resolve scoped services to avoid circular DbContext dependencies

## Important Files

- `Directory.Build.props` - Shared MSBuild properties for all projects
- `global.json` - SDK version pin (10.0.100)
- `.editorconfig` - Code style enforcement
- `IMPLEMENTATION.md` - Full API reference and setup guide
- `docs/ARCHITECTURE.md` - Internal component design
- `docs/PROVIDERS.md` - Guide for new database providers
- `Makefile` - Common development tasks

## Coverage Target

The project maintains 100% line coverage and 100% method coverage. Do not introduce uncovered lines.
