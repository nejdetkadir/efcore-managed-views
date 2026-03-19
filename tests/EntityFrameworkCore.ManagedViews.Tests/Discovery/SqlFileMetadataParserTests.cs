using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Exceptions;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Discovery;

public sealed class SqlFileMetadataParserTests
{
    [Fact]
    public void Parse_WithFullMetadata_ReturnsCorrectDefinition()
    {
        const string content = """
            -- @viewName: vw_active_products
            -- @schema: catalog
            -- @type: materialized
            -- @dependsOn: vw_base_products, vw_categories
            SELECT * FROM products WHERE active = true
            """;

        ManagedViewDefinition result = SqlFileMetadataParser.Parse(content, "vw_active_products.sql", "public", "test.sql");

        result.ViewName.Should().Be("vw_active_products");
        result.Schema.Should().Be("catalog");
        result.ViewType.Should().Be(ManagedViewType.Materialized);
        result.Sql.Should().Be("SELECT * FROM products WHERE active = true");
        result.DependsOn.Should().BeEquivalentTo(["vw_base_products", "vw_categories"]);
        result.Source.Should().Be("test.sql");
    }

    [Fact]
    public void Parse_WithNoMetadata_InfersFromFilename()
    {
        const string content = "SELECT * FROM products";
        const string fileName = "catalog.vw_products.sql";

        ManagedViewDefinition result = SqlFileMetadataParser.Parse(content, fileName, "public", "test.sql");

        result.ViewName.Should().Be("vw_products");
        result.Schema.Should().Be("catalog");
        result.Sql.Should().Be("SELECT * FROM products");
    }

    [Fact]
    public void Parse_MaterializedViewType_SetsCorrectType()
    {
        const string content = """
            -- @type: materialized
            SELECT 1
            """;

        ManagedViewDefinition result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "test.sql");

        result.ViewType.Should().Be(ManagedViewType.Materialized);
    }

    [Fact]
    public void Parse_WithIndexes_ParsesCorrectly()
    {
        const string content = """
            -- @viewName: vw_products
            -- @type: materialized
            -- @indexes: idx_cat(category_id); idx_date(created_at DESC)
            SELECT * FROM products
            """;

        ManagedViewDefinition result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "test.sql");

        result.Indexes.Should().HaveCount(2);
        result.Indexes[0].Name.Should().Be("idx_cat");
        result.Indexes[0].Columns.Should().Be("category_id");
        result.Indexes[1].Name.Should().Be("idx_date");
        result.Indexes[1].Columns.Should().Be("created_at DESC");
    }

    [Fact]
    public void Parse_EmptySql_ThrowsManagedViewSqlParseException()
    {
        const string content = """
            -- @viewName: vw_empty
            -- @schema: public

            """;

        Action act = () => SqlFileMetadataParser.Parse(content, "test.sql", "public", "test.sql");

        act.Should().Throw<ManagedViewSqlParseException>()
            .WithMessage("*No SQL body found*");
    }

    [Fact]
    public void Parse_UnknownViewType_ThrowsManagedViewSqlParseException()
    {
        const string content = """
            -- @type: invalid_type
            SELECT 1
            """;

        Action act = () => SqlFileMetadataParser.Parse(content, "test.sql", "public", "test.sql");

        act.Should().Throw<ManagedViewSqlParseException>()
            .WithMessage("*Unknown view type*invalid_type*");
    }

    [Fact]
    public void Parse_MultipleDependencies_SplitsCorrectly()
    {
        const string content = """
            -- @viewName: vw_agg
            -- @dependsOn: vw_a, vw_b, vw_c
            SELECT 1
            """;

        ManagedViewDefinition result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "test.sql");

        result.DependsOn.Should().BeEquivalentTo(["vw_a", "vw_b", "vw_c"]);
    }

    [Fact]
    public void Parse_StripsTrailingSemicolon()
    {
        const string content = "SELECT * FROM products;";

        ManagedViewDefinition result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "test.sql");

        result.Sql.Should().Be("SELECT * FROM products");
    }
}
