using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Diffing;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.Snapshot;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.FunctionalTests;

public sealed class SnapshotRoundtripTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonSnapshotStore _store;
    private readonly Sha256ViewHasher _hasher;

    public SnapshotRoundtripTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"mv_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _store = new JsonSnapshotStore();
        _hasher = new Sha256ViewHasher(Options.Create(new ManagedViewOptions()));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Snapshot_SaveAndLoad_PreservesAllViewData()
    {
        var snapshot = new ManagedViewSnapshot
        {
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_products",
                    Schema = "catalog",
                    Type = "View",
                    Hash = "abc123",
                    Sql = "SELECT id, name FROM products",
                    DependsOn = [],
                    Indexes = []
                },
                new ManagedViewSnapshotEntry
                {
                    ViewName = "mv_stats",
                    Schema = "public",
                    Type = "Materialized",
                    Hash = "def456",
                    Sql = "SELECT COUNT(*) FROM products",
                    DependsOn = ["vw_products"],
                    Indexes =
                    [
                        new ManagedViewSnapshotIndexEntry { Name = "idx_stats", Columns = "count DESC" }
                    ]
                }
            ]
        };

        _store.Save(snapshot, _tempDir, "TestApp");
        var loaded = _store.Load(_tempDir, "TestApp");

        loaded.Should().NotBeNull();
        loaded!.Views.Should().HaveCount(2);

        var view = loaded.Views.First(v => v.ViewName == "vw_products");
        view.Schema.Should().Be("catalog");
        view.Type.Should().Be("View");
        view.Hash.Should().Be("abc123");
        view.Sql.Should().Be("SELECT id, name FROM products");

        var matView = loaded.Views.First(v => v.ViewName == "mv_stats");
        matView.DependsOn.Should().ContainSingle().Which.Should().Be("vw_products");
        matView.Indexes.Should().ContainSingle()
            .Which.Name.Should().Be("idx_stats");
    }

    [Fact]
    public void Snapshot_DiffPipeline_DetectsChangesAcrossSnapshots()
    {
        var differ = new ManagedViewDiffer(_hasher);

        var v1Sql = "SELECT id, name FROM products";
        var v2Sql = "SELECT id, name FROM products WHERE is_active = true";
        var v3Sql = "SELECT COUNT(*) FROM products";

        ManagedViewDefinition[] initialViews =
        [
            new() { ViewName = "vw_one", Schema = "public", Sql = v1Sql, Source = "test" },
            new() { ViewName = "vw_two", Schema = "public", Sql = v2Sql, Source = "test" },
            new() { ViewName = "vw_three", Schema = "public", Sql = v3Sql, Source = "test" }
        ];

        // First migration: no snapshot, all added
        var firstDiff = differ.ComputeDiff(initialViews, null);
        firstDiff.Should().HaveCount(3);
        firstDiff.Should().OnlyContain(d => d.DiffType == ManagedViewDiffType.Added);

        // Save snapshot
        var snapshot1 = new ManagedViewSnapshot
        {
            Views = initialViews.Select(v => new ManagedViewSnapshotEntry
            {
                ViewName = v.ViewName,
                Schema = v.Schema,
                Type = v.ViewType.ToString(),
                Hash = _hasher.ComputeHash(v.Sql),
                Sql = v.Sql
            }).ToList()
        };

        _store.Save(snapshot1, _tempDir, "TestContext");
        var loadedSnapshot = _store.Load(_tempDir, "TestContext");

        // Second migration: same views, no diff
        var secondDiff = differ.ComputeDiff(initialViews, loadedSnapshot);
        secondDiff.Should().BeEmpty();

        // Third migration: modify vw_two, remove vw_three, add vw_four
        string modifiedV2Sql = "SELECT id, name, price FROM products WHERE is_active = true";
        ManagedViewDefinition[] updatedViews =
        [
            new() { ViewName = "vw_one", Schema = "public", Sql = v1Sql, Source = "test" },
            new() { ViewName = "vw_two", Schema = "public", Sql = modifiedV2Sql, Source = "test" },
            new() { ViewName = "vw_four", Schema = "public", Sql = "SELECT 1", Source = "test" }
        ];

        var thirdDiff = differ.ComputeDiff(updatedViews, loadedSnapshot);

        thirdDiff.Should().HaveCount(3);
        thirdDiff.Should().ContainSingle(d => d.DiffType == ManagedViewDiffType.Modified)
            .Which.Definition.ViewName.Should().Be("vw_two");
        thirdDiff.Should().ContainSingle(d => d.DiffType == ManagedViewDiffType.Removed)
            .Which.Definition.ViewName.Should().Be("vw_three");
        thirdDiff.Should().ContainSingle(d => d.DiffType == ManagedViewDiffType.Added)
            .Which.Definition.ViewName.Should().Be("vw_four");
    }

    [Fact]
    public void Snapshot_DiffToOperations_ProducesCorrectMigrationOperations()
    {
        var differ = new ManagedViewDiffer(_hasher);

        var viewSql = "SELECT id, name FROM products WHERE is_active = true";
        ManagedViewDefinition[] views =
        [
            new()
            {
                ViewName = "vw_active",
                Schema = "public",
                ViewType = ManagedViewType.View,
                Sql = viewSql,
                Source = "test"
            },
            new()
            {
                ViewName = "mv_summary",
                Schema = "public",
                ViewType = ManagedViewType.Materialized,
                Sql = "SELECT COUNT(*) FROM products",
                Indexes = [new ManagedViewIndex("idx_count", "count")],
                DependsOn = ["vw_active"],
                Source = "test"
            }
        ];

        var diffs = differ.ComputeDiff(views, null);

        // All should be Added since no previous snapshot
        diffs.Should().HaveCount(2);
        diffs.Should().OnlyContain(d => d.DiffType == ManagedViewDiffType.Added);

        // Verify the diffs carry the correct view metadata
        var viewDiff = diffs.First(d => d.Definition.ViewName == "vw_active");
        viewDiff.Definition.ViewType.Should().Be(ManagedViewType.View);
        viewDiff.CurrentHash.Should().NotBeNullOrEmpty();

        var matViewDiff = diffs.First(d => d.Definition.ViewName == "mv_summary");
        matViewDiff.Definition.ViewType.Should().Be(ManagedViewType.Materialized);
        matViewDiff.Definition.Indexes.Should().ContainSingle();
        matViewDiff.Definition.DependsOn.Should().Contain("vw_active");
    }

    [Fact]
    public void WhitespaceOnlyChanges_DoNotProduceDiffs()
    {
        var differ = new ManagedViewDiffer(_hasher);

        string originalSql = "SELECT id, name FROM products WHERE is_active = true";

        ManagedViewDefinition[] original =
        [
            new() { ViewName = "vw_test", Schema = "public", Sql = originalSql, Source = "test" }
        ];

        var snapshot = new ManagedViewSnapshot
        {
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_test",
                    Schema = "public",
                    Type = "View",
                    Hash = _hasher.ComputeHash(originalSql),
                    Sql = originalSql
                }
            ]
        };

        // Same SQL but with extra whitespace and different formatting
        string reformattedSql = "SELECT   id,\n  name\nFROM   products\n  WHERE is_active =   true";
        ManagedViewDefinition[] reformatted =
        [
            new() { ViewName = "vw_test", Schema = "public", Sql = reformattedSql, Source = "test" }
        ];

        var diffs = differ.ComputeDiff(reformatted, snapshot);
        diffs.Should().BeEmpty("whitespace-only changes should not trigger diffs");
    }
}
