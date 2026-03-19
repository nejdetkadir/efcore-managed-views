using EntityFrameworkCore.ManagedViews.Snapshot;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Snapshot;

public sealed class JsonSnapshotStoreEdgeCaseTests
{
    private readonly JsonSnapshotStore _sut = new();

    [Fact]
    public void Load_NonexistentDirectory_ReturnsNull()
    {
        var result = _sut.Load("/nonexistent/path/xyz", "TestContext");
        result.Should().BeNull();
    }

    [Fact]
    public void Save_WithIndexes_RoundTrips()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var snapshot = new ManagedViewSnapshot
            {
                Views =
                [
                    new ManagedViewSnapshotEntry
                    {
                        ViewName = "mv_test", Schema = "public",
                        Type = "Materialized", Hash = "hash123",
                        Sql = "SELECT 1",
                        Indexes =
                        [
                            new ManagedViewSnapshotIndexEntry
                            {
                                Name = "idx_test",
                                Columns = "col1"
                            }
                        ]
                    }
                ]
            };

            _sut.Save(snapshot, dir, "TestCtx");
            var loaded = _sut.Load(dir, "TestCtx");

            loaded.Should().NotBeNull();
            loaded!.Views.Should().ContainSingle();
            loaded.Views[0].Indexes.Should().ContainSingle();
            loaded.Views[0].Indexes[0].Name.Should().Be("idx_test");
            loaded.Views[0].Indexes[0].Columns.Should().Be("col1");
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
    public void Save_MultipleViews_RoundTrips()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var snapshot = new ManagedViewSnapshot
            {
                Views =
                [
                    new ManagedViewSnapshotEntry
                    {
                        ViewName = "vw_a", Schema = "public",
                        Type = "View", Hash = "h1", Sql = "SELECT 1"
                    },
                    new ManagedViewSnapshotEntry
                    {
                        ViewName = "vw_b", Schema = "catalog",
                        Type = "View", Hash = "h2", Sql = "SELECT 2"
                    },
                    new ManagedViewSnapshotEntry
                    {
                        ViewName = "mv_c", Schema = "public",
                        Type = "Materialized", Hash = "h3", Sql = "SELECT 3"
                    }
                ]
            };

            _sut.Save(snapshot, dir, "MultiCtx");
            var loaded = _sut.Load(dir, "MultiCtx");

            loaded.Should().NotBeNull();
            loaded!.Views.Should().HaveCount(3);
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
    public void Save_OverwritesExistingSnapshot()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var snapshot1 = new ManagedViewSnapshot
            {
                Views =
                [
                    new ManagedViewSnapshotEntry
                    {
                        ViewName = "vw_old", Schema = "public",
                        Type = "View", Hash = "old", Sql = "SELECT 1"
                    }
                ]
            };
            var snapshot2 = new ManagedViewSnapshot
            {
                Views =
                [
                    new ManagedViewSnapshotEntry
                    {
                        ViewName = "vw_new", Schema = "public",
                        Type = "View", Hash = "new", Sql = "SELECT 2"
                    }
                ]
            };

            _sut.Save(snapshot1, dir, "Ctx");
            _sut.Save(snapshot2, dir, "Ctx");

            var loaded = _sut.Load(dir, "Ctx");
            loaded!.Views.Should().ContainSingle();
            loaded.Views[0].ViewName.Should().Be("vw_new");
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
    public void Save_PreservesVersion()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var snapshot = new ManagedViewSnapshot { Version = "2.0.0", Views = [] };

            _sut.Save(snapshot, dir, "Ctx");
            var loaded = _sut.Load(dir, "Ctx");

            loaded!.Version.Should().Be("2.0.0");
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
    public void Save_ExistingDirectory_WritesFile()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var snapshot = new ManagedViewSnapshot { Views = [] };
            _sut.Save(snapshot, dir, "Ctx");

            var loaded = _sut.Load(dir, "Ctx");
            loaded.Should().NotBeNull();
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
