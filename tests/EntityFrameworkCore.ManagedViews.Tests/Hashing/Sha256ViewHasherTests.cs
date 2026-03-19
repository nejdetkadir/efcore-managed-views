using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Hashing;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Hashing;

public sealed class Sha256ViewHasherTests
{
    [Fact]
    public void ComputeHash_SameSql_ReturnsSameHash()
    {
        var options = Options.Create(new ManagedViewOptions { NormalizeSqlBeforeHashing = true });
        var hasher = new Sha256ViewHasher(options);
        const string sql = "SELECT * FROM products";

        string hash1 = hasher.ComputeHash(sql);
        string hash2 = hasher.ComputeHash(sql);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_DifferentSql_ReturnsDifferentHash()
    {
        var options = Options.Create(new ManagedViewOptions { NormalizeSqlBeforeHashing = true });
        var hasher = new Sha256ViewHasher(options);

        string hash1 = hasher.ComputeHash("SELECT * FROM products");
        string hash2 = hasher.ComputeHash("SELECT * FROM categories");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_WhitespaceOnlyDifference_ReturnsSameHash()
    {
        var options = Options.Create(new ManagedViewOptions { NormalizeSqlBeforeHashing = true });
        var hasher = new Sha256ViewHasher(options);

        string hash1 = hasher.ComputeHash("SELECT id, name FROM products");
        string hash2 = hasher.ComputeHash("SELECT   id,   name   FROM   products");

        hash1.Should().Be(hash2);
    }
}
