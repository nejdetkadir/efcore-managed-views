# EntityFrameworkCore.ManagedViews -- Implementation Guide

This document covers setup, configuration, API reference, and migration integration. For internal architecture, see the [Architecture Guide](docs/ARCHITECTURE.md). For building a new database provider, see the [Provider Guide](docs/PROVIDERS.md).

## Table of Contents

1. [Adding to a New Project](#1-adding-to-a-new-project)
2. [Configuration Reference](#2-configuration-reference)
3. [Defining Views](#3-defining-views)
4. [Mapping Entities to Views](#4-mapping-entities-to-views)
5. [Migration Integration](#5-migration-integration)
6. [Runtime Operations](#6-runtime-operations)
7. [Change Detection and Drift](#7-change-detection-and-drift)
8. [Dependency Resolution](#8-dependency-resolution)
9. [Public API Reference](#9-public-api-reference)
10. [Refactoring from EF Core Native View Approach](#10-refactoring-from-ef-core-native-view-approach)
11. [What ManagedViews Ensures for Developers](#11-what-managedviews-ensures-for-developers)

> **See also:** [README](README.md) | [Architecture](docs/ARCHITECTURE.md) | [Provider Guide](docs/PROVIDERS.md) | [Changelog](CHANGELOG.md)

---

## 1. Adding to a New Project

### Prerequisites

- .NET 10+
- Entity Framework Core 10.x
- A supported database provider (PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x)

### Install NuGet Packages

```bash
dotnet add package EntityFrameworkCore.ManagedViews
dotnet add package EntityFrameworkCore.ManagedViews.PostgreSQL
```

### Register in DbContext

```csharp
services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(connectionString)
        .UseManagedViews());
```

Or use the PostgreSQL convenience method:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(connectionString)
        .UseNpgsqlManagedViews());
```

### Register Services in DI (for runtime features)

```csharp
services.AddManagedViews(options =>
{
    options.ViewAssembly = typeof(AppDbContext).Assembly;
});
```

### Create Your First View

Add a `.sql` file to your project under a `Views/` directory:

```
YourProject/
  Views/
    vw_active_products.sql
```

Mark it as an embedded resource in your `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Views/**/*.sql" />
</ItemGroup>
```

Write the SQL file with metadata directives:

```sql
-- @viewName: vw_active_products
-- @schema: public
-- @type: view

SELECT
    p.id,
    p.name,
    p.price,
    c.name AS category_name
FROM products p
INNER JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true
```

---

## 2. Configuration Reference

### ManagedViewOptions

All configuration flows through `ManagedViewOptions`. Pass it via `UseManagedViews()`, `UseNpgsqlManagedViews()`, or `AddManagedViews()`.

```csharp
options.UseManagedViews(o =>
{
    o.ViewAssembly = typeof(AppDbContext).Assembly;
    o.SqlFilesPrefix = "Views";
    o.DefaultSchema = "public";
    o.TrackingTableName = "__ManagedViewsHistory";
    o.TrackingTableSchema = null;
    o.AutoCreateTrackingTable = true;
    o.NormalizeSqlBeforeHashing = true;
    o.StripCommentsBeforeHashing = true;
});
```

| Property | Type | Default | Description |
|---|---|---|---|
| `ViewAssembly` | `Assembly?` | `null` | Assembly containing embedded `.sql` resources. When `null`, defaults to the DbContext's assembly at discovery time. |
| `SqlFilesPrefix` | `string` | `"Views"` | Resource name prefix filter. Only embedded resources whose name contains this prefix are scanned. |
| `DefaultSchema` | `string` | `"public"` | Default database schema applied to views that don't specify one. |
| `TrackingTableName` | `string` | `"__ManagedViewsHistory"` | Name of the runtime tracking table that stores applied view hashes. |
| `TrackingTableSchema` | `string?` | `null` | Schema for the tracking table. When `null`, the table is created without a schema qualifier. |
| `AutoCreateTrackingTable` | `bool` | `true` | Whether to automatically create the tracking table when `EnsureCreatedAsync()` is called. |
| `NormalizeSqlBeforeHashing` | `bool` | `true` | Normalizes whitespace, line endings, and trailing semicolons in SQL before computing the hash. Prevents false-positive change detection from formatting differences. |
| `StripCommentsBeforeHashing` | `bool` | `true` | Strips SQL comments (`--` and `/* */`) before hashing. Prevents comment-only edits from triggering a migration. |

### SQL File Metadata Directives

Directives are placed at the top of `.sql` files as SQL comments. They are case-insensitive.

```sql
-- @viewName: vw_active_products
-- @schema: public
-- @type: materialized
-- @dependsOn: vw_base_products, vw_categories
-- @indexes: idx_product_name(name); idx_product_category(category_id, name)
```

| Directive | Required | Default | Description |
|---|---|---|---|
| `@viewName` | No | Inferred from filename | The database name of the view. If omitted, inferred from the filename (e.g., `vw_active_products.sql` becomes `vw_active_products`, `public.vw_active_products.sql` becomes `vw_active_products`). |
| `@schema` | No | Inferred from filename or `DefaultSchema` | The database schema. If filename is `myschema.vw_name.sql`, schema is inferred as `myschema`. Otherwise falls back to `ManagedViewOptions.DefaultSchema`. |
| `@type` | No | `view` | Either `view` or `materialized`. |
| `@dependsOn` | No | None | Comma-separated list of view names this view depends on. Used for topological ordering of CREATE/DROP operations. |
| `@indexes` | No | None | Semicolon-separated index definitions for materialized views. Format: `index_name(columns)`. Multiple columns use commas: `idx_name(col1, col2)`. |

### File Naming Conventions

When `@viewName` and `@schema` are omitted, they are inferred from the filename:

| Filename | Inferred ViewName | Inferred Schema |
|---|---|---|
| `vw_products.sql` | `vw_products` | `DefaultSchema` |
| `public.vw_products.sql` | `vw_products` | `public` |
| `catalog.mv_stats.sql` | `mv_stats` | `catalog` |

---

## 3. Defining Views

ManagedViews supports two ways to define views. Both can be used simultaneously; a `CompositeViewDiscoveryService` aggregates them and detects duplicates.

### Option A: Embedded SQL Files

Place `.sql` files in your project and mark them as embedded resources (see section 1). The `SqlFileViewDiscoveryService` scans the assembly for resources matching the `SqlFilesPrefix` pattern and parses their metadata headers.

**Regular view:**

```sql
-- @viewName: vw_active_products
-- @schema: public
-- @type: view

SELECT id, name, price
FROM products
WHERE is_active = true
```

**Materialized view with dependencies and indexes:**

```sql
-- @viewName: mv_category_stats
-- @schema: public
-- @type: materialized
-- @dependsOn: vw_active_products
-- @indexes: idx_cat_stats_category(category_name)

SELECT
    category_name,
    COUNT(*) AS product_count,
    AVG(price) AS avg_price
FROM vw_active_products
GROUP BY category_name
```

### Option B: Fluent API (C# Code)

Define views inside `OnModelCreating` using the `HasManagedView` extension:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasManagedView("vw_active_products", view => view
        .InSchema("public")
        .AsSql(@"
            SELECT id, name, price
            FROM products
            WHERE is_active = true"));

    modelBuilder.HasManagedView("mv_category_stats", view => view
        .InSchema("public")
        .AsMaterialized()
        .DependsOn("vw_active_products")
        .HasIndex("idx_cat_stats_category", "category_name")
        .AsSql(@"
            SELECT category_name, COUNT(*) AS product_count, AVG(price) AS avg_price
            FROM vw_active_products
            GROUP BY category_name"));
}
```

### ManagedViewBuilder Methods

| Method | Signature | Description |
|---|---|---|
| `InSchema` | `ManagedViewBuilder InSchema(string schema)` | Sets the database schema. Defaults to `ManagedViewOptions.DefaultSchema` if not called. |
| `AsSql` | `ManagedViewBuilder AsSql(string sql)` | Sets the SQL body (the SELECT statement). Required. |
| `AsMaterialized` | `ManagedViewBuilder AsMaterialized()` | Marks the view as a materialized view. Default is a regular view. |
| `DependsOn` | `ManagedViewBuilder DependsOn(string viewName)` | Declares this view depends on another managed view. Affects CREATE/DROP ordering. |
| `DependsOn` | `ManagedViewBuilder DependsOn(params string[] viewNames)` | Declares dependencies on multiple views at once. |
| `HasIndex` | `ManagedViewBuilder HasIndex(string indexName, string columns)` | Adds an index on a materialized view. Only meaningful for materialized views. |

---

## 4. Mapping Entities to Views

To query a managed view through EF Core's `DbSet<T>`, map a keyless entity to it using `ToManagedView<T>()`:

```csharp
public class ActiveProductView
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = null!;
}

public class AppDbContext : DbContext
{
    public DbSet<ActiveProductView> ActiveProducts => Set<ActiveProductView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActiveProductView>(b =>
        {
            b.HasNoKey();
            b.ToManagedView("vw_active_products", "public");
        });
    }
}
```

`ToManagedView<T>()` internally:
1. Calls EF Core's native `builder.ToView(viewName, schema)` so the entity is queryable
2. Adds `ManagedViews:IsManagedView` and `ManagedViews:ViewName` annotations for tracking

**Querying:**

```csharp
var products = await context.ActiveProducts
    .Where(p => p.Price > 50)
    .OrderBy(p => p.Name)
    .ToListAsync();
```

This generates standard SQL against the view, leveraging all of EF Core's LINQ translation.

---

## 5. Migration Integration

### Using MigrationBuilder Extensions

In your EF Core migration files, use `CreateManagedView` and `DropManagedView`:

```csharp
public partial class AddProductViews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateManagedView(
            name: "vw_active_products",
            schema: "public",
            sql: "SELECT id, name, price FROM products WHERE is_active = true",
            viewType: ManagedViewType.View);

        migrationBuilder.CreateManagedView(
            name: "mv_category_stats",
            schema: "public",
            sql: @"SELECT category_name, COUNT(*) AS product_count
                   FROM vw_active_products GROUP BY category_name",
            viewType: ManagedViewType.Materialized,
            indexes: [new ManagedViewIndex("idx_cat_name", "category_name")]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropManagedView(
            name: "mv_category_stats",
            schema: "public",
            viewType: ManagedViewType.Materialized);

        migrationBuilder.DropManagedView(
            name: "vw_active_products",
            schema: "public");
    }
}
```

### Custom Migration Operations

Under the hood, these extension methods add custom `MigrationOperation` instances:

| Operation | Properties | Description |
|---|---|---|
| `CreateManagedViewOperation` | `ViewName`, `Schema`, `Sql`, `ViewType`, `Indexes` | Creates a view/materialized view and its indexes. |
| `DropManagedViewOperation` | `ViewName`, `Schema`, `ViewType` | Drops a view/materialized view with CASCADE. |
| `RefreshMaterializedViewOperation` | `ViewName`, `Schema`, `Concurrently` | Refreshes a materialized view's data. |

The `PostgreSqlManagedViewMigrationsSqlGenerator` translates these operations into provider-specific DDL:

| Operation | Generated SQL (PostgreSQL) |
|---|---|
| `CreateManagedViewOperation` (View) | `CREATE OR REPLACE VIEW "schema"."name" AS <sql>` |
| `CreateManagedViewOperation` (Materialized) | `CREATE MATERIALIZED VIEW IF NOT EXISTS "schema"."name" AS <sql> WITH DATA` |
| `DropManagedViewOperation` (View) | `DROP VIEW IF EXISTS "schema"."name" CASCADE` |
| `DropManagedViewOperation` (Materialized) | `DROP MATERIALIZED VIEW IF EXISTS "schema"."name" CASCADE` |
| `RefreshMaterializedViewOperation` | `REFRESH MATERIALIZED VIEW [CONCURRENTLY] "schema"."name"` |
| Index creation | `CREATE INDEX IF NOT EXISTS "idx_name" ON "schema"."name" (columns)` |

### Design-Time Snapshot

ManagedViews stores a JSON snapshot file (`{ContextName}ManagedViewsSnapshot.json`) alongside your migrations. This snapshot records the SHA256 hash of each view's SQL at the time the migration was created. On the next `dotnet ef migrations add`, the differ compares current definitions against this snapshot to determine which views were added, modified, or removed.

---

## 6. Runtime Operations

### Refreshing Materialized Views

```csharp
// Refresh a single materialized view
await context.RefreshMaterializedViewAsync("mv_category_stats");

// Refresh with CONCURRENTLY (requires a unique index)
await context.RefreshMaterializedViewAsync("mv_category_stats", concurrently: true);

// Refresh ALL materialized views in dependency order
await context.RefreshAllMaterializedViewsAsync();
```

### Checking for Drift

```csharp
var report = await context.CheckViewDriftAsync();

if (report.HasDrift)
{
    foreach (var entry in report.Entries)
    {
        Console.WriteLine($"{entry.Schema}.{entry.ViewName}: {entry.DriftType}");
        // DriftType is one of: Missing, Modified, Untracked
    }
}
```

### Tracking Table Management

```csharp
// Ensure the tracking table exists (idempotent)
var repo = context.GetService<IManagedViewHistoryRepository>();
await repo.EnsureCreatedAsync();

// Record a view was applied
await repo.RecordAppliedAsync("vw_products", "public", hash, ManagedViewType.View);

// Check if a view is tracked
string? hash = await repo.GetHashAsync("vw_products", "public");

// Get all tracked entries
var all = await repo.GetAllAsync();

// Record a view was dropped
await repo.RecordRemovedAsync("vw_products", "public");
```

---

## 7. Change Detection and Drift

ManagedViews uses two complementary change detection mechanisms:

### Design-Time Change Detection (Snapshot-Based)

**When:** During `dotnet ef migrations add`

**How it works:**
1. The `CompositeViewDiscoveryService` discovers all current view definitions (from SQL files and fluent API)
2. The `IManagedViewHasher` computes a SHA256 hash for each view's SQL (after normalization)
3. The `IManagedViewDiffer` compares these hashes against the previous `ManagedViewSnapshot`
4. Diffs are generated: `Added`, `Modified`, or `Removed`
5. The `IManagedViewDependencyResolver` orders the operations (topological sort with cycle detection)
6. Custom migration operations are emitted
7. A new snapshot is saved

**SQL normalization before hashing:**
- Strips single-line (`--`) and multi-line (`/* */`) comments (when `StripCommentsBeforeHashing = true`)
- Collapses consecutive whitespace to a single space
- Normalizes line endings to `\n`
- Trims trailing semicolons
- Trims leading/trailing whitespace

This means reformatting SQL or changing comments will not trigger a migration.

### Runtime Drift Detection (Tracking Table)

**When:** On demand via `CheckViewDriftAsync()`

**How it works:**
1. The `__ManagedViewsHistory` table stores the hash of each view's SQL at the time it was last applied
2. `CheckViewDriftAsync()` re-discovers current definitions and recomputes hashes
3. Compares tracked hashes against current hashes
4. Reports drift as one of:

| DriftType | Meaning |
|---|---|
| `Missing` | View is tracked in `__ManagedViewsHistory` but no longer exists in the source definitions |
| `Modified` | View exists in both, but the SQL hash has changed |
| `Untracked` | View exists in source definitions but is not tracked (was never applied through ManagedViews) |

---

## 8. Dependency Resolution

Views often depend on other views. ManagedViews uses Kahn's algorithm (topological sort) to determine the correct order for CREATE and DROP operations.

### Declaring Dependencies

**SQL file:**
```sql
-- @dependsOn: vw_base_products, vw_categories
```

**Fluent API:**
```csharp
modelBuilder.HasManagedView("mv_stats", view => view
    .DependsOn("vw_products")
    .DependsOn("vw_categories")
    .AsSql("..."));
```

### How It Works

- **CREATE order:** Dependencies are created first. If `mv_stats` depends on `vw_products`, then `vw_products` is created before `mv_stats`.
- **DROP order:** The reverse. Dependents are dropped first. `mv_stats` is dropped before `vw_products`.
- **Circular dependencies:** If a cycle is detected (e.g., A depends on B, B depends on A), a `ManagedViewDependencyCycleException` is thrown with the names of the views involved.

---

## 9. Public API Reference

### Extension Methods

#### DbContextOptionsBuilderExtensions

```csharp
public static DbContextOptionsBuilder UseManagedViews(
    this DbContextOptionsBuilder builder,
    Action<ManagedViewOptions>? configure = null)
```
Enables managed view support for the DbContext. Adds the `ManagedViewOptionsExtension` to the options builder's internal service provider.

#### NpgsqlDbContextOptionsBuilderExtensions

```csharp
public static DbContextOptionsBuilder UseNpgsqlManagedViews(
    this DbContextOptionsBuilder builder,
    Action<ManagedViewOptions>? configure = null)
```
PostgreSQL convenience wrapper. Calls `UseManagedViews()` internally.

#### ModelBuilderExtensions

```csharp
public static ModelBuilder HasManagedView(
    this ModelBuilder builder,
    string viewName,
    Action<ManagedViewBuilder> configure)
```
Defines a managed view using the fluent API. The view definition is stored as a model annotation and picked up by the `FluentApiViewDiscoveryService`.

```csharp
public static EntityTypeBuilder<TEntity> ToManagedView<TEntity>(
    this EntityTypeBuilder<TEntity> builder,
    string viewName,
    string? schema = null) where TEntity : class
```
Maps a keyless entity to a managed view for LINQ querying. Internally calls `builder.ToView(viewName, schema)` and annotates the entity for ManagedViews tracking.

#### MigrationBuilderExtensions

```csharp
public static MigrationBuilder CreateManagedView(
    this MigrationBuilder builder,
    string name,
    string schema,
    string sql,
    ManagedViewType viewType = ManagedViewType.View,
    ManagedViewIndex[]? indexes = null)
```
Adds a `CreateManagedViewOperation` to the migration. For materialized views, indexes are created after the view.

```csharp
public static MigrationBuilder DropManagedView(
    this MigrationBuilder builder,
    string name,
    string schema,
    ManagedViewType viewType = ManagedViewType.View)
```
Adds a `DropManagedViewOperation` to the migration. Uses `CASCADE` to handle dependent objects.

#### DbContextExtensions

```csharp
public static Task RefreshMaterializedViewAsync(
    this DbContext context,
    string viewName,
    bool concurrently = false,
    CancellationToken cancellationToken = default)
```
Executes `REFRESH MATERIALIZED VIEW [CONCURRENTLY] "schema"."name"`. The schema is resolved from the tracking table. Use `concurrently: true` only when a unique index exists on the materialized view.

```csharp
public static Task RefreshAllMaterializedViewsAsync(
    this DbContext context,
    bool concurrently = false,
    CancellationToken cancellationToken = default)
```
Discovers all materialized views, orders them by dependency, and refreshes each one sequentially.

```csharp
public static Task<ViewDriftReport> CheckViewDriftAsync(
    this DbContext context,
    CancellationToken cancellationToken = default)
```
Compares current view definitions (from discovery) against the tracking table to detect drift. Returns a `ViewDriftReport` with `HasDrift` and a list of `ViewDriftEntry` items.

#### ServiceCollectionExtensions

```csharp
public static IServiceCollection AddManagedViews(
    this IServiceCollection services,
    Action<ManagedViewOptions>? configure = null)
```
Registers all core ManagedViews services into the DI container:
- `IManagedViewHasher` -> `Sha256ViewHasher` (Singleton)
- `IManagedViewDiffer` -> `ManagedViewDiffer` (Singleton)
- `IManagedViewSnapshotStore` -> `JsonSnapshotStore` (Singleton)
- `IManagedViewDependencyResolver` -> `TopologicalSortResolver` (Singleton)
- `IManagedViewDiscovery` -> `SqlFileViewDiscoveryService` (Singleton)
- `IManagedViewHistoryRepository` -> `ManagedViewHistoryRepository` (Scoped)
- `CompositeViewDiscoveryService` (Singleton)

### Interfaces

#### IManagedViewDefinition

```csharp
public interface IManagedViewDefinition
{
    string ViewName { get; }
    string Schema { get; }
    ManagedViewType ViewType { get; }
    string Sql { get; }
    IReadOnlyList<string> DependsOn { get; }
    IReadOnlyList<ManagedViewIndex> Indexes { get; }
    string Source { get; }
}
```
Core contract for a view definition. `Sql` contains only the SELECT body; the provider generates the full DDL wrapper.

#### IManagedViewProvider

```csharp
public interface IManagedViewProvider
{
    string GenerateCreateSql(IManagedViewDefinition definition);
    string GenerateDropSql(IManagedViewDefinition definition);
    IReadOnlyList<string> GenerateCreateIndexSql(IManagedViewDefinition definition);
    IReadOnlyList<string> GenerateDropIndexSql(IManagedViewDefinition definition);
    string GenerateRefreshSql(string viewName, string schema, bool concurrently);
    bool SupportsCreateOrReplace(ManagedViewType viewType);
    string GenerateGetViewDefinitionSql(string viewName, string schema);
}
```
Database provider contract for generating DDL. The PostgreSQL implementation (`PostgreSqlManagedViewProvider`) uses `CREATE OR REPLACE VIEW` for regular views and `DROP + CREATE` for materialized views (PostgreSQL does not support `CREATE OR REPLACE MATERIALIZED VIEW`).

#### IManagedViewDiscovery

```csharp
public interface IManagedViewDiscovery
{
    IReadOnlyList<IManagedViewDefinition> Discover(ManagedViewOptions options);
}
```
Discovers view definitions from a single source. Two built-in implementations:
- `SqlFileViewDiscoveryService` -- scans assembly embedded resources
- `FluentApiViewDiscoveryService` -- reads model annotations from `HasManagedView()`

#### IManagedViewHasher

```csharp
public interface IManagedViewHasher
{
    string ComputeHash(string sql);
}
```
Computes a deterministic SHA256 hash. SQL is normalized before hashing based on `ManagedViewOptions` settings.

#### IManagedViewDiffer

```csharp
public interface IManagedViewDiffer
{
    IReadOnlyList<ManagedViewDiff> ComputeDiff(
        IReadOnlyList<IManagedViewDefinition> currentDefinitions,
        ManagedViewSnapshot? previousSnapshot);
}
```
Compares current definitions against a snapshot. Returns diffs of type `Added`, `Modified`, or `Removed`.

#### IManagedViewDependencyResolver

```csharp
public interface IManagedViewDependencyResolver
{
    IReadOnlyList<IManagedViewDefinition> ResolveCreateOrder(
        IReadOnlyList<IManagedViewDefinition> definitions);

    IReadOnlyList<IManagedViewDefinition> ResolveDropOrder(
        IReadOnlyList<IManagedViewDefinition> definitions);
}
```
Topologically sorts views. `ResolveDropOrder` returns the reverse of `ResolveCreateOrder`.

#### IManagedViewHistoryRepository

```csharp
public interface IManagedViewHistoryRepository
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
    Task RecordAppliedAsync(string viewName, string schema, string hash,
        ManagedViewType viewType, CancellationToken cancellationToken = default);
    Task RecordRemovedAsync(string viewName, string schema,
        CancellationToken cancellationToken = default);
    Task<string?> GetHashAsync(string viewName, string schema,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedViewHistoryEntry>> GetAllAsync(
        CancellationToken cancellationToken = default);
    string GetCreateTableSql();
}
```
Manages the `__ManagedViewsHistory` table. Uses upsert semantics for `RecordAppliedAsync` (INSERT ON CONFLICT UPDATE).

#### IManagedViewSnapshotStore

```csharp
public interface IManagedViewSnapshotStore
{
    ManagedViewSnapshot? Load(string migrationsDirectory, string contextName);
    void Save(ManagedViewSnapshot snapshot, string migrationsDirectory, string contextName);
}
```
Persists the design-time snapshot as JSON.

### Models

#### ManagedViewDefinition

```csharp
public sealed class ManagedViewDefinition : IManagedViewDefinition
{
    public string ViewName { get; init; }
    public string Schema { get; init; }           // default: "public"
    public ManagedViewType ViewType { get; init; } // default: View
    public string Sql { get; init; }
    public IReadOnlyList<string> DependsOn { get; init; }
    public IReadOnlyList<ManagedViewIndex> Indexes { get; init; }
    public string Source { get; init; }
}
```

#### ManagedViewIndex

```csharp
public sealed record ManagedViewIndex(string Name, string Columns);
```

#### ManagedViewDiff

```csharp
public sealed class ManagedViewDiff
{
    public required IManagedViewDefinition Definition { get; init; }
    public required ManagedViewDiffType DiffType { get; init; }
    public string? PreviousHash { get; init; }
    public string? CurrentHash { get; init; }
}
```

#### ViewDriftReport

```csharp
public sealed class ViewDriftReport
{
    public bool HasDrift => Entries.Count > 0;
    public IReadOnlyList<ViewDriftEntry> Entries { get; init; }
}
```

#### ViewDriftEntry

```csharp
public sealed class ViewDriftEntry
{
    public required string ViewName { get; init; }
    public required string Schema { get; init; }
    public required ViewDriftType DriftType { get; init; }
    public string? ExpectedHash { get; init; }
    public string? ActualHash { get; init; }
}
```

#### ManagedViewHistoryEntry

```csharp
public sealed class ManagedViewHistoryEntry
{
    public required string ViewName { get; init; }
    public required string Schema { get; init; }
    public required string Hash { get; init; }
    public required string ViewType { get; init; }
    public required DateTimeOffset AppliedAt { get; init; }
}
```

### Enums

#### ManagedViewType

| Value | Description |
|---|---|
| `View = 0` | Standard database view (`CREATE VIEW`) |
| `Materialized = 1` | Materialized view with stored data (`CREATE MATERIALIZED VIEW`) |

#### ManagedViewDiffType

| Value | Description |
|---|---|
| `Added` | View is new and needs to be created |
| `Modified` | View SQL has changed and needs to be recreated |
| `Removed` | View was removed from definitions and needs to be dropped |

#### ViewDriftType

| Value | Description |
|---|---|
| `Missing` | View is tracked but no longer exists in source definitions |
| `Modified` | View exists but its SQL hash differs from what was tracked |
| `Untracked` | View exists in source definitions but was never applied through ManagedViews |

### Exceptions

| Exception | When Thrown |
|---|---|
| `ManagedViewException` | Base exception. Not thrown directly. |
| `ManagedViewSqlParseException` | A `.sql` file has invalid or unparseable metadata directives. |
| `ManagedViewProviderException` | Provider-specific error (e.g., unsupported view type). |
| `ManagedViewDependencyCycleException` | Circular dependency detected among views during topological sort. |
| `ManagedViewDuplicateException` | The same view name is defined in multiple sources (e.g., both a SQL file and fluent API). |
| `ManagedViewNotFoundException` | A referenced view dependency cannot be found in the known definitions. |

All exceptions inherit from `ManagedViewException` and include standard constructors (`()`, `(string message)`, `(string message, Exception innerException)`).

---

## 10. Refactoring from EF Core Native View Approach

EF Core's native view support uses `ToView()` for query mapping and requires you to manage view DDL manually. ManagedViews builds on top of that native mechanism, so refactoring is incremental -- you keep your existing queries and just add lifecycle management.

### Before: EF Core Native Approach

```csharp
// DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<ActiveProductView>(b =>
    {
        b.HasNoKey();
        b.ToView("vw_active_products", "public");
    });
}

// Migration (manual SQL)
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        CREATE OR REPLACE VIEW public.vw_active_products AS
        SELECT id, name, price FROM products WHERE is_active = true;
    ");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP VIEW IF EXISTS public.vw_active_products;");
}
```

**Problems with this approach:**
- View SQL is embedded in migration C# files -- hard to find, review, and reuse
- No change detection -- modifying a view requires you to manually write a new migration
- No dependency ordering -- you must manually sequence CREATE and DROP across migrations
- No drift detection -- no way to know if production views match your source code
- No tracking -- if someone edits a view directly in production, you have no visibility

### After: ManagedViews Approach

#### Step 1: Replace `ToView()` with `ToManagedView()`

```diff
  modelBuilder.Entity<ActiveProductView>(b =>
  {
      b.HasNoKey();
-     b.ToView("vw_active_products", "public");
+     b.ToManagedView("vw_active_products", "public");
  });
```

`ToManagedView()` calls `ToView()` internally, so all your existing LINQ queries continue to work exactly as before.

#### Step 2: Move SQL out of migrations into a `.sql` file

Create `Views/vw_active_products.sql`:

```sql
-- @viewName: vw_active_products
-- @schema: public
-- @type: view

SELECT id, name, price
FROM products
WHERE is_active = true
```

Add to `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Views/**/*.sql" />
</ItemGroup>
```

#### Step 3: Replace raw SQL migrations with typed operations

```diff
  protected override void Up(MigrationBuilder migrationBuilder)
  {
-     migrationBuilder.Sql(@"
-         CREATE OR REPLACE VIEW public.vw_active_products AS
-         SELECT id, name, price FROM products WHERE is_active = true;
-     ");
+     migrationBuilder.CreateManagedView(
+         name: "vw_active_products",
+         schema: "public",
+         sql: "SELECT id, name, price FROM products WHERE is_active = true");
  }

  protected override void Down(MigrationBuilder migrationBuilder)
  {
-     migrationBuilder.Sql("DROP VIEW IF EXISTS public.vw_active_products;");
+     migrationBuilder.DropManagedView(
+         name: "vw_active_products",
+         schema: "public");
  }
```

#### Step 4: Enable ManagedViews in DbContext options

```diff
  services.AddDbContext<AppDbContext>(options =>
      options
-         .UseNpgsql(connectionString));
+         .UseNpgsql(connectionString)
+         .UseManagedViews());
```

#### Step 5: (Optional) Add runtime features

```csharp
// Register services for runtime features
services.AddManagedViews(o => o.ViewAssembly = typeof(AppDbContext).Assembly);

// In a health check or startup:
var report = await context.CheckViewDriftAsync();
if (report.HasDrift)
{
    logger.LogWarning("Database views have drifted from source definitions");
}
```

### What Changes, What Stays the Same

| Aspect | Before (Native EF Core) | After (ManagedViews) |
|---|---|---|
| Entity mapping | `b.ToView("name", "schema")` | `b.ToManagedView("name", "schema")` |
| LINQ queries | `context.ActiveProducts.Where(...)` | Unchanged |
| View SQL location | Inside migration C# files | Dedicated `.sql` files or fluent API |
| Migration operations | Raw `migrationBuilder.Sql(...)` | Typed `CreateManagedView` / `DropManagedView` |
| Change detection | Manual | Automatic (hash-based snapshot diffing) |
| Dependency ordering | Manual | Automatic (topological sort) |
| Drift detection | Not available | `CheckViewDriftAsync()` |
| Materialized view refresh | Manual raw SQL | `RefreshMaterializedViewAsync()` |

---

## 11. What ManagedViews Ensures for Developers

At the end of the day, ManagedViews gives you these guarantees:

### Your views are code, not afterthoughts

View definitions live in your source repository as `.sql` files or fluent API declarations -- not buried inside migration snapshots. They are reviewable in pull requests, traceable in git history, and readable by DBAs and developers alike.

### Changes are detected, not forgotten

Every time you modify a view's SQL, the hash-based differ catches it. You cannot accidentally deploy stale views because the snapshot comparison surfaces every addition, modification, and removal.

### Dependencies are respected, not guessed

If `mv_category_stats` depends on `vw_active_products`, the topological sort guarantees `vw_active_products` is created first and dropped last. Circular dependencies are caught at build time, not at 2 AM in production.

### Formatting noise is ignored

Whitespace changes, comment edits, and semicolon differences do not trigger false migrations. The SQL normalizer ensures only semantically meaningful changes produce new hashes.

### Production drift is visible

`CheckViewDriftAsync()` tells you whether the views running in production match what your code defines. You can wire this into health checks, CI pipelines, or deployment gates to catch manual database edits before they cause problems.

### Materialized views are first-class citizens

Creating, indexing, refreshing, and dropping materialized views is handled through typed APIs -- no more copy-pasting raw `REFRESH MATERIALIZED VIEW` statements across your codebase.

### EF Core's query engine is fully preserved

`ToManagedView()` is built on top of `ToView()`. Your LINQ queries, projections, includes, and filters work exactly as they would with native EF Core view mapping. ManagedViews adds lifecycle management without touching the query path.

### The migration pipeline is provider-aware

The `IManagedViewProvider` abstraction means PostgreSQL-specific DDL (like `CREATE OR REPLACE VIEW` vs `DROP + CREATE MATERIALIZED VIEW`) is generated correctly. Adding a new database provider means implementing one interface, not rewriting the diffing or dependency logic.

### Errors are specific, not generic

When something goes wrong, you get `ManagedViewDependencyCycleException` (with the cycle path), `ManagedViewDuplicateException` (with the conflicting sources), or `ManagedViewSqlParseException` (with the malformed directive) -- not a generic "something failed" message.

### The tracking table is your audit log

The `__ManagedViewsHistory` table records which views were applied, when, and with what hash. This is your single source of truth for what the database should look like, independent of whether someone ran migrations in order or skipped steps.
