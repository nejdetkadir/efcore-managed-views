using EntityFrameworkCore.ManagedViews.Migrations.Operations;
using EntityFrameworkCore.ManagedViews.Models;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkCore.ManagedViews.Extensions;

/// <summary>
/// Extension methods for <see cref="MigrationBuilder"/> to create and drop managed views.
/// </summary>
public static class MigrationBuilderExtensions
{
    /// <summary>
    /// Creates a managed view.
    /// </summary>
    public static MigrationBuilder CreateManagedView(
        this MigrationBuilder builder,
        string name,
        string schema,
        string sql,
        ManagedViewType viewType = ManagedViewType.View,
        ManagedViewIndex[]? indexes = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Operations.Add(new CreateManagedViewOperation
        {
            ViewName = name,
            Schema = schema,
            Sql = sql,
            ViewType = viewType,
            Indexes = indexes?.ToList() ?? []
        });

        return builder;
    }

    /// <summary>
    /// Drops a managed view.
    /// </summary>
    public static MigrationBuilder DropManagedView(
        this MigrationBuilder builder,
        string name,
        string schema,
        ManagedViewType viewType = ManagedViewType.View)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Operations.Add(new DropManagedViewOperation
        {
            ViewName = name,
            Schema = schema,
            ViewType = viewType
        });

        return builder;
    }
}
