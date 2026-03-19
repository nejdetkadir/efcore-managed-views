using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Exceptions;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Discovery;

public sealed class SqlFileMetadataParserEdgeCaseTests
{
    [Fact]
    public void Parse_ViewType_CaseInsensitive()
    {
        const string content = "-- @type: MATERIALIZED\nSELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.ViewType.Should().Be(ManagedViewType.Materialized);
    }

    [Fact]
    public void Parse_ViewTypeLowerCase_Works()
    {
        const string content = "-- @type: view\nSELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.ViewType.Should().Be(ManagedViewType.View);
    }

    [Fact]
    public void Parse_FilenameWithoutSchemaPrefix_UsesDefaultSchema()
    {
        const string content = "SELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "vw_products.sql", "default_schema", "src");
        result.ViewName.Should().Be("vw_products");
        result.Schema.Should().Be("default_schema");
    }

    [Fact]
    public void Parse_FilenameWithDot_InfersSchemaAndName()
    {
        const string content = "SELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "analytics.mv_stats.sql", "public", "src");
        result.ViewName.Should().Be("mv_stats");
        result.Schema.Should().Be("analytics");
    }

    [Fact]
    public void Parse_DirectiveOverridesFilename()
    {
        const string content = "-- @viewName: overridden_name\n-- @schema: overridden_schema\nSELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "original.vw_original.sql", "public", "src");
        result.ViewName.Should().Be("overridden_name");
        result.Schema.Should().Be("overridden_schema");
    }

    [Fact]
    public void Parse_OnlyComments_KeepsCommentsAsSqlBody()
    {
        const string content = "-- just a comment\n-- another comment";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.Sql.Should().Contain("-- just a comment");
    }

    [Fact]
    public void Parse_OnlyMetadataDirectives_ThrowsManagedViewSqlParseException()
    {
        const string content = "-- @viewName: vw_test\n-- @schema: public";
        var act = () => SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        act.Should().Throw<ManagedViewSqlParseException>()
            .WithMessage("*No SQL body*");
    }

    [Fact]
    public void Parse_SingleIndex_ParsedCorrectly()
    {
        const string content = "-- @indexes: idx_single(col1)\nSELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.Indexes.Should().ContainSingle();
        result.Indexes[0].Name.Should().Be("idx_single");
        result.Indexes[0].Columns.Should().Be("col1");
    }

    [Fact]
    public void Parse_IndexWithMultipleColumns()
    {
        const string content = "-- @indexes: idx_composite(col1, col2 DESC)\nSELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.Indexes.Should().ContainSingle();
        result.Indexes[0].Columns.Should().Be("col1, col2 DESC");
    }

    [Fact]
    public void Parse_MultipleIndexes_SemicolonSeparated()
    {
        const string content = "-- @indexes: idx_a(col_a); idx_b(col_b); idx_c(col_c)\nSELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.Indexes.Should().HaveCount(3);
    }

    [Fact]
    public void Parse_NoDependencies_ReturnsEmptyList()
    {
        const string content = "SELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.DependsOn.Should().BeEmpty();
    }

    [Fact]
    public void Parse_SingleDependency_ReturnsSingle()
    {
        const string content = "-- @dependsOn: vw_base\nSELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.DependsOn.Should().ContainSingle("vw_base");
    }

    [Fact]
    public void Parse_SqlWithLeadingWhitespace_Trims()
    {
        const string content = "\n\n   SELECT 1   \n\n";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.Sql.Should().Be("SELECT 1");
    }

    [Fact]
    public void Parse_DefaultViewType_IsView()
    {
        const string content = "SELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.ViewType.Should().Be(ManagedViewType.View);
    }

    [Fact]
    public void Parse_SetsSource()
    {
        const string content = "SELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "my/resource/path.sql");
        result.Source.Should().Be("my/resource/path.sql");
    }

    [Fact]
    public void Parse_InvalidIndexFormat_NoParentheses_ThrowsSqlParseException()
    {
        const string content = "-- @indexes: bad_index_no_parens\nSELECT 1";
        var act = () => SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        act.Should().Throw<ManagedViewSqlParseException>()
            .WithMessage("*Invalid index definition*");
    }

    [Fact]
    public void Parse_InvalidIndexFormat_EmptyParentheses_ThrowsSqlParseException()
    {
        const string content = "-- @indexes: bad()\nSELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        result.Indexes.Should().ContainSingle();
    }

    [Fact]
    public void Parse_InvalidIndexFormat_ReversedParentheses_ThrowsSqlParseException()
    {
        const string content = "-- @indexes: bad)col(\nSELECT 1";
        var act = () => SqlFileMetadataParser.Parse(content, "test.sql", "public", "src");
        act.Should().Throw<ManagedViewSqlParseException>()
            .WithMessage("*Invalid index definition*");
    }

    [Fact]
    public void Parse_SimpleFileName_NoSchemaInferred()
    {
        const string content = "SELECT 1";
        var result = SqlFileMetadataParser.Parse(content, "simple.sql", "default_sch", "src");
        result.ViewName.Should().Be("simple");
        result.Schema.Should().Be("default_sch");
    }

    [Fact]
    public void Parse_NullContent_ThrowsArgumentException()
    {
        var act = () => SqlFileMetadataParser.Parse(null!, "test.sql", "public", "src");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WhitespaceContent_ThrowsArgumentException()
    {
        var act = () => SqlFileMetadataParser.Parse("   ", "test.sql", "public", "src");
        act.Should().Throw<ArgumentException>();
    }
}
