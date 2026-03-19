using EntityFrameworkCore.ManagedViews.Migrations.Operations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Migrations;

public sealed class RefreshMaterializedViewOperationTests
{
    [Fact]
    public void InheritsFromMigrationOperation()
    {
        var op = new RefreshMaterializedViewOperation();
        op.Should().BeAssignableTo<MigrationOperation>();
    }

    [Fact]
    public void DefaultSchema_IsPublic()
    {
        var op = new RefreshMaterializedViewOperation();
        op.Schema.Should().Be("public");
    }

    [Fact]
    public void DefaultConcurrently_IsFalse()
    {
        var op = new RefreshMaterializedViewOperation();
        op.Concurrently.Should().BeFalse();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var op = new RefreshMaterializedViewOperation
        {
            ViewName = "mv_stats",
            Schema = "analytics",
            Concurrently = true
        };

        op.ViewName.Should().Be("mv_stats");
        op.Schema.Should().Be("analytics");
        op.Concurrently.Should().BeTrue();
    }
}
