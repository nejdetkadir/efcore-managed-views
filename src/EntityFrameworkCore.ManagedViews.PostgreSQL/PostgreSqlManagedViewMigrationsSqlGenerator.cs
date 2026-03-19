using EntityFrameworkCore.ManagedViews.Migrations.Operations;
using EntityFrameworkCore.ManagedViews.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace EntityFrameworkCore.ManagedViews.PostgreSQL;

/// <summary>
/// Extends Npgsql's migration SQL generator to handle ManagedView operations.
/// </summary>
public class PostgreSqlManagedViewMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
{
    private readonly PostgreSqlManagedViewProvider _viewProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlManagedViewMigrationsSqlGenerator"/> class.
    /// </summary>
    public PostgreSqlManagedViewMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
#pragma warning disable EF1001
        INpgsqlSingletonOptions npgsqlOptions,
#pragma warning restore EF1001
        PostgreSqlManagedViewProvider viewProvider)
        : base(dependencies, npgsqlOptions)
    {
        _viewProvider = viewProvider;
    }

    /// <inheritdoc />
    protected override void Generate(MigrationOperation operation,
        IModel? model, MigrationCommandListBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        switch (operation)
        {
            case CreateManagedViewOperation createOp:
                GenerateCreateView(createOp, builder);
                break;

            case DropManagedViewOperation dropOp:
                GenerateDropView(dropOp, builder);
                break;

            case RefreshMaterializedViewOperation refreshOp:
                GenerateRefreshView(refreshOp, builder);
                break;

            default:
                base.Generate(operation, model, builder);
                break;
        }
    }

    private void GenerateCreateView(CreateManagedViewOperation operation,
        MigrationCommandListBuilder builder)
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = operation.ViewName,
            Schema = operation.Schema,
            ViewType = operation.ViewType,
            Sql = operation.Sql,
            Indexes = operation.Indexes
        };

        if (operation.ViewType == ManagedViewType.Materialized)
        {
            builder.AppendLine(_viewProvider.GenerateDropSql(definition));
            builder.AppendLine(";");
            builder.EndCommand();
        }

        builder.AppendLine(_viewProvider.GenerateCreateSql(definition));
        builder.AppendLine(";");
        builder.EndCommand();

        foreach (string indexSql in _viewProvider.GenerateCreateIndexSql(definition))
        {
            builder.AppendLine(indexSql);
            builder.AppendLine(";");
            builder.EndCommand();
        }
    }

    private void GenerateDropView(DropManagedViewOperation operation,
        MigrationCommandListBuilder builder)
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = operation.ViewName,
            Schema = operation.Schema,
            ViewType = operation.ViewType
        };

        builder.AppendLine(_viewProvider.GenerateDropSql(definition));
        builder.AppendLine(";");
        builder.EndCommand();
    }

    private void GenerateRefreshView(RefreshMaterializedViewOperation operation,
        MigrationCommandListBuilder builder)
    {
        builder.AppendLine(
            _viewProvider.GenerateRefreshSql(
                operation.ViewName, operation.Schema, operation.Concurrently));
        builder.AppendLine(";");
        builder.EndCommand();
    }
}
