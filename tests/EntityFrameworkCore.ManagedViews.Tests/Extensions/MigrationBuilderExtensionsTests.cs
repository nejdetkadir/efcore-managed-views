using EntityFrameworkCore.ManagedViews.Extensions;
using EntityFrameworkCore.ManagedViews.Migrations.Operations;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Extensions;

public class MigrationBuilderExtensionsTests
{
    [Fact]
    public void CreateManagedView_AddsCreateOperation()
    {
        var builder = new MigrationBuilder("Npgsql");

        builder.CreateManagedView(
            name: "vw_active_products",
            schema: "public",
            sql: "SELECT id, name FROM products WHERE is_active = true");

        builder.Operations.Should().ContainSingle()
            .Which.Should().BeOfType<CreateManagedViewOperation>()
            .Which.ViewName.Should().Be("vw_active_products");
    }

    [Fact]
    public void CreateManagedView_SetsAllProperties()
    {
        var builder = new MigrationBuilder("Npgsql");
        var indexes = new[] { new ManagedViewIndex("idx_price", "price DESC") };

        builder.CreateManagedView(
            name: "mv_stats",
            schema: "catalog",
            sql: "SELECT COUNT(*) FROM products",
            viewType: ManagedViewType.Materialized,
            indexes: indexes);

        var op = builder.Operations.Should().ContainSingle()
            .Which.Should().BeOfType<CreateManagedViewOperation>().Subject;

        op.ViewName.Should().Be("mv_stats");
        op.Schema.Should().Be("catalog");
        op.Sql.Should().Be("SELECT COUNT(*) FROM products");
        op.ViewType.Should().Be(ManagedViewType.Materialized);
        op.Indexes.Should().ContainSingle()
            .Which.Name.Should().Be("idx_price");
    }

    [Fact]
    public void CreateManagedView_DefaultViewType_IsView()
    {
        var builder = new MigrationBuilder("Npgsql");

        builder.CreateManagedView("vw_test", "public", "SELECT 1");

        var op = (CreateManagedViewOperation)builder.Operations[0];
        op.ViewType.Should().Be(ManagedViewType.View);
    }

    [Fact]
    public void CreateManagedView_NullIndexes_DefaultsToEmpty()
    {
        var builder = new MigrationBuilder("Npgsql");

        builder.CreateManagedView("vw_test", "public", "SELECT 1");

        var op = (CreateManagedViewOperation)builder.Operations[0];
        op.Indexes.Should().BeEmpty();
    }

    [Fact]
    public void CreateManagedView_ReturnsSameBuilder_ForChaining()
    {
        var builder = new MigrationBuilder("Npgsql");

        var result = builder.CreateManagedView("vw_test", "public", "SELECT 1");

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void DropManagedView_AddsDropOperation()
    {
        var builder = new MigrationBuilder("Npgsql");

        builder.DropManagedView("vw_active_products", "public");

        builder.Operations.Should().ContainSingle()
            .Which.Should().BeOfType<DropManagedViewOperation>()
            .Which.ViewName.Should().Be("vw_active_products");
    }

    [Fact]
    public void DropManagedView_SetsAllProperties()
    {
        var builder = new MigrationBuilder("Npgsql");

        builder.DropManagedView("mv_stats", "catalog", ManagedViewType.Materialized);

        var op = builder.Operations.Should().ContainSingle()
            .Which.Should().BeOfType<DropManagedViewOperation>().Subject;

        op.ViewName.Should().Be("mv_stats");
        op.Schema.Should().Be("catalog");
        op.ViewType.Should().Be(ManagedViewType.Materialized);
    }

    [Fact]
    public void DropManagedView_DefaultViewType_IsView()
    {
        var builder = new MigrationBuilder("Npgsql");

        builder.DropManagedView("vw_test", "public");

        var op = (DropManagedViewOperation)builder.Operations[0];
        op.ViewType.Should().Be(ManagedViewType.View);
    }

    [Fact]
    public void DropManagedView_ReturnsSameBuilder_ForChaining()
    {
        var builder = new MigrationBuilder("Npgsql");

        var result = builder.DropManagedView("vw_test", "public");

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void MultipleOperations_CanBeChained()
    {
        var builder = new MigrationBuilder("Npgsql");

        builder
            .CreateManagedView("vw_base", "public", "SELECT 1")
            .CreateManagedView("vw_derived", "public", "SELECT * FROM vw_base")
            .DropManagedView("vw_old", "public");

        builder.Operations.Should().HaveCount(3);
        builder.Operations[0].Should().BeOfType<CreateManagedViewOperation>();
        builder.Operations[1].Should().BeOfType<CreateManagedViewOperation>();
        builder.Operations[2].Should().BeOfType<DropManagedViewOperation>();
    }
}
