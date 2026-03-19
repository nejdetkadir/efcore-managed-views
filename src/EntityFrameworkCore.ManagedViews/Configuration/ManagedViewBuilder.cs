using EntityFrameworkCore.ManagedViews.Models;

namespace EntityFrameworkCore.ManagedViews.Configuration;

/// <summary>
/// Fluent builder for configuring individual managed views.
/// </summary>
public sealed class ManagedViewBuilder
{
    internal string? SchemaValue { get; private set; }
    internal string? SqlValue { get; private set; }
    internal ManagedViewType ViewTypeValue { get; private set; } = ManagedViewType.View;
    internal List<string> Dependencies { get; } = [];
    internal List<ManagedViewIndex> IndexDefinitions { get; } = [];

    /// <summary>
    /// Sets the database schema for this view.
    /// </summary>
    public ManagedViewBuilder InSchema(string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        SchemaValue = schema;
        return this;
    }

    /// <summary>
    /// Sets the SQL body for this view.
    /// </summary>
    public ManagedViewBuilder AsSql(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        SqlValue = sql;
        return this;
    }

    /// <summary>
    /// Marks this view as a materialized view.
    /// </summary>
    public ManagedViewBuilder AsMaterialized()
    {
        ViewTypeValue = ManagedViewType.Materialized;
        return this;
    }

    /// <summary>
    /// Declares a dependency on another managed view.
    /// </summary>
    public ManagedViewBuilder DependsOn(string viewName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewName);
        Dependencies.Add(viewName);
        return this;
    }

    /// <summary>
    /// Declares dependencies on multiple managed views.
    /// </summary>
    public ManagedViewBuilder DependsOn(params string[] viewNames)
    {
        ArgumentNullException.ThrowIfNull(viewNames);
        foreach (string name in viewNames)
        {
            DependsOn(name);
        }
        return this;
    }

    /// <summary>
    /// Adds an index on this materialized view.
    /// </summary>
    public ManagedViewBuilder HasIndex(string indexName, string columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
        ArgumentException.ThrowIfNullOrWhiteSpace(columns);
        IndexDefinitions.Add(new ManagedViewIndex(indexName, columns));
        return this;
    }
}
