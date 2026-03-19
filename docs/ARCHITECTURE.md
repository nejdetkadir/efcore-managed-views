# Architecture

This document describes the internal architecture of EntityFrameworkCore.ManagedViews: how components interact, data flows through the pipeline, and where to look when extending the system.

For setup and usage, see the [Implementation Guide](../IMPLEMENTATION.md). For building a new database provider, see the [Provider Guide](PROVIDERS.md).

## Table of Contents

- [Package Layout](#package-layout)
- [Component Overview](#component-overview)
- [Design-Time Pipeline](#design-time-pipeline)
- [Runtime Pipeline](#runtime-pipeline)
- [Service Registration](#service-registration)
- [Extension Points](#extension-points)
- [Data Flow Diagrams](#data-flow-diagrams)

## Package Layout

```
EntityFrameworkCore.ManagedViews          # Core (database-agnostic)
EntityFrameworkCore.ManagedViews.PostgreSQL   # PostgreSQL provider
```

The core package contains all abstractions, algorithms, and EF Core integration. Provider packages implement `IManagedViewProvider` and the migration SQL generator for a specific database.

### Why Two Packages?

A user who only needs PostgreSQL installs both. But the split means:

- The core diffing, hashing, snapshot, and dependency logic is reusable across databases
- Adding a new provider (e.g., SQL Server) requires no changes to the core package
- Test projects can test core logic without a database

## Component Overview

### Abstractions (`Abstractions/`)

Interfaces that define the contracts between components. No implementations live here.

| Interface | Responsibility |
|-----------|----------------|
| `IManagedViewDefinition` | Data contract for a view (name, schema, type, SQL, dependencies, indexes) |
| `IManagedViewProvider` | Database-specific DDL generation (CREATE, DROP, REFRESH, indexes) |
| `IManagedViewDiscovery` | Discovers view definitions from a source (SQL files, fluent API) |
| `IManagedViewHasher` | Computes deterministic hashes of SQL content |
| `IManagedViewDiffer` | Compares current definitions against a snapshot |
| `IManagedViewDependencyResolver` | Orders views by dependency graph |
| `IManagedViewSnapshotStore` | Persists/loads the design-time snapshot |
| `IManagedViewHistoryRepository` | Manages the runtime tracking table |

### Discovery (`Discovery/`)

Two discovery services scan for view definitions and a composite aggregates them:

```
SqlFileViewDiscoveryService     ──┐
                                  ├──> CompositeViewDiscoveryService
FluentApiViewDiscoveryService  ──┘
```

- **`SqlFileViewDiscoveryService`** -- Scans assembly embedded resources for `.sql` files, delegates parsing to `SqlFileMetadataParser`
- **`FluentApiViewDiscoveryService`** -- Reads model annotations set by `ModelBuilder.HasManagedView()`
- **`CompositeViewDiscoveryService`** -- Aggregates all sources, detects duplicates (same view defined in both SQL and fluent API)
- **`SqlFileMetadataParser`** -- Extracts directives (`@viewName`, `@schema`, etc.) and SQL body from file content

### Hashing (`Hashing/`)

- **`Sha256ViewHasher`** -- Normalizes SQL via `SqlNormalizer`, then computes SHA256
- **`SqlNormalizer`** -- Strips comments, collapses whitespace, normalizes line endings, trims semicolons. Controlled by `ManagedViewOptions.NormalizeSqlBeforeHashing` and `StripCommentsBeforeHashing`

The normalizer ensures formatting changes do not produce different hashes.

### Diffing (`Diffing/`)

- **`ManagedViewDiffer`** -- Compares current definitions (with computed hashes) against a `ManagedViewSnapshot`. Produces diffs: `Added`, `Modified`, `Removed`

### Dependency Resolution (`DependencyResolution/`)

- **`TopologicalSortResolver`** -- Implements Kahn's algorithm. `ResolveCreateOrder` returns dependencies-first ordering. `ResolveDropOrder` returns the reverse. Throws `ManagedViewDependencyCycleException` on cycles.

### Snapshot (`Snapshot/`)

- **`JsonSnapshotStore`** -- Reads/writes `{ContextName}ManagedViewsSnapshot.json` in the migrations directory
- **`ManagedViewSnapshot`** -- Root model containing `ManagedViewSnapshotEntry` items with hash, SQL, type, and indexes

### Tracking (`Tracking/`)

- **`ManagedViewHistoryRepository`** -- Manages the `__ManagedViewsHistory` table via raw SQL (avoids EF Core entity registration to prevent circular dependencies). Uses upsert for recording applied views.

### Configuration (`Configuration/`)

- **`ManagedViewOptions`** -- All user-configurable settings (assembly, prefix, schema, normalization flags, tracking table name)
- **`ManagedViewBuilder`** -- Fluent builder for defining views in `OnModelCreating`
- **`ManagedViewOptionsExtension`** -- `IDbContextOptionsExtension` that registers core services into EF Core's internal service provider

### Migrations (`Migrations/`)

- **`CreateManagedViewOperation`** / **`DropManagedViewOperation`** / **`RefreshMaterializedViewOperation`** -- Custom `MigrationOperation` subclasses
- **`ManagedViewDesignTimeServices`** -- `IDesignTimeServices` implementation that registers ManagedViews into the EF Core design-time pipeline

### Extensions (`Extensions/`)

| Extension Class | Target | Purpose |
|----------------|--------|---------|
| `DbContextOptionsBuilderExtensions` | `DbContextOptionsBuilder` | `.UseManagedViews()` |
| `ModelBuilderExtensions` | `ModelBuilder` / `EntityTypeBuilder` | `.HasManagedView()`, `.ToManagedView()` |
| `MigrationBuilderExtensions` | `MigrationBuilder` | `.CreateManagedView()`, `.DropManagedView()` |
| `DbContextExtensions` | `DbContext` | `.RefreshMaterializedViewAsync()`, `.CheckViewDriftAsync()` |
| `ServiceCollectionExtensions` | `IServiceCollection` | `.AddManagedViews()` |

### Exceptions (`Exceptions/`)

All exceptions inherit from `ManagedViewException`:

```
ManagedViewException
├── ManagedViewSqlParseException        # Invalid .sql file metadata
├── ManagedViewProviderException        # Unsupported operation for provider
├── ManagedViewDependencyCycleException # Circular dependency detected
├── ManagedViewDuplicateException       # Same view defined in multiple sources
└── ManagedViewNotFoundException        # Referenced dependency not found
```

## Design-Time Pipeline

When you run `dotnet ef migrations add`, the following sequence executes:

```
1. ManagedViewDesignTimeServices registers into EF Core's design-time pipeline
2. CompositeViewDiscoveryService.DiscoverAll()
   ├── SqlFileViewDiscoveryService.Discover()    → scans embedded .sql files
   └── FluentApiViewDiscoveryService.Discover()  → reads model annotations
3. Sha256ViewHasher.ComputeHash() for each definition
   └── SqlNormalizer.Normalize() → strip comments, collapse whitespace
4. JsonSnapshotStore.Load() → load previous snapshot
5. ManagedViewDiffer.ComputeDiff() → compare hashes → Added / Modified / Removed
6. TopologicalSortResolver.ResolveCreateOrder() → dependency-ordered creates
7. TopologicalSortResolver.ResolveDropOrder() → dependency-ordered drops
8. Emit CreateManagedViewOperation / DropManagedViewOperation
9. JsonSnapshotStore.Save() → persist new snapshot
```

## Runtime Pipeline

### Refresh Materialized View

```
context.RefreshMaterializedViewAsync("mv_name")
  1. ManagedViewHistoryRepository.GetAllAsync() → find tracked schema
  2. IManagedViewProvider.GenerateRefreshSql() → build REFRESH statement
  3. context.Database.ExecuteSqlRawAsync() → execute
```

### Refresh All Materialized Views

```
context.RefreshAllMaterializedViewsAsync()
  1. CompositeViewDiscoveryService.DiscoverAll()
  2. Filter to ManagedViewType.Materialized
  3. TopologicalSortResolver.ResolveCreateOrder() → dependency order
  4. For each: IManagedViewProvider.GenerateRefreshSql() → execute
```

### Check View Drift

```
context.CheckViewDriftAsync()
  1. ManagedViewHistoryRepository.GetAllAsync() → tracked entries
  2. CompositeViewDiscoveryService.DiscoverAll() → current definitions
  3. Compare:
     - Tracked but not in current → Missing
     - In both but hash differs → Modified
     - In current but not tracked → Untracked
  4. Return ViewDriftReport
```

## Service Registration

### Via `ManagedViewOptionsExtension.ApplyServices` (EF Core internal provider)

When `UseManagedViews()` or `UseNpgsqlManagedViews()` is called, the `ManagedViewOptionsExtension` registers core services into EF Core's internal service provider using `TryAddSingleton` / `TryAddScoped`:

| Service | Implementation | Lifetime |
|---------|----------------|----------|
| `IOptions<ManagedViewOptions>` | Configured options | Singleton |
| `IManagedViewHasher` | `Sha256ViewHasher` | Singleton |
| `IManagedViewDiffer` | `ManagedViewDiffer` | Singleton |
| `IManagedViewSnapshotStore` | `JsonSnapshotStore` | Singleton |
| `IManagedViewDependencyResolver` | `TopologicalSortResolver` | Singleton |
| `IManagedViewDiscovery` | `SqlFileViewDiscoveryService` | Singleton |
| `CompositeViewDiscoveryService` | Self | Singleton |
| `IManagedViewHistoryRepository` | `ManagedViewHistoryRepository` | Scoped |

The PostgreSQL `NpgsqlManagedViewOptionsExtension` additionally registers:

| Service | Implementation | Lifetime |
|---------|----------------|----------|
| `IManagedViewProvider` | `PostgreSqlManagedViewProvider` | Singleton |

### Via `IServiceCollection.AddManagedViews()` (application DI)

For applications that need ManagedViews services outside the DbContext (e.g., health checks, background jobs), the same services are registered into the application's DI container.

## Extension Points

### Adding a New Database Provider

Implement `IManagedViewProvider` and a migration SQL generator. See the [Provider Guide](PROVIDERS.md) for details.

### Adding a New Discovery Source

Implement `IManagedViewDiscovery` and register it. The `CompositeViewDiscoveryService` accepts `IEnumerable<IManagedViewDiscovery>`, so multiple sources are automatically aggregated.

### Custom Hashing

Replace `IManagedViewHasher` registration to use a different algorithm or normalization strategy.

### Custom Snapshot Storage

Replace `IManagedViewSnapshotStore` to store snapshots in a database, blob storage, or any other medium instead of the filesystem.

## Data Flow Diagrams

### View Definition Lifecycle

```
Developer writes .sql file or HasManagedView()
        │
        ▼
    Discovery (scan assembly / read model)
        │
        ▼
    Hashing (normalize SQL → SHA256)
        │
        ▼
    Diffing (compare with snapshot)
        │
        ▼
    Dependency Resolution (topological sort)
        │
        ▼
    Migration Operations (CreateManagedViewOperation / DropManagedViewOperation)
        │
        ▼
    Provider DDL Generation (CREATE VIEW / DROP VIEW / CREATE INDEX)
        │
        ▼
    Database (applied via EF Core migrations)
        │
        ▼
    Tracking Table (__ManagedViewsHistory records hash)
        │
        ▼
    Snapshot Updated (JSON file for next diff)
```

### Drift Detection Flow

```
context.CheckViewDriftAsync()
        │
        ├──── Read tracked entries from __ManagedViewsHistory
        │
        ├──── Discover current definitions (SQL files + fluent API)
        │
        ├──── Hash current definitions
        │
        └──── Compare tracked vs current
                │
                ├── Tracked but missing → ViewDriftType.Missing
                ├── Both exist, hash differs → ViewDriftType.Modified
                └── Current but untracked → ViewDriftType.Untracked
```
