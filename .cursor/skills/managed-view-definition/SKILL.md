---
name: managed-view-definition
description: >
  Creates and configures managed view definitions in EntityFrameworkCore.ManagedViews.
  Use when adding new database views, configuring materialized views, setting up indexes,
  defining view dependencies, mapping entities to views, or working with SQL file directives.
---

# Defining Managed Views

## Two Approaches

### 1. SQL File Definitions (Recommended)

Create `.sql` files with directive comments:

```sql
-- @viewName: vw_active_products
-- @schema: public
-- @type: view
-- @dependsOn: vw_categories

SELECT p.id, p.name, p.price, c.name AS category_name
FROM products p
INNER JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true
```

Ensure embedded resource in `.csproj`:
```xml
<EmbeddedResource Include="Views/**/*.sql" />
```

### 2. Fluent API Definitions

```csharp
modelBuilder.HasManagedView("vw_active_products", v => v
    .InSchema("public")
    .AsSql("SELECT id, name, price FROM products WHERE is_active = true")
    .DependsOn("vw_categories"));
```

## Materialized Views

Set `@type: materialized` (SQL) or call `.AsMaterialized()` (fluent).

Add indexes with `@indexes: idx_name(col1); idx_other(col2 DESC)`.

Refresh at runtime:
```csharp
await context.RefreshMaterializedViewAsync("mv_stats");
await context.RefreshAllMaterializedViewsAsync(concurrently: true);
```

## Entity Mapping

```csharp
modelBuilder.Entity<ActiveProductView>(b =>
{
    b.HasNoKey();
    b.ToManagedView("vw_active_products");
});
```

## Dependencies

Views that reference other views must declare dependencies so the topological sort creates them in the correct order. Use `@dependsOn` directive or `.DependsOn()` fluent call.

## Drift Detection

```csharp
var report = await context.CheckViewDriftAsync();
// report.Entries contains Missing, Modified, or Untracked items
```

## Reference

- `@IMPLEMENTATION.md` for full API reference
- `@docs/ARCHITECTURE.md` for pipeline internals
