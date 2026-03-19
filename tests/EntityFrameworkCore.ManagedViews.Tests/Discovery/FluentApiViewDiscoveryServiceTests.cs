using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Extensions;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Discovery;

public sealed class FluentApiViewDiscoveryServiceTests
{
#pragma warning disable CA1812
    private sealed class EmptyContext(DbContextOptions options) : DbContext(options)
    {
    }

    private sealed class SingleViewContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasManagedView("vw_test", v => v.AsSql("SELECT 1"));
        }
    }

    private sealed class MultiViewContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasManagedView("vw_a", v => v.AsSql("SELECT 1"));
            modelBuilder.HasManagedView("vw_b", v => v.InSchema("catalog").AsSql("SELECT 2"));
        }
    }

    private sealed class MaterializedViewContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasManagedView("mv_test", v => v.AsMaterialized().AsSql("SELECT 1"));
        }
    }
#pragma warning restore CA1812

    [Fact]
    public void Discover_NoAnnotations_ReturnsEmpty()
    {
        var opts = new DbContextOptionsBuilder<EmptyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        using var context = new EmptyContext(opts);
        var sut = new FluentApiViewDiscoveryService(context);

        var result = sut.Discover(new ManagedViewOptions());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Discover_WithManagedView_ReturnsDefinitions()
    {
        var opts = new DbContextOptionsBuilder<SingleViewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        using var context = new SingleViewContext(opts);
        var sut = new FluentApiViewDiscoveryService(context);

        var result = sut.Discover(new ManagedViewOptions());

        result.Should().ContainSingle();
        result[0].ViewName.Should().Be("vw_test");
    }

    [Fact]
    public void Discover_MultipleViews_ReturnsAll()
    {
        var opts = new DbContextOptionsBuilder<MultiViewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        using var context = new MultiViewContext(opts);
        var sut = new FluentApiViewDiscoveryService(context);

        var result = sut.Discover(new ManagedViewOptions());

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Discover_MaterializedView_PreservesType()
    {
        var opts = new DbContextOptionsBuilder<MaterializedViewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        using var context = new MaterializedViewContext(opts);
        var sut = new FluentApiViewDiscoveryService(context);

        var result = sut.Discover(new ManagedViewOptions());

        result[0].ViewType.Should().Be(ManagedViewType.Materialized);
    }
}
