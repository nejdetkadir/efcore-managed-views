# EntityFrameworkCore.ManagedViews — Project Scope Summary

## Overview

A NuGet package that brings **code-first view management** to EF Core — with migration integration, change detection, and per-provider support. The goal is to make database views first-class citizens in EF Core migrations, just like tables.

---

## Design Decisions

### 1. View Definition Source
**Both — .sql files as primary, fluent API as alternative**
- Primary: Embedded `.sql` files in a convention-based folder (e.g., `Views/`)
- Alternative: C# fluent API for simple views (`builder.ToManagedView("SELECT ...")`)

### 2. Change Detection Strategy
**Dual approach — tracking table + snapshot file**
- **Runtime:** SHA256 hash-based tracking table (`__ManagedViewsHistory`) to detect drift
- **Design-time/CI:** Snapshot file (similar to EF's `ModelSnapshot`) committed to source control

### 3. Migration Integration
**Auto-generate migration steps via `IDesignTimeServices` hook**
- Hooks into EF Core's design-time pipeline
- Automatically detects view changes and generates migration operations
- Works seamlessly with `dotnet ef migrations add`

### 4. View Types (PostgreSQL Provider)
**Full spectrum — Regular + Materialized + Indexed views**
- `CREATE VIEW` — standard views
- `CREATE MATERIALIZED VIEW` — with refresh support
- Provider-specific indexed view support

### 5. View Update Strategy
**CREATE OR REPLACE (always recreate)**
- Simple, predictable, idempotent
- `CREATE OR REPLACE VIEW` for regular views
- `DROP + CREATE` for materialized views (PostgreSQL doesn't support `CREATE OR REPLACE MATERIALIZED VIEW`)

### 6. Dependency & Lifecycle Features
**Lean scope — essentials only**
- Auto-detect view dependencies and order CREATE/DROP correctly
- Refresh materialized views via helper method
- **Out of scope (v1):** View-to-view layered dependency graphs, seed data support

---

## Package Architecture

### NuGet Packages

| Package | Purpose |
|---------|---------|
| `EntityFrameworkCore.ManagedViews` | Core abstractions, interfaces, snapshot logic |
| `EntityFrameworkCore.ManagedViews.PostgreSQL` | Npgsql provider — SQL generation, materialized views |

> Future providers (SQL Server, MySQL, SQLite) will follow the same `EntityFrameworkCore.ManagedViews.{Provider}` pattern.

### Target Frameworks & Compatibility

| Dependency | Versions |
|-----------|----------|
| .NET | 8.0+ (LTS minimum) |
| EF Core | 8.x, 9.x, 10.x (multi-target) |
| Npgsql.EntityFrameworkCore.PostgreSQL | Matching EF Core versions |

---

## Key Technical Concepts

### View Discovery
- Convention: `Views/` folder with `.sql` files (embedded resources)
- Fluent API: `modelBuilder.HasManagedView("vw_active_products", sql)` 
- File naming convention: `{ViewName}.sql` or `{Schema}.{ViewName}.sql`

### Migration Pipeline
```
dotnet ef migrations add AddViews
  → IDesignTimeServices hooks in
  → Scans view definitions (.sql + fluent)
  → Computes SHA256 hashes
  → Compares against snapshot
  → Generates MigrationOperations for changed views
  → Outputs migration with CreateView/DropView operations
```

### Tracking Table (`__ManagedViewsHistory`)
| Column | Type | Purpose |
|--------|------|---------|
| ViewName | text (PK) | Fully qualified view name |
| Schema | text | Database schema |
| Hash | text | SHA256 of the SQL definition |
| ViewType | text | Regular / Materialized |
| AppliedAt | timestamptz | When the view was created/updated |

### Snapshot File
- Generated alongside EF model snapshot
- Contains all view definitions and their hashes
- Enables offline diff detection in CI/CD pipelines

---

## Developer Experience Goals

```csharp
// Registration — one line in DbContext configuration
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseManagedViews());  // ← that's it

// Fluent API (optional, for simple views)
modelBuilder.HasManagedView("vw_active_products", mv => mv
    .AsSql("SELECT id, name, price FROM products WHERE is_active = true")
    .InSchema("catalog"));

// Materialized view with refresh
modelBuilder.HasManagedView("mv_product_stats", mv => mv
    .AsMaterialized()
    .AsSql("SELECT category_id, COUNT(*) as count FROM products GROUP BY category_id")
    .WithIndex("idx_mv_product_stats_category", "category_id"));

// Query as normal EF entity
var products = await context.ActiveProducts.ToListAsync();

// Refresh materialized view
await context.RefreshMaterializedView("mv_product_stats");
```

---

## Confirmation Checklist

| Decision | Choice |
|----------|--------|
| View definition source | .sql files + fluent API |
| Change detection | Tracking table + snapshot |
| Migration integration | IDesignTimeServices auto-generation |
| View types (PostgreSQL) | Regular + Materialized + Indexed |
| Update strategy | CREATE OR REPLACE (recreate) |
| Dependencies | Lean — auto-ordering + mat view refresh |
| Package name | `EntityFrameworkCore.ManagedViews` |
| .NET version | 8.0+ |
| EF Core versions | 8.x / 9.x / 10.x |
| First provider | PostgreSQL (Npgsql) |

---

**Status:** Awaiting confirmation to proceed with PRD.md and TDD.md generation.
