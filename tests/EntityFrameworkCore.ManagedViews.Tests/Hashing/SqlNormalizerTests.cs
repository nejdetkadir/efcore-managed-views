using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Hashing;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Hashing;

public sealed class SqlNormalizerTests
{
    private readonly SqlNormalizer _sut = new();

    [Fact]
    public void Normalize_CollapseWhitespace_ProducesSameResult()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = false };
        string sql1 = "SELECT   id,   name   FROM   products";
        string sql2 = "SELECT id, name FROM products";

        string result1 = _sut.Normalize(sql1, options);
        string result2 = _sut.Normalize(sql2, options);

        result1.Should().Be(result2);
    }

    [Fact]
    public void Normalize_StripsMetadataDirectives()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = false };
        const string sql = """
            -- @viewName: vw_products
            -- @schema: catalog
            SELECT * FROM products
            """;

        string result = _sut.Normalize(sql, options);

        result.Should().NotContain("@viewName");
        result.Should().NotContain("@schema");
        result.Should().Contain("SELECT * FROM products");
    }

    [Fact]
    public void Normalize_StripsComments_WhenConfigured()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = true };
        const string sql = """
            -- This is a comment
            SELECT * FROM products
            """;

        string result = _sut.Normalize(sql, options);

        result.Should().NotContain("--");
        result.Should().Contain("SELECT * FROM products");
    }

    [Fact]
    public void Normalize_PreservesComments_WhenNotConfigured()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = false };
        const string sql = """
            -- This is a comment
            SELECT * FROM products
            """;

        string result = _sut.Normalize(sql, options);

        result.Should().Contain("-- This is a comment");
        result.Should().Contain("SELECT * FROM products");
    }

    [Fact]
    public void Normalize_RemovesTrailingSemicolon()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = false };
        const string sql = "SELECT * FROM products;";

        string result = _sut.Normalize(sql, options);

        result.Should().Be("SELECT * FROM products");
    }

    [Fact]
    public void Normalize_DifferentLineEndings_ProduceSameResult()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = false };
        string sqlLf = "SELECT\nid\nFROM\nproducts";
        string sqlCrLf = "SELECT\r\nid\r\nFROM\r\nproducts";
        string sqlCr = "SELECT\rid\rFROM\rproducts";

        string resultLf = _sut.Normalize(sqlLf, options);
        string resultCrLf = _sut.Normalize(sqlCrLf, options);
        string resultCr = _sut.Normalize(sqlCr, options);

        resultLf.Should().Be(resultCrLf);
        resultCrLf.Should().Be(resultCr);
    }
}
