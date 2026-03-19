using EntityFrameworkCore.ManagedViews.Migrations.Operations;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Migrations;

public class DropManagedViewOperationTests
{
    [Fact]
    public void InheritsFromMigrationOperation()
    {
        var op = new DropManagedViewOperation();
        op.Should().BeAssignableTo<MigrationOperation>();
    }

    [Fact]
    public void DefaultSchema_IsPublic()
    {
        var op = new DropManagedViewOperation();
        op.Schema.Should().Be("public");
    }

    [Fact]
    public void DefaultViewType_IsView()
    {
        var op = new DropManagedViewOperation();
        op.ViewType.Should().Be(ManagedViewType.View);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var op = new DropManagedViewOperation
        {
            ViewName = "mv_stats",
            Schema = "reporting",
            ViewType = ManagedViewType.Materialized
        };

        op.ViewName.Should().Be("mv_stats");
        op.Schema.Should().Be("reporting");
        op.ViewType.Should().Be(ManagedViewType.Materialized);
    }
}
