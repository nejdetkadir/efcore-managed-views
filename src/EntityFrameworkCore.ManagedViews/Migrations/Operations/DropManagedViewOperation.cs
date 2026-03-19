using EntityFrameworkCore.ManagedViews.Models;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.ManagedViews.Migrations.Operations;

/// <summary>
/// Migration operation that drops a managed view.
/// </summary>
public sealed class DropManagedViewOperation : MigrationOperation
{
    /// <summary>
    /// The name of the view to drop.
    /// </summary>
    public string ViewName { get; set; } = null!;

    /// <summary>
    /// The database schema.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// The type of view.
    /// </summary>
    public ManagedViewType ViewType { get; set; }
}
