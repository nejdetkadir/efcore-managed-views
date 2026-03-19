using EntityFrameworkCore.ManagedViews.Snapshot;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Snapshot;

public sealed class JsonSnapshotStoreTests
{
    private readonly JsonSnapshotStore _sut = new();

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        const string contextName = "TestDbContext";
        var snapshot = new ManagedViewSnapshot
        {
            Version = "1.0.0",
            GeneratedAt = DateTimeOffset.UtcNow,
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_products",
                    Schema = "public",
                    Type = "View",
                    Hash = "abc123",
                    Sql = "SELECT * FROM products"
                }
            ]
        };

        try
        {
            _sut.Save(snapshot, dir, contextName);
            ManagedViewSnapshot? loaded = _sut.Load(dir, contextName);

            loaded.Should().NotBeNull();
            loaded!.Views.Should().HaveCount(1);
            loaded.Views[0].ViewName.Should().Be("vw_products");
            loaded.Views[0].Schema.Should().Be("public");
            loaded.Views[0].Hash.Should().Be("abc123");
            loaded.Views[0].Sql.Should().Be("SELECT * FROM products");
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void Load_NoFile_ReturnsNull()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        const string contextName = "NonExistentContext";

        ManagedViewSnapshot? result = _sut.Load(dir, contextName);

        result.Should().BeNull();
    }
}
