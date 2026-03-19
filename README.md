# EntityFrameworkCore.ManagedViews

[![CI](https://github.com/nejdetkadir/efcore-managed-views/actions/workflows/ci.yml/badge.svg)](https://github.com/nejdetkadir/efcore-managed-views/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/EntityFrameworkCore.ManagedViews.svg)](https://www.nuget.org/packages/EntityFrameworkCore.ManagedViews)
[![NuGet Downloads](https://img.shields.io/nuget/dt/EntityFrameworkCore.ManagedViews.svg)](https://www.nuget.org/packages/EntityFrameworkCore.ManagedViews)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Code-first view management for Entity Framework Core. Define, version, track, and migrate database views using the same workflow as tables.

## Table of Contents

- [Features](#features)
- [Packages](#packages)
- [Quick Start](#quick-start)
- [SQL File Directives](#sql-file-directives)
- [Materialized Views](#materialized-views)
- [Drift Detection](#drift-detection)
- [Architecture](#architecture)
- [Documentation](#documentation)
- [Requirements](#requirements)
- [Contributing](#contributing)
- [License](#license)

## Features

- **SQL file definitions** -- Define views in embedded `.sql` files with metadata directives
- **Fluent API definitions** -- Define views in C# using `modelBuilder.HasManagedView()`
- **Change detection** -- SHA256 hashing with SQL normalization prevents formatting-only changes from triggering migrations
- **Snapshot-based tracking** -- Design-time snapshot file (similar to EF Core's ModelSnapshot) for CI/CD workflows
- **Runtime tracking** -- `__ManagedViewsHistory` table for production drift detection
- **Dependency ordering** -- Topological sort ensures views are created/dropped in the correct order
- **Materialized views** -- Full support for PostgreSQL materialized views with index and refresh management
- **Migration integration** -- Custom operations (`CreateManagedViewOperation`, `DropManagedViewOperation`) integrate into EF Core migrations

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| [`EntityFrameworkCore.ManagedViews`](https://www.nuget.org/packages/EntityFrameworkCore.ManagedViews) | Core abstractions, diffing, hashing, snapshot, discovery | [![NuGet](https://img.shields.io/nuget/v/EntityFrameworkCore.ManagedViews.svg)](https://www.nuget.org/packages/EntityFrameworkCore.ManagedViews) |
| [`EntityFrameworkCore.ManagedViews.PostgreSQL`](https://www.nuget.org/packages/EntityFrameworkCore.ManagedViews.PostgreSQL) | PostgreSQL provider with materialized view support | [![NuGet](https://img.shields.io/nuget/v/EntityFrameworkCore.ManagedViews.PostgreSQL.svg)](https://www.nuget.org/packages/EntityFrameworkCore.ManagedViews.PostgreSQL) |

## Quick Start

### 1. Install

```bash
dotnet add package EntityFrameworkCore.ManagedViews.PostgreSQL
```

### 2. Define views with SQL files

Add `.sql` files to a `Views/` folder and mark them as embedded resources:

```xml
<ItemGroup>
  <EmbeddedResource Include="Views/**/*.sql" />
</ItemGroup>
```

```sql
-- Views/vw_active_products.sql
-- @viewName: vw_active_products
-- @schema: public
-- @type: view

SELECT p.id, p.name, p.price, c.name AS category_name
FROM products p
INNER JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true
```

### 3. Or define views with the fluent API

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasManagedView("vw_active_products", v => v
        .InSchema("public")
        .AsSql("SELECT id, name, price FROM products WHERE is_active = true"));
}
```

### 4. Map entities to views

```csharp
modelBuilder.Entity<ActiveProductView>(b =>
{
    b.HasNoKey();
    b.ToManagedView("vw_active_products");
});
```

### 5. Enable managed views

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseNpgsqlManagedViews());
```

## SQL File Directives

| Directive | Description | Example |
|-----------|-------------|---------|
| `@viewName` | Name of the view (inferred from filename if omitted) | `-- @viewName: vw_active_products` |
| `@schema` | Database schema (inferred from filename or defaults to `public`) | `-- @schema: catalog` |
| `@type` | `view` or `materialized` | `-- @type: materialized` |
| `@dependsOn` | Comma-separated dependencies | `-- @dependsOn: vw_base, vw_other` |
| `@indexes` | Semicolon-separated index defs (materialized views only) | `-- @indexes: idx_name(column1); idx_other(col2 DESC)` |

See the [Implementation Guide](IMPLEMENTATION.md#2-configuration-reference) for file naming conventions and schema inference.

## Materialized Views

```sql
-- Views/mv_category_stats.sql
-- @viewName: mv_category_stats
-- @type: materialized
-- @dependsOn: vw_active_products
-- @indexes: idx_cat_stats(category_name)

SELECT category_name, COUNT(*) AS product_count, AVG(price) AS avg_price
FROM vw_active_products
GROUP BY category_name
```

Refresh at runtime:

```csharp
await context.RefreshMaterializedViewAsync("mv_category_stats");
await context.RefreshAllMaterializedViewsAsync(concurrently: true);
```

## Drift Detection

Check for drift between tracked views and current definitions:

```csharp
var report = await context.CheckViewDriftAsync();
if (report.HasDrift)
{
    foreach (var entry in report.Entries)
    {
        Console.WriteLine($"{entry.ViewName}: {entry.DriftType}");
    }
}
```

| Drift Type | Meaning |
|------------|---------|
| `Missing` | Tracked in `__ManagedViewsHistory` but removed from source definitions |
| `Modified` | SQL hash has changed since last applied |
| `Untracked` | Exists in source definitions but was never applied |

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                        Your Application                          │
├──────────────────────────────────────────────────────────────────┤
│  DbContext.UseManagedViews()  ·  ModelBuilder.HasManagedView()   │
│  MigrationBuilder.CreateManagedView()  ·  ToManagedView<T>()    │
├──────────────────────────────────────────────────────────────────┤
│                EntityFrameworkCore.ManagedViews                   │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────────┐   │
│  │Discovery │ │ Hashing  │ │ Diffing  │ │   Dep. Resolution │   │
│  │(SQL/API) │ │ (SHA256) │ │(Snapshot)│ │(Topological Sort) │   │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────────┘   │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                        │
│  │Snapshot  │ │Tracking  │ │Migration │                        │
│  │ (JSON)   │ │ (DB)     │ │  (Ops)   │                        │
│  └──────────┘ └──────────┘ └──────────┘                        │
├──────────────────────────────────────────────────────────────────┤
│           EntityFrameworkCore.ManagedViews.PostgreSQL             │
│  ┌───────────────────┐ ┌────────────────────────────────┐       │
│  │ PostgreSQL DDL    │ │ Migration SQL Generator        │       │
│  │  Provider         │ │ (Npgsql)                       │       │
│  └───────────────────┘ └────────────────────────────────┘       │
├──────────────────────────────────────────────────────────────────┤
│                    Entity Framework Core                         │
└──────────────────────────────────────────────────────────────────┘
```

For a detailed architecture breakdown, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Documentation

| Document | Description |
|----------|-------------|
| [Implementation Guide](IMPLEMENTATION.md) | Setup, configuration, API reference, migration integration, refactoring guide |
| [Architecture](docs/ARCHITECTURE.md) | Component design, data flow, extension points |
| [Provider Guide](docs/PROVIDERS.md) | How to implement `IManagedViewProvider` for a new database |
| [Project Summary](docs/SUMMARY.md) | Design decisions, package layout, developer experience goals |
| [Changelog](CHANGELOG.md) | Release history following [Keep a Changelog](https://keepachangelog.com) |
| [Contributing](CONTRIBUTING.md) | How to contribute, development setup, coding standards |
| [Security Policy](SECURITY.md) | Reporting vulnerabilities |
| [Code of Conduct](CODE_OF_CONDUCT.md) | Community standards ([Contributor Covenant](https://www.contributor-covenant.org)) |

## Requirements

- .NET 10
- Entity Framework Core 10.x
- PostgreSQL (via Npgsql.EntityFrameworkCore.PostgreSQL 10.x)

## Contributing

Contributions are welcome. Please read the [Contributing Guide](CONTRIBUTING.md) before submitting a pull request.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
