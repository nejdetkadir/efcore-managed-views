using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Diffing;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.Snapshot;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Diffing;

public sealed class ManagedViewDifferTests
{
    private static ManagedViewDefinition CreateDefinition(string name, string schema, string sql) => new()
    {
        ViewName = name,
        Schema = schema,
        Sql = sql,
        Source = "test"
    };

    private static ManagedViewDiffer CreateDiffer() => new(
        new Sha256ViewHasher(Options.Create(new ManagedViewOptions())));

    [Fact]
    public void ComputeDiff_NoPreviousSnapshot_AllAdded()
    {
        var definitions = new IManagedViewDefinition[]
        {
            CreateDefinition("vw_products", "public", "SELECT * FROM products"),
            CreateDefinition("vw_categories", "public", "SELECT * FROM categories")
        };
        var differ = CreateDiffer();

        IReadOnlyList<ManagedViewDiff> result = differ.ComputeDiff(definitions, null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.DiffType == ManagedViewDiffType.Added);
    }

    [Fact]
    public void ComputeDiff_NoChanges_ReturnsEmpty()
    {
        var definition = CreateDefinition("vw_products", "public", "SELECT * FROM products");
        var hasher = new Sha256ViewHasher(Options.Create(new ManagedViewOptions()));
        string hash = hasher.ComputeHash(definition.Sql);
        var snapshot = new ManagedViewSnapshot
        {
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_products",
                    Schema = "public",
                    Type = "View",
                    Hash = hash,
                    Sql = definition.Sql
                }
            ]
        };
        var differ = new ManagedViewDiffer(hasher);

        IReadOnlyList<ManagedViewDiff> result = differ.ComputeDiff([definition], snapshot);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_SqlChanged_ReturnsModified()
    {
        var definition = CreateDefinition("vw_products", "public", "SELECT id, name FROM products");
        var hasher = new Sha256ViewHasher(Options.Create(new ManagedViewOptions()));
        var snapshot = new ManagedViewSnapshot
        {
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_products",
                    Schema = "public",
                    Type = "View",
                    Hash = hasher.ComputeHash("SELECT * FROM products"),
                    Sql = "SELECT * FROM products"
                }
            ]
        };
        var differ = new ManagedViewDiffer(hasher);

        IReadOnlyList<ManagedViewDiff> result = differ.ComputeDiff([definition], snapshot);

        result.Should().HaveCount(1);
        result[0].DiffType.Should().Be(ManagedViewDiffType.Modified);
        result[0].PreviousHash.Should().NotBe(result[0].CurrentHash);
    }

    [Fact]
    public void ComputeDiff_ViewRemoved_ReturnsRemoved()
    {
        var snapshot = new ManagedViewSnapshot
        {
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_removed",
                    Schema = "public",
                    Type = "View",
                    Hash = "abc123",
                    Sql = "SELECT * FROM removed"
                }
            ]
        };
        var differ = CreateDiffer();

        IReadOnlyList<ManagedViewDiff> result = differ.ComputeDiff([], snapshot);

        result.Should().HaveCount(1);
        result[0].DiffType.Should().Be(ManagedViewDiffType.Removed);
        result[0].Definition.ViewName.Should().Be("vw_removed");
    }

    [Fact]
    public void ComputeDiff_MixedChanges_ReturnsCorrectDiffs()
    {
        var hasher = new Sha256ViewHasher(Options.Create(new ManagedViewOptions()));
        var unchanged = CreateDefinition("vw_unchanged", "public", "SELECT * FROM unchanged");
        var modified = CreateDefinition("vw_modified", "public", "SELECT id FROM modified");
        var added = CreateDefinition("vw_added", "public", "SELECT * FROM added");

        var snapshot = new ManagedViewSnapshot
        {
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_unchanged",
                    Schema = "public",
                    Type = "View",
                    Hash = hasher.ComputeHash(unchanged.Sql),
                    Sql = unchanged.Sql
                },
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_modified",
                    Schema = "public",
                    Type = "View",
                    Hash = hasher.ComputeHash("SELECT * FROM modified"),
                    Sql = "SELECT * FROM modified"
                },
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_removed",
                    Schema = "public",
                    Type = "View",
                    Hash = "oldhash",
                    Sql = "SELECT * FROM removed"
                }
            ]
        };
        var differ = new ManagedViewDiffer(hasher);
        var current = new IManagedViewDefinition[] { unchanged, modified, added };

        IReadOnlyList<ManagedViewDiff> result = differ.ComputeDiff(current, snapshot);

        result.Should().HaveCount(3);
        result.Should().ContainSingle(d => d.DiffType == ManagedViewDiffType.Added && d.Definition.ViewName == "vw_added");
        result.Should().ContainSingle(d => d.DiffType == ManagedViewDiffType.Modified && d.Definition.ViewName == "vw_modified");
        result.Should().ContainSingle(d => d.DiffType == ManagedViewDiffType.Removed && d.Definition.ViewName == "vw_removed");
    }
}
