using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.Diffing;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.Snapshot;
using EntityFrameworkCore.ManagedViews.Tracking;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.ManagedViews.Extensions;

/// <summary>
/// Extension methods for registering ManagedViews services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ManagedViews services to the service collection.
    /// </summary>
    public static IServiceCollection AddManagedViews(
        this IServiceCollection services,
        Action<ManagedViewOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ManagedViewOptions();
        configure?.Invoke(options);

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));
        services.AddSingleton<IManagedViewHasher, Sha256ViewHasher>();
        services.AddSingleton<IManagedViewDiffer, ManagedViewDiffer>();
        services.AddSingleton<IManagedViewSnapshotStore, JsonSnapshotStore>();
        services.AddSingleton<IManagedViewDependencyResolver, TopologicalSortResolver>();
        services.AddSingleton<IManagedViewDiscovery, SqlFileViewDiscoveryService>();
        services.AddScoped<IManagedViewHistoryRepository, ManagedViewHistoryRepository>();
        services.AddSingleton<CompositeViewDiscoveryService>();

        return services;
    }
}
