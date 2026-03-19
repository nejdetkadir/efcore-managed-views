using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.Diffing;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.Snapshot;
using EntityFrameworkCore.ManagedViews.Tracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EntityFrameworkCore.ManagedViews.Configuration;

/// <summary>
/// EF Core options extension that carries ManagedViews configuration through the DbContext service provider.
/// </summary>
public sealed class ManagedViewOptionsExtension : IDbContextOptionsExtension
{
    /// <summary>
    /// The managed view options.
    /// </summary>
    public ManagedViewOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewOptionsExtension"/> class.
    /// </summary>
    public ManagedViewOptionsExtension(ManagedViewOptions options)
    {
        Options = options;
    }

    /// <inheritdoc />
    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(Options));
        services.TryAddSingleton<IManagedViewHasher, Sha256ViewHasher>();
        services.TryAddSingleton<IManagedViewDiffer, ManagedViewDiffer>();
        services.TryAddSingleton<IManagedViewSnapshotStore, JsonSnapshotStore>();
        services.TryAddSingleton<IManagedViewDependencyResolver, TopologicalSortResolver>();
        services.TryAddSingleton<IManagedViewDiscovery, SqlFileViewDiscoveryService>();
        services.TryAddSingleton<CompositeViewDiscoveryService>();
        services.TryAddScoped<IManagedViewHistoryRepository>(sp =>
        {
            var currentDbContext = sp.GetRequiredService<Microsoft.EntityFrameworkCore.Infrastructure.ICurrentDbContext>();
            var viewOptions = sp.GetRequiredService<IOptions<ManagedViewOptions>>();
            return new ManagedViewHistoryRepository(currentDbContext.Context, viewOptions);
        });
    }

    /// <inheritdoc />
    public void Validate(IDbContextOptions options) { }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "ManagedViews ";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["ManagedViews:Enabled"] = "true";
        }
    }
}
