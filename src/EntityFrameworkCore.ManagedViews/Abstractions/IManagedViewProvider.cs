using EntityFrameworkCore.ManagedViews.Models;

namespace EntityFrameworkCore.ManagedViews.Abstractions;

/// <summary>
/// Database provider-specific implementation for generating DDL statements
/// for managed views. Each supported database (PostgreSQL, SQL Server, etc.)
/// provides its own implementation.
/// </summary>
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
