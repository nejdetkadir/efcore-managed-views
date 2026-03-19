using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Migrations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Migrations;

public sealed class ManagedViewDesignTimeServicesTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ManagedViewOptions()));
        new ManagedViewDesignTimeServices().ConfigureDesignTimeServices(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void ConfigureDesignTimeServices_RegistersHasher()
    {
        using var provider = BuildProvider();
        provider.GetService<IManagedViewHasher>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureDesignTimeServices_RegistersDiffer()
    {
        using var provider = BuildProvider();
        provider.GetService<IManagedViewDiffer>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureDesignTimeServices_RegistersSnapshotStore()
    {
        using var provider = BuildProvider();
        provider.GetService<IManagedViewSnapshotStore>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureDesignTimeServices_RegistersDependencyResolver()
    {
        using var provider = BuildProvider();
        provider.GetService<IManagedViewDependencyResolver>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureDesignTimeServices_RegistersCompositeDiscovery()
    {
        using var provider = BuildProvider();
        provider.GetService<CompositeViewDiscoveryService>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureDesignTimeServices_RegistersSqlFileDiscovery()
    {
        using var provider = BuildProvider();
        provider.GetService<SqlFileViewDiscoveryService>().Should().NotBeNull();
    }
}
