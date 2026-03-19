using EntityFrameworkCore.ManagedViews.Migrations.Operations;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Migrations;

public class CreateManagedViewOperationTests
{
    [Fact]
    public void InheritsFromMigrationOperation()
    {
        var op = new CreateManagedViewOperation();
        op.Should().BeAssignableTo<MigrationOperation>();
    }

    [Fact]
    public void DefaultSchema_IsPublic()
    {
        var op = new CreateManagedViewOperation();
        op.Schema.Should().Be("public");
    }

    [Fact]
    public void DefaultViewType_IsView()
    {
        var op = new CreateManagedViewOperation();
        op.ViewType.Should().Be(ManagedViewType.View);
    }

    [Fact]
    public void DefaultIndexes_IsEmpty()
    {
        var op = new CreateManagedViewOperation();
        op.Indexes.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var op = new CreateManagedViewOperation
        {
            ViewName = "mv_test",
            Schema = "analytics",
            Sql = "SELECT COUNT(*) FROM orders",
            ViewType = ManagedViewType.Materialized,
            Hash = "abc123",
            Indexes = [new ManagedViewIndex("idx_count", "count")]
        };

        op.ViewName.Should().Be("mv_test");
        op.Schema.Should().Be("analytics");
        op.Sql.Should().Be("SELECT COUNT(*) FROM orders");
        op.ViewType.Should().Be(ManagedViewType.Materialized);
        op.Hash.Should().Be("abc123");
        op.Indexes.Should().ContainSingle();
    }
}
