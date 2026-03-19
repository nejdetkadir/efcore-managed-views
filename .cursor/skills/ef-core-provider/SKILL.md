---
name: ef-core-provider
description: >
  Implements a new database provider for EntityFrameworkCore.ManagedViews.
  Use when adding support for SQL Server, MySQL, SQLite, or any other database.
  Covers IManagedViewProvider implementation, IDbContextOptionsExtension registration,
  DDL generation, and migration SQL generator extension.
---

# Implementing a Database Provider

## Overview

ManagedViews uses a provider abstraction to support different databases. Each provider implements `IManagedViewProvider` to generate database-specific DDL.

## Steps

### 1. Create Package

```bash
dotnet new classlib -n EntityFrameworkCore.ManagedViews.{Database} -f net10.0
dotnet sln add src/EntityFrameworkCore.ManagedViews.{Database}
```

Add reference to core:
```xml
<ProjectReference Include="../EntityFrameworkCore.ManagedViews/EntityFrameworkCore.ManagedViews.csproj" />
```

### 2. Implement IManagedViewProvider

The interface has 7 methods:

| Method | Purpose |
|--------|---------|
| `GenerateCreateSql` | CREATE VIEW / CREATE MATERIALIZED VIEW |
| `GenerateDropSql` | DROP VIEW / DROP MATERIALIZED VIEW |
| `GenerateCreateIndexSql` | CREATE INDEX for materialized views |
| `GenerateDropIndexSql` | DROP INDEX for materialized views |
| `GenerateRefreshSql` | REFRESH MATERIALIZED VIEW |
| `SupportsCreateOrReplace` | Whether CREATE OR REPLACE is available |
| `GenerateGetViewDefinitionSql` | Query to read current view definition from system catalog |

### 3. Create IDbContextOptionsExtension

Register the provider into EF Core's internal DI:

```csharp
public sealed class {Database}ManagedViewOptionsExtension : IDbContextOptionsExtension
{
    public void ApplyServices(IServiceCollection services)
    {
        services.TryAddSingleton<IManagedViewProvider, {Database}ManagedViewProvider>();
    }
    // ... Info, Validate, etc.
}
```

### 4. Create Extension Method

```csharp
public static DbContextOptionsBuilder Use{Database}ManagedViews(
    this DbContextOptionsBuilder builder)
{
    builder.UseManagedViews();
    var extension = new {Database}ManagedViewOptionsExtension();
    ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);
    return builder;
}
```

### 5. Extend Migration SQL Generator (Optional)

Override `Generate(MigrationOperation, IModel, MigrationCommandListBuilder)` to handle `CreateManagedViewOperation`, `DropManagedViewOperation`, and `RefreshMaterializedViewOperation`.

## Reference

See `references/` for the PostgreSQL implementation as a working example.
See `@docs/PROVIDERS.md` for the complete provider guide.
