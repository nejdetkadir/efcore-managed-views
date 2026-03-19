# PostgreSQL Provider Reference

## Key Implementation Details

### GenerateCreateSql

- Regular views: `CREATE OR REPLACE VIEW "schema"."name" AS {sql}`
- Materialized views: `CREATE MATERIALIZED VIEW IF NOT EXISTS "schema"."name" AS {sql} WITH DATA`
- Materialized views do NOT support CREATE OR REPLACE

### GenerateDropSql

- Regular views: `DROP VIEW IF EXISTS "schema"."name"`
- Materialized views: `DROP MATERIALIZED VIEW IF EXISTS "schema"."name"`

### GenerateCreateIndexSql

- Only for materialized views
- `CREATE INDEX IF NOT EXISTS "idx_name" ON "schema"."view"(columns)`

### GenerateRefreshSql

- `REFRESH MATERIALIZED VIEW "schema"."name"` (standard)
- `REFRESH MATERIALIZED VIEW CONCURRENTLY "schema"."name"` (concurrent, requires unique index)

### SupportsCreateOrReplace

- Returns `true` for `ManagedViewType.View`
- Returns `false` for `ManagedViewType.MaterializedView`

### GenerateGetViewDefinitionSql

Queries `pg_catalog.pg_views` and `pg_catalog.pg_matviews` system catalogs.

## Source Files

- `@src/EntityFrameworkCore.ManagedViews.PostgreSQL/PostgreSqlManagedViewProvider.cs`
- `@src/EntityFrameworkCore.ManagedViews.PostgreSQL/PostgreSqlManagedViewMigrationsSqlGenerator.cs`
- `@src/EntityFrameworkCore.ManagedViews.PostgreSQL/Extensions/NpgsqlDbContextOptionsBuilderExtensions.cs`
