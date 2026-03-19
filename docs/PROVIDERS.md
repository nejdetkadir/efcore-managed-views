# Database Provider Implementation Guide

This guide explains how to implement database provider support for EntityFrameworkCore.ManagedViews. ManagedViews uses a provider abstraction pattern to support different database systems while keeping the core logic database-agnostic.

## Overview

ManagedViews generates DDL statements (CREATE VIEW, DROP VIEW, etc.) through database-specific providers. Each provider implements the `IManagedViewProvider` interface to generate SQL appropriate for its database system.

**Architecture:**

```
┌─────────────────────────────────────────────────────────┐
│                    ManagedViews Core                     │
│  (Discovery, Diffing, Hashing, Migration Operations)    │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
              ┌────────────────────────┐
              │  IManagedViewProvider  │
              └────────────────────────┘
                    │           │
         ┌──────────┴───┐   ┌───┴──────────┐
         ▼              ▼   ▼              ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│   PostgreSQL    │  │   SQL Server    │  │     MySQL       │
│    Provider     │  │    Provider     │  │    Provider     │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

The **PostgreSQL provider** (`EntityFrameworkCore.ManagedViews.PostgreSQL`) serves as the reference implementation.

## IManagedViewProvider Interface

The core abstraction for database-specific SQL generation:

```csharp
using EntityFrameworkCore.ManagedViews.Models;

namespace EntityFrameworkCore.ManagedViews.Abstractions;

public interface IManagedViewProvider
{
    /// <summary>
    /// Generates the SQL to create a view.
    /// </summary>
    string GenerateCreateSql(IManagedViewDefinition definition);

    /// <summary>
    /// Generates the SQL to drop a view.
    /// </summary>
    string GenerateDropSql(IManagedViewDefinition definition);

    /// <summary>
    /// Generates the SQL to create indexes on a materialized view.
    /// </summary>
    IReadOnlyList<string> GenerateCreateIndexSql(IManagedViewDefinition definition);

    /// <summary>
    /// Generates the SQL to drop indexes on a materialized view.
    /// </summary>
    IReadOnlyList<string> GenerateDropIndexSql(IManagedViewDefinition definition);

    /// <summary>
    /// Generates the SQL to refresh a materialized view.
    /// </summary>
    string GenerateRefreshSql(string viewName, string schema, bool concurrently);

    /// <summary>
    /// Whether this provider supports CREATE OR REPLACE for the given view type.
    /// </summary>
    bool SupportsCreateOrReplace(ManagedViewType viewType);

    /// <summary>
    /// Generates the SQL to query the actual view definition from the database.
    /// Used for drift detection.
    /// </summary>
    string GenerateGetViewDefinitionSql(string viewName, string schema);
}
```

### Method Responsibilities

| Method | Purpose |
|--------|---------|
| `GenerateCreateSql` | Create or replace a view/materialized view |
| `GenerateDropSql` | Drop a view with CASCADE (handle dependencies) |
| `GenerateCreateIndexSql` | Create indexes on materialized views (return empty list for regular views) |
| `GenerateDropIndexSql` | Drop indexes before view modification |
| `GenerateRefreshSql` | Refresh materialized view data (with optional concurrent refresh) |
| `SupportsCreateOrReplace` | Indicates if the database supports atomic create-or-replace |
| `GenerateGetViewDefinitionSql` | Query system catalogs for drift detection |

## Implementation Guide

Follow these steps to implement a new database provider.

### Step 1: Create a New Package

Create a new class library project with the naming convention `EntityFrameworkCore.ManagedViews.<DatabaseName>`:

```bash
dotnet new classlib -n EntityFrameworkCore.ManagedViews.SqlServer
```

Add required dependencies:

```xml
<ItemGroup>
  <PackageReference Include="EntityFrameworkCore.ManagedViews" Version="x.x.x" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
</ItemGroup>
```

### Step 2: Implement IManagedViewProvider

Create the provider class that implements `IManagedViewProvider`:

```csharp
using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Exceptions;
using EntityFrameworkCore.ManagedViews.Models;

namespace EntityFrameworkCore.ManagedViews.SqlServer;

public sealed class SqlServerManagedViewProvider : IManagedViewProvider
{
    public string GenerateCreateSql(IManagedViewDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        string qualifiedName = QuoteName(definition.Schema, definition.ViewName);

        return definition.ViewType switch
        {
            ManagedViewType.View =>
                $"CREATE OR ALTER VIEW {qualifiedName} AS\n{definition.Sql}",

            ManagedViewType.Materialized =>
                throw new ManagedViewProviderException(
                    "SQL Server does not support materialized views. Use indexed views instead."),

            _ => throw new ManagedViewProviderException(
                $"Unsupported view type: {definition.ViewType}")
        };
    }

    public string GenerateDropSql(IManagedViewDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        string qualifiedName = QuoteName(definition.Schema, definition.ViewName);

        return $"DROP VIEW IF EXISTS {qualifiedName}";
    }

    public IReadOnlyList<string> GenerateCreateIndexSql(IManagedViewDefinition definition)
    {
        // SQL Server indexed views require specific handling
        // The clustered index must be created WITH SCHEMABINDING
        return [];
    }

    public IReadOnlyList<string> GenerateDropIndexSql(IManagedViewDefinition definition)
    {
        return [];
    }

    public string GenerateRefreshSql(string viewName, string schema, bool concurrently)
    {
        // SQL Server indexed views auto-update; no manual refresh needed
        throw new ManagedViewProviderException(
            "SQL Server indexed views are automatically maintained.");
    }

    public bool SupportsCreateOrReplace(ManagedViewType viewType)
    {
        return viewType == ManagedViewType.View;
    }

    public string GenerateGetViewDefinitionSql(string viewName, string schema)
    {
        return $"""
            SELECT m.definition
            FROM sys.sql_modules m
            INNER JOIN sys.objects o ON m.object_id = o.object_id
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE o.name = '{viewName}' AND s.name = '{schema}' AND o.type = 'V'
            """;
    }

    private static string QuoteName(string schema, string name)
    {
        return $"[{schema}].[{name}]";
    }
}
```

### Step 3: Handle View Types Appropriately

The `ManagedViewType` enum defines supported view types:

```csharp
public enum ManagedViewType
{
    View = 0,        // Standard database view (CREATE VIEW)
    Materialized = 1 // Materialized view with stored data
}
```

Provider implementations must handle each type according to database capabilities:

| Database | Regular Views | Materialized Views |
|----------|--------------|-------------------|
| PostgreSQL | CREATE OR REPLACE VIEW | CREATE MATERIALIZED VIEW IF NOT EXISTS |
| SQL Server | CREATE OR ALTER VIEW | Not supported (use indexed views) |
| MySQL | CREATE OR REPLACE VIEW | Not supported |
| Oracle | CREATE OR REPLACE VIEW | CREATE MATERIALIZED VIEW |

### Step 4: Create Extension Method

Create a fluent extension method for `DbContextOptionsBuilder`:

```csharp
using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.ManagedViews.SqlServer.Extensions;

public static class SqlServerDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Enables managed view support for a SQL Server DbContext.
    /// </summary>
    public static DbContextOptionsBuilder UseSqlServerManagedViews(
        this DbContextOptionsBuilder builder,
        Action<ManagedViewOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        // Register core ManagedViews services
        builder.UseManagedViews(configure);

        // Register SQL Server-specific provider
        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(
            new SqlServerManagedViewOptionsExtension());

        return builder;
    }
}
```

### Step 5: Implement IDbContextOptionsExtension

Create the options extension to register services:

```csharp
public sealed class SqlServerManagedViewOptionsExtension : IDbContextOptionsExtension
{
    public DbContextOptionsExtensionInfo Info => new ExtInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        // Register the SQL Server provider implementation
        services.AddSingleton<IManagedViewProvider, SqlServerManagedViewProvider>();
    }

    public void Validate(IDbContextOptions options) { }

    private sealed class ExtInfo(IDbContextOptionsExtension extension) 
        : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "SqlServerManagedViews ";
        public override int GetServiceProviderHashCode() => 0;
        
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) 
            => other is ExtInfo;
        
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["SqlServerManagedViews:Enabled"] = "true";
    }
}
```

### Step 6: Optional - Custom Migration SQL Generator

For migration support, you may need to customize the migrations SQL generator:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public class SqlServerManagedViewMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
{
    private readonly IManagedViewProvider _provider;

    public SqlServerManagedViewMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        ICommandBatchPreparer commandBatchPreparer,
        IManagedViewProvider provider)
        : base(dependencies, commandBatchPreparer)
    {
        _provider = provider;
    }

    protected override void Generate(
        CreateManagedViewOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        var definition = operation.ToDefinition();
        var sql = _provider.GenerateCreateSql(definition);
        
        builder.AppendLine(sql);
        builder.EndCommand();
    }
}
```

Register it in your extension:

```csharp
public void ApplyServices(IServiceCollection services)
{
    services.AddSingleton<IManagedViewProvider, SqlServerManagedViewProvider>();
    services.AddScoped<IMigrationsSqlGenerator, SqlServerManagedViewMigrationsSqlGenerator>();
}
```

## PostgreSQL Reference Implementation

The PostgreSQL provider demonstrates the complete implementation pattern.

### Create SQL Generation

```csharp
public string GenerateCreateSql(IManagedViewDefinition definition)
{
    ArgumentNullException.ThrowIfNull(definition);
    string qualifiedName = QuoteName(definition.Schema, definition.ViewName);

    return definition.ViewType switch
    {
        ManagedViewType.View =>
            $"CREATE OR REPLACE VIEW {qualifiedName} AS\n{definition.Sql}",

        ManagedViewType.Materialized =>
            $"CREATE MATERIALIZED VIEW IF NOT EXISTS {qualifiedName} AS\n{definition.Sql}\nWITH DATA",

        _ => throw new ManagedViewProviderException(
            $"Unsupported view type: {definition.ViewType}")
    };
}
```

**Key points:**
- Regular views use `CREATE OR REPLACE` (atomic operation)
- Materialized views use `CREATE IF NOT EXISTS` (no CREATE OR REPLACE support in PostgreSQL)
- `WITH DATA` populates the materialized view immediately

### Drop SQL Generation

```csharp
public string GenerateDropSql(IManagedViewDefinition definition)
{
    ArgumentNullException.ThrowIfNull(definition);
    string qualifiedName = QuoteName(definition.Schema, definition.ViewName);

    return definition.ViewType switch
    {
        ManagedViewType.View =>
            $"DROP VIEW IF EXISTS {qualifiedName} CASCADE",

        ManagedViewType.Materialized =>
            $"DROP MATERIALIZED VIEW IF EXISTS {qualifiedName} CASCADE",

        _ => throw new ManagedViewProviderException(
            $"Unsupported view type: {definition.ViewType}")
    };
}
```

**Key points:**
- Different DDL keywords for view types
- `CASCADE` handles dependent objects
- `IF EXISTS` prevents errors on missing views

### Index Management

```csharp
public IReadOnlyList<string> GenerateCreateIndexSql(IManagedViewDefinition definition)
{
    ArgumentNullException.ThrowIfNull(definition);
    if (definition.ViewType != ManagedViewType.Materialized || definition.Indexes.Count == 0)
    {
        return [];
    }

    string qualifiedName = QuoteName(definition.Schema, definition.ViewName);

    return definition.Indexes
        .Select(idx =>
            $"CREATE INDEX IF NOT EXISTS \"{idx.Name}\" ON {qualifiedName} ({idx.Columns})")
        .ToList();
}
```

**Key points:**
- Only materialized views support indexes
- `IF NOT EXISTS` for idempotent operations
- Returns empty list for regular views

### Materialized View Refresh

```csharp
public string GenerateRefreshSql(string viewName, string schema, bool concurrently)
{
    string qualifiedName = QuoteName(schema, viewName);
    string concurrent = concurrently ? " CONCURRENTLY" : "";
    return $"REFRESH MATERIALIZED VIEW{concurrent} {qualifiedName}";
}
```

**Key points:**
- `CONCURRENTLY` allows queries during refresh (requires unique index)
- Non-concurrent refresh locks the view

### Drift Detection Query

```csharp
public string GenerateGetViewDefinitionSql(string viewName, string schema)
{
    return $"""
        SELECT definition
        FROM pg_catalog.pg_views
        WHERE viewname = '{viewName}' AND schemaname = '{schema}'
        UNION ALL
        SELECT definition
        FROM pg_catalog.pg_matviews
        WHERE matviewname = '{viewName}' AND schemaname = '{schema}'
        """;
}
```

**Key points:**
- Queries PostgreSQL system catalogs
- Handles both view types with UNION
- Returns the stored SQL definition

## SQL Server Example

A hypothetical SQL Server implementation showing database-specific differences.

### Key Differences from PostgreSQL

| Feature | PostgreSQL | SQL Server |
|---------|------------|------------|
| View Creation | `CREATE OR REPLACE VIEW` | `CREATE OR ALTER VIEW` |
| Identifier Quoting | `"schema"."name"` | `[schema].[name]` |
| Materialized Views | Native support | Use indexed views |
| View Refresh | `REFRESH MATERIALIZED VIEW` | Automatic (indexed views) |
| System Catalog | `pg_catalog.pg_views` | `sys.sql_modules` |

### Indexed Views (SQL Server Alternative)

SQL Server uses **indexed views** instead of materialized views:

```sql
-- Create view with SCHEMABINDING (required for indexed views)
CREATE VIEW [dbo].[OrderSummary]
WITH SCHEMABINDING
AS
SELECT 
    CustomerId,
    COUNT_BIG(*) AS OrderCount,
    SUM(Total) AS TotalAmount
FROM dbo.Orders
GROUP BY CustomerId;

-- Create unique clustered index (materializes the view)
CREATE UNIQUE CLUSTERED INDEX IX_OrderSummary 
ON [dbo].[OrderSummary](CustomerId);
```

Indexed views have restrictions:
- Must use `SCHEMABINDING`
- Limited to specific functions and operations
- Automatically maintained (no manual refresh)

### System Catalog Query

```csharp
public string GenerateGetViewDefinitionSql(string viewName, string schema)
{
    return $"""
        SELECT m.definition
        FROM sys.sql_modules m
        INNER JOIN sys.objects o ON m.object_id = o.object_id
        INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
        WHERE o.name = '{viewName}' AND s.name = '{schema}' AND o.type = 'V'
        """;
}
```

## Testing Your Provider

### Unit Tests for DDL Generation

Test each method with various inputs:

```csharp
public sealed class SqlServerManagedViewProviderTests
{
    private readonly SqlServerManagedViewProvider _sut = new();

    [Fact]
    public void GenerateCreateSql_RegularView_ReturnsCreateOrAlter()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyView",
            Schema = "dbo",
            ViewType = ManagedViewType.View,
            Sql = "SELECT 1 AS Id",
            Source = "Test"
        };

        var result = _sut.GenerateCreateSql(definition);

        result.Should().Contain("CREATE OR ALTER VIEW");
        result.Should().Contain("[dbo].[MyView]");
        result.Should().Contain("SELECT 1 AS Id");
    }

    [Fact]
    public void GenerateCreateSql_MaterializedView_ThrowsNotSupported()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyMatView",
            Schema = "dbo",
            ViewType = ManagedViewType.Materialized,
            Sql = "SELECT 1 AS Id",
            Source = "Test"
        };

        var act = () => _sut.GenerateCreateSql(definition);

        act.Should().Throw<ManagedViewProviderException>()
           .WithMessage("*does not support materialized views*");
    }

    [Fact]
    public void GenerateDropSql_ReturnsDropViewIfExists()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyView",
            Schema = "dbo",
            ViewType = ManagedViewType.View,
            Sql = "SELECT 1",
            Source = "Test"
        };

        var result = _sut.GenerateDropSql(definition);

        result.Should().Be("DROP VIEW IF EXISTS [dbo].[MyView]");
    }

    [Fact]
    public void SupportsCreateOrReplace_View_ReturnsTrue()
    {
        var result = _sut.SupportsCreateOrReplace(ManagedViewType.View);

        result.Should().BeTrue();
    }

    [Fact]
    public void GenerateGetViewDefinitionSql_QueriesSysModules()
    {
        var result = _sut.GenerateGetViewDefinitionSql("MyView", "dbo");

        result.Should().Contain("sys.sql_modules");
        result.Should().Contain("o.name = 'MyView'");
        result.Should().Contain("s.name = 'dbo'");
    }
}
```

### Integration Tests with Testcontainers

Use Testcontainers for real database testing:

```csharp
using Testcontainers.MsSql;

public sealed class SqlServerIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task CreateView_ExecutesSuccessfully()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .UseSqlServerManagedViews();

        await using var context = new TestDbContext(builder.Options);
        await context.Database.EnsureCreatedAsync();

        // Create a view
        var provider = context.GetService<IManagedViewProvider>();
        var definition = new ManagedViewDefinition
        {
            ViewName = "TestView",
            Schema = "dbo",
            ViewType = ManagedViewType.View,
            Sql = "SELECT 1 AS Id",
            Source = "Test"
        };

        var sql = provider.GenerateCreateSql(definition);
        await context.Database.ExecuteSqlRawAsync(sql);

        // Verify view exists
        var exists = await context.Database.ExecuteSqlRawAsync(
            "SELECT * FROM [dbo].[TestView]");
        
        exists.Should().BeGreaterThan(-1);
    }

    [Fact]
    public async Task ApplyManagedViewsAsync_CreatesViewsFromFiles()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .UseSqlServerManagedViews(options =>
            {
                options.ViewsDirectory = "Views";
            });

        await using var context = new TestDbContext(builder.Options);
        await context.Database.EnsureCreatedAsync();

        await context.ApplyManagedViewsAsync();

        // Verify views were created
        var history = context.GetService<IManagedViewHistoryRepository>();
        var views = await history.GetAllAsync();
        
        views.Should().NotBeEmpty();
    }
}
```

### Test Project Structure

```
tests/
├── EntityFrameworkCore.ManagedViews.SqlServer.Tests/
│   ├── SqlServerManagedViewProviderTests.cs      # Unit tests
│   ├── SqlServerIntegrationTests.cs              # Testcontainers tests
│   ├── SqlServerMigrationsSqlGeneratorTests.cs   # Migration tests
│   └── Infrastructure/
│       └── SqlServerFixture.cs                   # Test fixtures
```

### Test Container Setup

```csharp
[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Your_password123")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

## Checklist for New Providers

Before publishing your provider, verify:

- [ ] All `IManagedViewProvider` methods implemented
- [ ] Proper identifier quoting for the database
- [ ] Appropriate handling of unsupported features (throw `ManagedViewProviderException`)
- [ ] Extension method follows naming convention (`Use<Database>ManagedViews`)
- [ ] `IDbContextOptionsExtension` registers provider as singleton
- [ ] Unit tests cover all DDL generation methods
- [ ] Integration tests verify actual database operations
- [ ] README documents any database-specific limitations
- [ ] NuGet package metadata configured

## See Also

- [README.md](../README.md) - Getting started guide
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Contributing guidelines
- [PostgreSQL Provider Source](../src/EntityFrameworkCore.ManagedViews.PostgreSQL/) - Reference implementation
