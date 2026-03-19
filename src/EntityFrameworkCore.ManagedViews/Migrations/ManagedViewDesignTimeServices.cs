using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.Diffing;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.Snapshot;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.ManagedViews.Migrations;

/// <summary>
/// Registers ManagedViews services into EF Core's design-time pipeline.
/// EF Core automatically discovers this class via assembly scanning when
/// running dotnet ef commands.
/// </summary>
public sealed class ManagedViewDesignTimeServices : IDesignTimeServices
{
    /// <inheritdoc />
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IManagedViewHasher, Sha256ViewHasher>();
        serviceCollection.AddSingleton<IManagedViewDiffer, ManagedViewDiffer>();
        serviceCollection.AddSingleton<IManagedViewSnapshotStore, JsonSnapshotStore>();
        serviceCollection.AddSingleton<IManagedViewDependencyResolver, TopologicalSortResolver>();
        serviceCollection.AddSingleton<CompositeViewDiscoveryService>();
        serviceCollection.AddSingleton<SqlFileViewDiscoveryService>();
        serviceCollection.AddSingleton<FluentApiViewDiscoveryService>();
    }
}
