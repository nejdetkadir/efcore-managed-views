using EntityFrameworkCore.ManagedViews.Models;

namespace EntityFrameworkCore.ManagedViews.Abstractions;

/// <summary>
/// Represents a managed database view definition that can be tracked,
/// versioned, and applied through EF Core migrations.
/// </summary>
public interface IManagedViewDefinition
{
    /// <summary>
    /// The name of the view in the database (e.g., "vw_active_products").
    /// </summary>
    string ViewName { get; }

    /// <summary>
    /// The database schema (e.g., "public", "catalog").
    /// </summary>
    string Schema { get; }

    /// <summary>
    /// The type of view: regular or materialized.
    /// </summary>
    ManagedViewType ViewType { get; }

    /// <summary>
    /// The raw SQL body of the view (the SELECT statement).
    /// Does NOT include CREATE VIEW prefix -- the provider generates the full DDL.
    /// </summary>
    string Sql { get; }

    /// <summary>
    /// Names of other managed views this view depends on.
    /// Used for topological ordering of CREATE/DROP operations.
    /// </summary>
    IReadOnlyList<string> DependsOn { get; }

    /// <summary>
    /// Indexes to create on the view (materialized views only).
    /// </summary>
    IReadOnlyList<ManagedViewIndex> Indexes { get; }

    /// <summary>
    /// The source of this definition (for diagnostics and error messages).
    /// Example: "Views/vw_active_products.sql" or "FluentApi:AppDbContext"
    /// </summary>
    string Source { get; }
}
