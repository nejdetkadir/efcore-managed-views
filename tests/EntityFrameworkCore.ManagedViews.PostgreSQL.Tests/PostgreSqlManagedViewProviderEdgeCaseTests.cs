using EntityFrameworkCore.ManagedViews.Exceptions;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.PostgreSQL.Tests;

public sealed class PostgreSqlManagedViewProviderEdgeCaseTests
{
    private readonly PostgreSqlManagedViewProvider _sut = new();

    [Fact]
    public void GenerateCreateSql_NullDefinition_ThrowsArgumentNullException()
    {
        var act = () => _sut.GenerateCreateSql(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateDropSql_NullDefinition_ThrowsArgumentNullException()
    {
        var act = () => _sut.GenerateDropSql(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateCreateIndexSql_NullDefinition_ThrowsArgumentNullException()
    {
        var act = () => _sut.GenerateCreateIndexSql(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateDropIndexSql_NullDefinition_ThrowsArgumentNullException()
    {
        var act = () => _sut.GenerateDropIndexSql(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateCreateIndexSql_MaterializedViewNoIndexes_ReturnsEmpty()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "mv_no_idx", Schema = "public",
            ViewType = ManagedViewType.Materialized, Sql = "SELECT 1",
            Source = "test", Indexes = []
        };

        _sut.GenerateCreateIndexSql(def).Should().BeEmpty();
    }

    [Fact]
    public void GenerateDropIndexSql_NoIndexes_ReturnsEmpty()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "vw_test", Schema = "public",
            ViewType = ManagedViewType.View, Sql = "SELECT 1",
            Source = "test", Indexes = []
        };

        _sut.GenerateDropIndexSql(def).Should().BeEmpty();
    }

    [Fact]
    public void GenerateCreateSql_QuotesSchemaAndName()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "my_view", Schema = "my_schema",
            ViewType = ManagedViewType.View, Sql = "SELECT 1",
            Source = "test"
        };

        var result = _sut.GenerateCreateSql(def);
        result.Should().Contain("\"my_schema\".\"my_view\"");
    }

    [Fact]
    public void GenerateDropSql_QuotesSchemaAndName()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "my_view", Schema = "my_schema",
            ViewType = ManagedViewType.View, Sql = "SELECT 1",
            Source = "test"
        };

        var result = _sut.GenerateDropSql(def);
        result.Should().Contain("\"my_schema\".\"my_view\"");
    }

    [Fact]
    public void GenerateRefreshSql_QuotesSchemaAndName()
    {
        var result = _sut.GenerateRefreshSql("my_mv", "my_schema", false);
        result.Should().Be("REFRESH MATERIALIZED VIEW \"my_schema\".\"my_mv\"");
    }

    [Fact]
    public void GenerateGetViewDefinitionSql_IncludesBothViewAndMatView()
    {
        var result = _sut.GenerateGetViewDefinitionSql("vw_test", "public");
        result.Should().Contain("pg_views");
        result.Should().Contain("pg_matviews");
        result.Should().Contain("UNION ALL");
    }

    [Fact]
    public void GenerateCreateIndexSql_MultipleIndexes_ReturnsAll()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "mv_test", Schema = "public",
            ViewType = ManagedViewType.Materialized, Sql = "SELECT 1",
            Source = "test",
            Indexes =
            [
                new ManagedViewIndex("idx_a", "col_a"),
                new ManagedViewIndex("idx_b", "col_b DESC"),
                new ManagedViewIndex("idx_c", "col_c, col_d")
            ]
        };

        var result = _sut.GenerateCreateIndexSql(def);

        result.Should().HaveCount(3);
        result[0].Should().Contain("\"idx_a\"");
        result[1].Should().Contain("col_b DESC");
        result[2].Should().Contain("col_c, col_d");
    }

    [Fact]
    public void GenerateDropIndexSql_UsesSchemaQualifiedName()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "mv_test", Schema = "analytics",
            ViewType = ManagedViewType.Materialized, Sql = "SELECT 1",
            Source = "test",
            Indexes = [new ManagedViewIndex("idx_x", "col_x")]
        };

        var result = _sut.GenerateDropIndexSql(def);

        result.Should().ContainSingle();
        result[0].Should().Contain("\"analytics\".\"idx_x\"");
    }

    [Fact]
    public void SupportsCreateOrReplace_UnsupportedType_ReturnsFalse()
    {
        _sut.SupportsCreateOrReplace((ManagedViewType)99).Should().BeFalse();
    }

    [Fact]
    public void GenerateCreateSql_UnsupportedViewType_ThrowsProviderException()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "vw_test", Schema = "public",
            ViewType = (ManagedViewType)99, Sql = "SELECT 1",
            Source = "test"
        };

        var act = () => _sut.GenerateCreateSql(def);

        act.Should().Throw<EntityFrameworkCore.ManagedViews.Exceptions.ManagedViewProviderException>()
            .WithMessage("*Unsupported view type*");
    }

    [Fact]
    public void GenerateDropSql_UnsupportedViewType_ThrowsProviderException()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "vw_test", Schema = "public",
            ViewType = (ManagedViewType)99, Sql = "SELECT 1",
            Source = "test"
        };

        var act = () => _sut.GenerateDropSql(def);

        act.Should().Throw<EntityFrameworkCore.ManagedViews.Exceptions.ManagedViewProviderException>()
            .WithMessage("*Unsupported view type*");
    }

    [Fact]
    public void GenerateCreateIndexSql_NonMaterialized_WithIndexes_ReturnsEmpty()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "vw_test", Schema = "public",
            ViewType = ManagedViewType.View, Sql = "SELECT 1",
            Source = "test",
            Indexes = [new ManagedViewIndex("idx_1", "col1")]
        };

        _sut.GenerateCreateIndexSql(def).Should().BeEmpty();
    }

    [Fact]
    public void GenerateDropIndexSql_MaterializedWithIndexes_ReturnsStatements()
    {
        var def = new ManagedViewDefinition
        {
            ViewName = "mv_test", Schema = "public",
            ViewType = ManagedViewType.Materialized, Sql = "SELECT 1",
            Source = "test",
            Indexes = [new ManagedViewIndex("idx_1", "col1"), new ManagedViewIndex("idx_2", "col2")]
        };

        var result = _sut.GenerateDropIndexSql(def);
        result.Should().HaveCount(2);
    }
}
