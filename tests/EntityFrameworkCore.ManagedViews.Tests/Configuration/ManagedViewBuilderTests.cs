using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Configuration;

public sealed class ManagedViewBuilderTests
{
    [Fact]
    public void InSchema_SetsSchema()
    {
        var builder = new ManagedViewBuilder();

        builder.InSchema("catalog");

        builder.SchemaValue.Should().Be("catalog");
    }

    [Fact]
    public void AsSql_SetsSql()
    {
        var builder = new ManagedViewBuilder();
        const string sql = "SELECT * FROM products";

        builder.AsSql(sql);

        builder.SqlValue.Should().Be(sql);
    }

    [Fact]
    public void AsMaterialized_SetsViewType()
    {
        var builder = new ManagedViewBuilder();

        builder.AsMaterialized();

        builder.ViewTypeValue.Should().Be(ManagedViewType.Materialized);
    }

    [Fact]
    public void DependsOn_AddsDependency()
    {
        var builder = new ManagedViewBuilder();

        builder.DependsOn("vw_base");

        builder.Dependencies.Should().ContainSingle("vw_base");
    }

    [Fact]
    public void DependsOn_Multiple_AddsAllDependencies()
    {
        var builder = new ManagedViewBuilder();

        builder.DependsOn("vw_a", "vw_b", "vw_c");

        builder.Dependencies.Should().BeEquivalentTo(["vw_a", "vw_b", "vw_c"]);
    }

    [Fact]
    public void HasIndex_AddsIndex()
    {
        var builder = new ManagedViewBuilder();

        builder.HasIndex("idx_category", "category_id");

        builder.IndexDefinitions.Should().ContainSingle();
        builder.IndexDefinitions[0].Name.Should().Be("idx_category");
        builder.IndexDefinitions[0].Columns.Should().Be("category_id");
    }
}
