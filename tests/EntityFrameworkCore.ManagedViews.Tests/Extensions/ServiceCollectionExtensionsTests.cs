using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddManagedViews_RegistersHasher()
    {
        var services = new ServiceCollection();
        services.AddManagedViews();
        var provider = services.BuildServiceProvider();

        provider.GetService<IManagedViewHasher>().Should().NotBeNull();
    }

    [Fact]
    public void AddManagedViews_RegistersDiffer()
    {
        var services = new ServiceCollection();
        services.AddManagedViews();
        var provider = services.BuildServiceProvider();

        provider.GetService<IManagedViewDiffer>().Should().NotBeNull();
    }

    [Fact]
    public void AddManagedViews_RegistersSnapshotStore()
    {
        var services = new ServiceCollection();
        services.AddManagedViews();
        var provider = services.BuildServiceProvider();

        provider.GetService<IManagedViewSnapshotStore>().Should().NotBeNull();
    }

    [Fact]
    public void AddManagedViews_RegistersDependencyResolver()
    {
        var services = new ServiceCollection();
        services.AddManagedViews();
        var provider = services.BuildServiceProvider();

        provider.GetService<IManagedViewDependencyResolver>().Should().NotBeNull();
    }

    [Fact]
    public void AddManagedViews_RegistersDiscovery()
    {
        var services = new ServiceCollection();
        services.AddManagedViews();
        var provider = services.BuildServiceProvider();

        provider.GetService<IManagedViewDiscovery>().Should().NotBeNull();
    }

    [Fact]
    public void AddManagedViews_RegistersCompositeDiscovery()
    {
        var services = new ServiceCollection();
        services.AddManagedViews();
        var provider = services.BuildServiceProvider();

        provider.GetService<CompositeViewDiscoveryService>().Should().NotBeNull();
    }

    [Fact]
    public void AddManagedViews_RegistersOptions()
    {
        var services = new ServiceCollection();
        services.AddManagedViews();
        var provider = services.BuildServiceProvider();

        var options = provider.GetService<IOptions<ManagedViewOptions>>();
        options.Should().NotBeNull();
        options!.Value.DefaultSchema.Should().Be("public");
    }

    [Fact]
    public void AddManagedViews_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddManagedViews(o => o.DefaultSchema = "custom");
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ManagedViewOptions>>();
        options.Value.DefaultSchema.Should().Be("custom");
    }

    [Fact]
    public void AddManagedViews_WithNullConfigure_UsesDefaults()
    {
        var services = new ServiceCollection();
        services.AddManagedViews(null);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ManagedViewOptions>>();
        options.Value.DefaultSchema.Should().Be("public");
    }

    [Fact]
    public void AddManagedViews_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var act = () => services.AddManagedViews();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddManagedViews_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddManagedViews();
        result.Should().BeSameAs(services);
    }
}
