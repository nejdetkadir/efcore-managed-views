using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.Diffing;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.Snapshot;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.FunctionalTests;

public sealed class CompositePipelineTests
{
    private static ManagedViewDefinition MakeDef(string name, string sql, string schema = "public",
        ManagedViewType type = ManagedViewType.View, params string[] dependsOn) => new()
    {
        ViewName = name, Schema = schema, Sql = sql, ViewType = type,
        DependsOn = dependsOn.ToList(), Source = "test"
    };

    [Fact]
    public void FullPipeline_DiscoverDiffResolveSnapshot()
    {
        var options = new ManagedViewOptions();
        var hasher = new Sha256ViewHasher(Options.Create(options));
        var differ = new ManagedViewDiffer(hasher);
        var resolver = new TopologicalSortResolver();
        var snapshotStore = new JsonSnapshotStore();

        var source = Substitute.For<IManagedViewDiscovery>();
        source.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            MakeDef("vw_base", "SELECT 1"),
            MakeDef("vw_derived", "SELECT * FROM vw_base", dependsOn: "vw_base"),
            MakeDef("mv_agg", "SELECT COUNT(*) FROM vw_base", type: ManagedViewType.Materialized, dependsOn: "vw_base")
        ]);

        var composite = new CompositeViewDiscoveryService([source]);
        var definitions = composite.DiscoverAll(options);

        definitions.Should().HaveCount(3);

        var diffs = differ.ComputeDiff(definitions, null);
        diffs.Should().HaveCount(3);
        diffs.Should().OnlyContain(d => d.DiffType == ManagedViewDiffType.Added);

        var createOrder = resolver.ResolveCreateOrder(definitions);
        createOrder[0].ViewName.Should().Be("vw_base");

        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var snapshot = new ManagedViewSnapshot
            {
                Views = definitions.Select(d => new ManagedViewSnapshotEntry
                {
                    ViewName = d.ViewName,
                    Schema = d.Schema,
                    Type = d.ViewType.ToString(),
                    Hash = hasher.ComputeHash(d.Sql),
                    Sql = d.Sql
                }).ToList()
            };

            snapshotStore.Save(snapshot, dir, "TestCtx");
            var loaded = snapshotStore.Load(dir, "TestCtx");

            var noDiffs = differ.ComputeDiff(definitions, loaded);
            noDiffs.Should().BeEmpty();
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
    public void Pipeline_DuplicateDetection_AcrossSources()
    {
        var options = new ManagedViewOptions();

        var sqlSource = Substitute.For<IManagedViewDiscovery>();
        sqlSource.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            new ManagedViewDefinition { ViewName = "vw_shared", Schema = "public", Sql = "SELECT 1", Source = "sql_file" }
        ]);

        var fluentSource = Substitute.For<IManagedViewDiscovery>();
        fluentSource.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            new ManagedViewDefinition { ViewName = "vw_shared", Schema = "public", Sql = "SELECT 2", Source = "fluent_api" }
        ]);

        var composite = new CompositeViewDiscoveryService([sqlSource, fluentSource]);

        var act = () => composite.DiscoverAll(options);
        act.Should().Throw<Exceptions.ManagedViewDuplicateException>();
    }

    [Fact]
    public void Pipeline_CircularDependency_DetectedInResolveOrder()
    {
        var options = new ManagedViewOptions();
        var resolver = new TopologicalSortResolver();

        var source = Substitute.For<IManagedViewDiscovery>();
        source.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            MakeDef("vw_a", "SELECT 1", dependsOn: "vw_c"),
            MakeDef("vw_b", "SELECT 1", dependsOn: "vw_a"),
            MakeDef("vw_c", "SELECT 1", dependsOn: "vw_b")
        ]);

        var composite = new CompositeViewDiscoveryService([source]);
        var definitions = composite.DiscoverAll(options);

        var act = () => resolver.ResolveCreateOrder(definitions);
        act.Should().Throw<Exceptions.ManagedViewDependencyCycleException>();
    }

    [Fact]
    public void Pipeline_ModifiedView_DetectedByDiffer()
    {
        var options = new ManagedViewOptions();
        var hasher = new Sha256ViewHasher(Options.Create(options));
        var differ = new ManagedViewDiffer(hasher);

        var snapshot = new ManagedViewSnapshot
        {
            Views =
            [
                new ManagedViewSnapshotEntry
                {
                    ViewName = "vw_test", Schema = "public", Type = "View",
                    Hash = hasher.ComputeHash("SELECT 1"), Sql = "SELECT 1"
                }
            ]
        };

        var currentDefs = new IManagedViewDefinition[]
        {
            MakeDef("vw_test", "SELECT 1, 2")
        };

        var diffs = differ.ComputeDiff(currentDefs, snapshot);
        diffs.Should().ContainSingle();
        diffs[0].DiffType.Should().Be(ManagedViewDiffType.Modified);
    }

    [Fact]
    public void Pipeline_DropOrderIsReverseOfCreateOrder()
    {
        var resolver = new TopologicalSortResolver();
        var definitions = new[]
        {
            MakeDef("vw_c", "SELECT 1", dependsOn: "vw_b"),
            MakeDef("vw_b", "SELECT 1", dependsOn: "vw_a"),
            MakeDef("vw_a", "SELECT 1")
        };

        var createOrder = resolver.ResolveCreateOrder(definitions);
        var dropOrder = resolver.ResolveDropOrder(definitions);

        createOrder.Select(d => d.ViewName).Should().ContainInOrder("vw_a", "vw_b", "vw_c");
        dropOrder.Select(d => d.ViewName).Should().ContainInOrder("vw_c", "vw_b", "vw_a");
    }

}
