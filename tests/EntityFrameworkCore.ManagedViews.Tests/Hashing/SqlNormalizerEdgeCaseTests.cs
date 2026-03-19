using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Hashing;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Hashing;

public sealed class SqlNormalizerEdgeCaseTests
{
    private readonly SqlNormalizer _sut = new();

    [Fact]
    public void Normalize_EmptyString_ReturnsEmpty()
    {
        var options = new ManagedViewOptions();
        _sut.Normalize("", options).Should().BeEmpty();
    }

    [Fact]
    public void Normalize_WhitespaceOnly_ReturnsEmpty()
    {
        var options = new ManagedViewOptions();
        _sut.Normalize("   \t  \n  ", options).Should().BeEmpty();
    }

    [Fact]
    public void Normalize_StripsMultiLineComments()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = true };
        var result = _sut.Normalize("/* comment */SELECT 1", options);
        result.Should().Be("SELECT 1");
    }

    [Fact]
    public void Normalize_NestedMultiLineComments()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = true };
        var result = _sut.Normalize("/* outer /* inner */ */SELECT 1", options);
        result.Should().Contain("SELECT 1");
    }

    [Fact]
    public void Normalize_MultipleTrailingSemicolons_RemovesAll()
    {
        var options = new ManagedViewOptions();
        var result = _sut.Normalize("SELECT 1;;", options);
        result.Should().Be("SELECT 1");
    }

    [Fact]
    public void Normalize_TabsAreCollapsed()
    {
        var options = new ManagedViewOptions();
        var result = _sut.Normalize("SELECT\t\tid\t\tFROM\tproducts", options);
        result.Should().Be("SELECT id FROM products");
    }

    [Fact]
    public void Normalize_MixedLineEndingsBecomeLF()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = false };
        var result = _sut.Normalize("SELECT\r\n1\rFROM\nproducts", options);
        result.Should().NotContain("\r");
    }

    [Fact]
    public void Normalize_MetadataDirectivesAlwaysStripped()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = false };
        var sql = "-- @viewName: test\n-- @schema: public\nSELECT 1";
        var result = _sut.Normalize(sql, options);
        result.Should().NotContain("@viewName");
        result.Should().NotContain("@schema");
        result.Should().Contain("SELECT 1");
    }

    [Fact]
    public void Normalize_PreservesStringLiterals()
    {
        var options = new ManagedViewOptions { StripCommentsBeforeHashing = false };
        var result = _sut.Normalize("SELECT 'hello world' FROM t", options);
        result.Should().Contain("'hello world'");
    }

    [Fact]
    public void Normalize_LeadingAndTrailingWhitespace_Trimmed()
    {
        var options = new ManagedViewOptions();
        var result = _sut.Normalize("   SELECT 1   ", options);
        result.Should().Be("SELECT 1");
    }
}
