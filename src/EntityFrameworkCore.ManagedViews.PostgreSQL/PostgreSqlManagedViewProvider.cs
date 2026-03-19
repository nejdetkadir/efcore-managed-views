using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Exceptions;
using EntityFrameworkCore.ManagedViews.Models;

namespace EntityFrameworkCore.ManagedViews.PostgreSQL;

/// <summary>
/// PostgreSQL-specific SQL generation for managed views.
/// </summary>
public sealed class PostgreSqlManagedViewProvider : IManagedViewProvider
{
    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public IReadOnlyList<string> GenerateDropIndexSql(IManagedViewDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Indexes.Count == 0)
        {
            return [];
        }

        return definition.Indexes
            .Select(idx => $"DROP INDEX IF EXISTS \"{definition.Schema}\".\"{idx.Name}\"")
            .ToList();
    }

    /// <inheritdoc />
    public string GenerateRefreshSql(string viewName, string schema, bool concurrently)
    {
        string qualifiedName = QuoteName(schema, viewName);
        string concurrent = concurrently ? " CONCURRENTLY" : "";
        return $"REFRESH MATERIALIZED VIEW{concurrent} {qualifiedName}";
    }

    /// <inheritdoc />
    public bool SupportsCreateOrReplace(ManagedViewType viewType)
    {
        return viewType == ManagedViewType.View;
    }

    /// <inheritdoc />
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

    private static string QuoteName(string schema, string name)
    {
        return $"\"{schema}\".\"{name}\"";
    }
}
