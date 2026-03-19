using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Hashing;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Hashing;

public sealed class Sha256ViewHasherEdgeCaseTests
{
    [Fact]
    public void ComputeHash_WithNormalizationDisabled_WhitespaceDifferencesDetected()
    {
        var options = Options.Create(new ManagedViewOptions { NormalizeSqlBeforeHashing = false });
        var hasher = new Sha256ViewHasher(options);

        string hash1 = hasher.ComputeHash("SELECT  id  FROM  products");
        string hash2 = hasher.ComputeHash("SELECT id FROM products");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_CommentOnlyDifference_WithStripping_SameHash()
    {
        var options = Options.Create(new ManagedViewOptions
        {
            NormalizeSqlBeforeHashing = true,
            StripCommentsBeforeHashing = true
        });
        var hasher = new Sha256ViewHasher(options);

        string hash1 = hasher.ComputeHash("SELECT * FROM products");
        string hash2 = hasher.ComputeHash("-- comment\nSELECT * FROM products");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_CommentOnlyDifference_WithoutStripping_DifferentHash()
    {
        var options = Options.Create(new ManagedViewOptions
        {
            NormalizeSqlBeforeHashing = true,
            StripCommentsBeforeHashing = false
        });
        var hasher = new Sha256ViewHasher(options);

        string hash1 = hasher.ComputeHash("SELECT * FROM products");
        string hash2 = hasher.ComputeHash("-- comment\nSELECT * FROM products");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_TrailingSemicolon_WithNormalization_SameHash()
    {
        var options = Options.Create(new ManagedViewOptions { NormalizeSqlBeforeHashing = true });
        var hasher = new Sha256ViewHasher(options);

        string hash1 = hasher.ComputeHash("SELECT 1");
        string hash2 = hasher.ComputeHash("SELECT 1;");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_ReturnsLowercaseHexString()
    {
        var options = Options.Create(new ManagedViewOptions());
        var hasher = new Sha256ViewHasher(options);

        string hash = hasher.ComputeHash("SELECT 1");

        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public void ComputeHash_EmptyString_ReturnsHash()
    {
        var options = Options.Create(new ManagedViewOptions());
        var hasher = new Sha256ViewHasher(options);

        string hash = hasher.ComputeHash("");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new Sha256ViewHasher(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeHash_MultiLineComment_WithStripping_Removed()
    {
        var options = Options.Create(new ManagedViewOptions
        {
            NormalizeSqlBeforeHashing = true,
            StripCommentsBeforeHashing = true
        });
        var hasher = new Sha256ViewHasher(options);

        string hash1 = hasher.ComputeHash("SELECT * FROM products");
        string hash2 = hasher.ComputeHash("/* multi\nline\ncomment */\nSELECT * FROM products");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_LineEndingDifferences_WithNormalization_SameHash()
    {
        var options = Options.Create(new ManagedViewOptions { NormalizeSqlBeforeHashing = true });
        var hasher = new Sha256ViewHasher(options);

        string hashLf = hasher.ComputeHash("SELECT\nid\nFROM\nproducts");
        string hashCrlf = hasher.ComputeHash("SELECT\r\nid\r\nFROM\r\nproducts");

        hashLf.Should().Be(hashCrlf);
    }
}
