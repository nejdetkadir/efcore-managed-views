using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.ManagedViews.Migrations.Operations;

/// <summary>
/// Migration operation that refreshes a materialized view.
/// </summary>
public sealed class RefreshMaterializedViewOperation : MigrationOperation
{
    /// <summary>
    /// The name of the materialized view.
    /// </summary>
    public string ViewName { get; set; } = null!;

    /// <summary>
    /// The database schema.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// Whether to refresh concurrently (requires a unique index).
    /// </summary>
    public bool Concurrently { get; set; }
}
