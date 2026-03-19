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

public sealed class ManagedViewDifferEdgeCaseTests
{
    private static ManagedViewDiffer CreateDiffer() => new(
        new Sha256ViewHasher(Options.Create(new ManagedViewOptions())));

    [Fact]
    public void ComputeDiff_NullDefinitions_ThrowsArgumentNullException()
    {
        var differ = CreateDiffer();
        var act = () => differ.ComputeDiff(null!, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeDiff_EmptyDefinitionsEmptySnapshot_ReturnsEmpty()
    {
        var differ = CreateDiffer();
        var snapshot = new ManagedViewSnapshot { Views = [] };

        var result = differ.ComputeDiff([], snapshot);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_EmptyDefinitionsNullSnapshot_ReturnsEmpty()
    {
        var differ = CreateDiffer();

        var result = differ.ComputeDiff([], null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_WhitespaceOnlyChange_WithNormalization_NoModified()
    {
        var hasher = new Sha256ViewHasher(Options.Create(new ManagedViewOptions()));
        string originalSql = "SELECT id FROM products";
        string hash = hasher.ComputeHash(originalSql);

        var def = new ManagedViewDefinition
        {
            ViewName = "vw_test", Schema = "public",
            Sql = "SELECT   id   FROM   products",
            Source = "test"
        };
        var snapshot = new ManagedViewSnapshot
        {
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_test", Schema = "public",
                    Type = "View", Hash = hash, Sql = originalSql
                }
            ]
        };

        var differ = new ManagedViewDiffer(hasher);
        var result = differ.ComputeDiff([def], snapshot);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_AddedViewHasCorrectHash()
    {
        var hasher = new Sha256ViewHasher(Options.Create(new ManagedViewOptions()));
        var def = new ManagedViewDefinition
        {
            ViewName = "vw_new", Schema = "public",
            Sql = "SELECT 1", Source = "test"
        };

        var differ = new ManagedViewDiffer(hasher);
        var result = differ.ComputeDiff([def], null);

        result.Should().ContainSingle();
        result[0].DiffType.Should().Be(ManagedViewDiffType.Added);
        result[0].CurrentHash.Should().NotBeNullOrEmpty();
        result[0].PreviousHash.Should().BeNull();
    }

    [Fact]
    public void ComputeDiff_RemovedViewHasCorrectProperties()
    {
        var snapshot = new ManagedViewSnapshot
        {
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_old", Schema = "public",
                    Type = "View", Hash = "oldhash", Sql = "SELECT 1"
                }
            ]
        };

        var differ = CreateDiffer();
        var result = differ.ComputeDiff([], snapshot);

        result.Should().ContainSingle();
        result[0].DiffType.Should().Be(ManagedViewDiffType.Removed);
        result[0].PreviousHash.Should().Be("oldhash");
        result[0].Definition.ViewName.Should().Be("vw_old");
    }

    [Fact]
    public void ComputeDiff_DifferentSchemas_TreatedAsDifferentViews()
    {
        var hasher = new Sha256ViewHasher(Options.Create(new ManagedViewOptions()));
        var def1 = new ManagedViewDefinition
        {
            ViewName = "vw_test", Schema = "public",
            Sql = "SELECT 1", Source = "test"
        };
        var def2 = new ManagedViewDefinition
        {
            ViewName = "vw_test", Schema = "catalog",
            Sql = "SELECT 2", Source = "test"
        };

        var differ = new ManagedViewDiffer(hasher);
        var result = differ.ComputeDiff([def1, def2], null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.DiffType == ManagedViewDiffType.Added);
    }
}
