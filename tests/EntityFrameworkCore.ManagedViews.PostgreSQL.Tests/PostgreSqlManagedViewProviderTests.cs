using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.PostgreSQL;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.PostgreSQL.Tests;

public sealed class PostgreSqlManagedViewProviderTests
{
    private readonly PostgreSqlManagedViewProvider _sut = new();

    [Fact]
    public void GenerateCreateSql_RegularView_ReturnsCreateOrReplace()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyView",
            Schema = "public",
            ViewType = ManagedViewType.View,
            Sql = "SELECT 1 AS Id",
            Source = "Test"
        };

        var result = _sut.GenerateCreateSql(definition);

        result.Should().Contain("CREATE OR REPLACE VIEW");
        result.Should().Contain("\"public\".\"MyView\"");
        result.Should().Contain("SELECT 1 AS Id");
    }

    [Fact]
    public void GenerateCreateSql_MaterializedView_ReturnsCreateMaterialized()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyMatView",
            Schema = "public",
            ViewType = ManagedViewType.Materialized,
            Sql = "SELECT 1 AS Id",
            Source = "Test"
        };

        var result = _sut.GenerateCreateSql(definition);

        result.Should().Contain("CREATE MATERIALIZED VIEW IF NOT EXISTS");
        result.Should().Contain("\"public\".\"MyMatView\"");
        result.Should().Contain("SELECT 1 AS Id");
        result.Should().Contain("WITH DATA");
    }

    [Fact]
    public void GenerateDropSql_RegularView_ReturnsDropView()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyView",
            Schema = "public",
            ViewType = ManagedViewType.View,
            Sql = "SELECT 1",
            Source = "Test"
        };

        var result = _sut.GenerateDropSql(definition);

        result.Should().Be("DROP VIEW IF EXISTS \"public\".\"MyView\" CASCADE");
    }

    [Fact]
    public void GenerateDropSql_MaterializedView_ReturnsDropMaterializedView()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyMatView",
            Schema = "public",
            ViewType = ManagedViewType.Materialized,
            Sql = "SELECT 1",
            Source = "Test"
        };

        var result = _sut.GenerateDropSql(definition);

        result.Should().Be("DROP MATERIALIZED VIEW IF EXISTS \"public\".\"MyMatView\" CASCADE");
    }

    [Fact]
    public void GenerateCreateIndexSql_MaterializedViewWithIndexes_ReturnsIndexStatements()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyMatView",
            Schema = "public",
            ViewType = ManagedViewType.Materialized,
            Sql = "SELECT 1 AS Id",
            Source = "Test",
            Indexes =
            [
                new ManagedViewIndex("IX_MyMatView_Id", "Id"),
                new ManagedViewIndex("IX_MyMatView_Name", "Name")
            ]
        };

        var result = _sut.GenerateCreateIndexSql(definition);

        result.Should().HaveCount(2);
        result.Should().Contain("CREATE INDEX IF NOT EXISTS \"IX_MyMatView_Id\" ON \"public\".\"MyMatView\" (Id)");
        result.Should().Contain("CREATE INDEX IF NOT EXISTS \"IX_MyMatView_Name\" ON \"public\".\"MyMatView\" (Name)");
    }

    [Fact]
    public void GenerateCreateIndexSql_RegularView_ReturnsEmpty()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyView",
            Schema = "public",
            ViewType = ManagedViewType.View,
            Sql = "SELECT 1",
            Source = "Test",
            Indexes = [new ManagedViewIndex("IX_MyView_Id", "Id")]
        };

        var result = _sut.GenerateCreateIndexSql(definition);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateDropIndexSql_WithIndexes_ReturnsDropStatements()
    {
        var definition = new ManagedViewDefinition
        {
            ViewName = "MyMatView",
            Schema = "public",
            ViewType = ManagedViewType.Materialized,
            Sql = "SELECT 1",
            Source = "Test",
            Indexes =
            [
                new ManagedViewIndex("IX_MyMatView_Id", "Id"),
                new ManagedViewIndex("IX_MyMatView_Name", "Name")
            ]
        };

        var result = _sut.GenerateDropIndexSql(definition);

        result.Should().HaveCount(2);
        result.Should().Contain("DROP INDEX IF EXISTS \"public\".\"IX_MyMatView_Id\"");
        result.Should().Contain("DROP INDEX IF EXISTS \"public\".\"IX_MyMatView_Name\"");
    }

    [Fact]
    public void GenerateRefreshSql_Normal_ReturnsRefresh()
    {
        var result = _sut.GenerateRefreshSql("MyMatView", "public", concurrently: false);

        result.Should().Be("REFRESH MATERIALIZED VIEW \"public\".\"MyMatView\"");
    }

    [Fact]
    public void GenerateRefreshSql_Concurrently_ReturnsRefreshConcurrently()
    {
        var result = _sut.GenerateRefreshSql("MyMatView", "public", concurrently: true);

        result.Should().Be("REFRESH MATERIALIZED VIEW CONCURRENTLY \"public\".\"MyMatView\"");
    }

    [Fact]
    public void SupportsCreateOrReplace_View_ReturnsTrue()
    {
        var result = _sut.SupportsCreateOrReplace(ManagedViewType.View);

        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsCreateOrReplace_Materialized_ReturnsFalse()
    {
        var result = _sut.SupportsCreateOrReplace(ManagedViewType.Materialized);

        result.Should().BeFalse();
    }

    [Fact]
    public void GenerateGetViewDefinitionSql_ReturnsQueryAgainstPgCatalog()
    {
        var result = _sut.GenerateGetViewDefinitionSql("MyView", "public");

        result.Should().Contain("pg_catalog.pg_views");
        result.Should().Contain("pg_catalog.pg_matviews");
        result.Should().Contain("viewname = 'MyView'");
        result.Should().Contain("schemaname = 'public'");
        result.Should().Contain("matviewname = 'MyView'");
    }
}
