using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.Diffing;
using EntityFrameworkCore.ManagedViews.Extensions;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.Migrations.Operations;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.PostgreSQL;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.FunctionalTests;

/// <summary>
/// Tests the full pipeline: discover views -> diff -> resolve order ->
/// generate migration operations -> generate SQL.
/// Verifies that all components integrate correctly without a database.
/// </summary>
public class MigrationOperationPipelineTests
{
    private readonly Sha256ViewHasher _hasher;
    private readonly ManagedViewDiffer _differ;
    private readonly TopologicalSortResolver _resolver;
    private readonly PostgreSqlManagedViewProvider _provider;

    public MigrationOperationPipelineTests()
    {
        _hasher = new Sha256ViewHasher(Options.Create(new ManagedViewOptions()));
        _differ = new ManagedViewDiffer(_hasher);
        _resolver = new TopologicalSortResolver();
        _provider = new PostgreSqlManagedViewProvider();
    }

    [Fact]
    public void Pipeline_NewViews_ProducesCorrectCreateOperationsAndSql()
    {
        var baseView = new ManagedViewDefinition
        {
            ViewName = "vw_orders",
            Schema = "sales",
            ViewType = ManagedViewType.View,
            Sql = "SELECT id, total, customer_id FROM orders WHERE status = 'confirmed'",
            Source = "test"
        };

        var summaryView = new ManagedViewDefinition
        {
            ViewName = "mv_order_totals",
            Schema = "sales",
            ViewType = ManagedViewType.Materialized,
            Sql = "SELECT customer_id, SUM(total) as lifetime_total FROM vw_orders GROUP BY customer_id",
            DependsOn = ["vw_orders"],
            Indexes = [new ManagedViewIndex("idx_customer", "customer_id")],
            Source = "test"
        };

        // Step 1: Diff (no previous snapshot = all added)
        var diffs = _differ.ComputeDiff([baseView, summaryView], null);
        diffs.Should().HaveCount(2);
        diffs.Should().OnlyContain(d => d.DiffType == ManagedViewDiffType.Added);

        // Step 2: Resolve dependency order for creates
        var addedDefs = diffs.Select(d => d.Definition).ToList();
        var createOrder = _resolver.ResolveCreateOrder(addedDefs);

        createOrder[0].ViewName.Should().Be("vw_orders", "base view must be created first");
        createOrder[1].ViewName.Should().Be("mv_order_totals", "dependent view created second");

        // Step 3: Generate migration operations via MigrationBuilder
        var builder = new MigrationBuilder("Npgsql");
        foreach (var def in createOrder)
        {
            builder.CreateManagedView(
                name: def.ViewName,
                schema: def.Schema,
                sql: def.Sql,
                viewType: def.ViewType,
                indexes: def.Indexes.ToArray());
        }

        builder.Operations.Should().HaveCount(2);
        var createOps = builder.Operations.Cast<CreateManagedViewOperation>().ToList();
        createOps[0].ViewName.Should().Be("vw_orders");
        createOps[1].ViewName.Should().Be("mv_order_totals");
        createOps[1].Indexes.Should().ContainSingle().Which.Name.Should().Be("idx_customer");

        // Step 4: Generate SQL from definitions
        string viewSql = _provider.GenerateCreateSql(baseView);
        viewSql.Should().StartWith("CREATE OR REPLACE VIEW");
        viewSql.Should().Contain("\"sales\".\"vw_orders\"");

        string matViewSql = _provider.GenerateCreateSql(summaryView);
        matViewSql.Should().StartWith("CREATE MATERIALIZED VIEW IF NOT EXISTS");
        matViewSql.Should().Contain("\"sales\".\"mv_order_totals\"");
        matViewSql.Should().Contain("WITH DATA");

        var indexSqls = _provider.GenerateCreateIndexSql(summaryView);
        indexSqls.Should().ContainSingle()
            .Which.Should().Contain("idx_customer");
    }

    [Fact]
    public void Pipeline_ModifiedView_ProducesDropThenCreate()
    {
        var originalSql = "SELECT id, name FROM products";
        var modifiedSql = "SELECT id, name, price FROM products WHERE is_active = true";

        var view = new ManagedViewDefinition
        {
            ViewName = "vw_products",
            Schema = "public",
            ViewType = ManagedViewType.Materialized,
            Sql = modifiedSql,
            Source = "test"
        };

        var snapshot = new Snapshot.ManagedViewSnapshot
        {
            Views =
            [
                new Snapshot.ManagedViewSnapshotEntry
                {
                    ViewName = "vw_products",
                    Schema = "public",
                    Type = "Materialized",
                    Hash = _hasher.ComputeHash(originalSql),
                    Sql = originalSql
                }
            ]
        };

        var diffs = _differ.ComputeDiff([view], snapshot);
        diffs.Should().ContainSingle()
            .Which.DiffType.Should().Be(ManagedViewDiffType.Modified);

        // For materialized views, modification requires drop + create
        var diff = diffs[0];
        var builder = new MigrationBuilder("Npgsql");

        if (!_provider.SupportsCreateOrReplace(diff.Definition.ViewType))
        {
            builder.DropManagedView(diff.Definition.ViewName, diff.Definition.Schema, diff.Definition.ViewType);
        }

        builder.CreateManagedView(
            name: diff.Definition.ViewName,
            schema: diff.Definition.Schema,
            sql: diff.Definition.Sql,
            viewType: diff.Definition.ViewType);

        builder.Operations.Should().HaveCount(2);
        builder.Operations[0].Should().BeOfType<DropManagedViewOperation>();
        builder.Operations[1].Should().BeOfType<CreateManagedViewOperation>();
    }

    [Fact]
    public void Pipeline_RemovedView_ProducesDropOperation()
    {
        var snapshot = new Snapshot.ManagedViewSnapshot
        {
            Views =
            [
                new Snapshot.ManagedViewSnapshotEntry
                {
                    ViewName = "vw_old",
                    Schema = "public",
                    Type = "View",
                    Hash = "somehash",
                    Sql = "SELECT 1"
                }
            ]
        };

        var diffs = _differ.ComputeDiff([], snapshot);
        diffs.Should().ContainSingle()
            .Which.DiffType.Should().Be(ManagedViewDiffType.Removed);

        var builder = new MigrationBuilder("Npgsql");
        foreach (var diff in diffs.Where(d => d.DiffType == ManagedViewDiffType.Removed))
        {
            builder.DropManagedView(diff.Definition.ViewName, diff.Definition.Schema, diff.Definition.ViewType);
        }

        builder.Operations.Should().ContainSingle()
            .Which.Should().BeOfType<DropManagedViewOperation>()
            .Which.ViewName.Should().Be("vw_old");

        string dropSql = _provider.GenerateDropSql(new ManagedViewDefinition
        {
            ViewName = "vw_old",
            Schema = "public",
            ViewType = ManagedViewType.View,
            Source = "snapshot"
        });

        dropSql.Should().Be("DROP VIEW IF EXISTS \"public\".\"vw_old\" CASCADE");
    }

    [Fact]
    public void Pipeline_ComplexScenario_MixedOperationsInCorrectOrder()
    {
        var keepView = new ManagedViewDefinition
        {
            ViewName = "vw_keep",
            Schema = "public",
            Sql = "SELECT 1",
            Source = "test"
        };

        var modifiedView = new ManagedViewDefinition
        {
            ViewName = "vw_modified",
            Schema = "public",
            Sql = "SELECT id, name, UPPER(name) as upper_name FROM items",
            Source = "test"
        };

        var newView = new ManagedViewDefinition
        {
            ViewName = "vw_new",
            Schema = "public",
            Sql = "SELECT * FROM vw_keep",
            DependsOn = ["vw_keep"],
            Source = "test"
        };

        var snapshot = new Snapshot.ManagedViewSnapshot
        {
            Views =
            [
                new Snapshot.ManagedViewSnapshotEntry
                {
                    ViewName = "vw_keep",
                    Schema = "public",
                    Type = "View",
                    Hash = _hasher.ComputeHash("SELECT 1"),
                    Sql = "SELECT 1"
                },
                new Snapshot.ManagedViewSnapshotEntry
                {
                    ViewName = "vw_modified",
                    Schema = "public",
                    Type = "View",
                    Hash = _hasher.ComputeHash("SELECT id, name FROM items"),
                    Sql = "SELECT id, name FROM items"
                },
                new Snapshot.ManagedViewSnapshotEntry
                {
                    ViewName = "vw_removed",
                    Schema = "public",
                    Type = "View",
                    Hash = _hasher.ComputeHash("SELECT old_stuff FROM old_table"),
                    Sql = "SELECT old_stuff FROM old_table"
                }
            ]
        };

        var diffs = _differ.ComputeDiff([keepView, modifiedView, newView], snapshot);

        var added = diffs.Where(d => d.DiffType == ManagedViewDiffType.Added).ToList();
        var modified = diffs.Where(d => d.DiffType == ManagedViewDiffType.Modified).ToList();
        var removed = diffs.Where(d => d.DiffType == ManagedViewDiffType.Removed).ToList();

        added.Should().ContainSingle().Which.Definition.ViewName.Should().Be("vw_new");
        modified.Should().ContainSingle().Which.Definition.ViewName.Should().Be("vw_modified");
        removed.Should().ContainSingle().Which.Definition.ViewName.Should().Be("vw_removed");

        // vw_keep has the same hash, so it's not in the diff at all
        diffs.Should().NotContain(d => d.Definition.ViewName == "vw_keep");
    }
}
