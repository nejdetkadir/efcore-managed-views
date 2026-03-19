using EntityFrameworkCore.ManagedViews.Models;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.ManagedViews.Migrations.Operations;

/// <summary>
/// Migration operation that creates a managed view.
/// </summary>
public sealed class CreateManagedViewOperation : MigrationOperation
{
    /// <summary>
    /// The name of the view.
    /// </summary>
    public string ViewName { get; set; } = null!;

    /// <summary>
    /// The database schema.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// The SQL body of the view.
    /// </summary>
    public string Sql { get; set; } = null!;

    /// <summary>
    /// The type of view.
    /// </summary>
    public ManagedViewType ViewType { get; set; }

    /// <summary>
    /// The hash of the view SQL.
    /// </summary>
    public string Hash { get; set; } = null!;

    /// <summary>
    /// Indexes to create on materialized views.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable CA1002 // Do not expose generic lists
    public List<ManagedViewIndex> Indexes { get; set; } = [];
#pragma warning restore CA1002
#pragma warning restore CA2227
}
