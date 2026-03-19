using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace EntityFrameworkCore.ManagedViews.Extensions;

/// <summary>
/// Runtime extension methods for DbContext to interact with managed views.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Refreshes a materialized view.
    /// </summary>
    public static async Task RefreshMaterializedViewAsync(
        this DbContext context,
        string viewName,
        bool concurrently = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewName);

        var provider = context.GetService<IManagedViewProvider>();
        var options = context.GetService<IOptions<ManagedViewOptions>>().Value;

        string schema = await ResolveSchemaAsync(context, viewName, options, cancellationToken).ConfigureAwait(false);

        string sql = provider.GenerateRefreshSql(viewName, schema, concurrently);
        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Refreshes all materialized views in dependency order.
    /// </summary>
    public static async Task RefreshAllMaterializedViewsAsync(
        this DbContext context,
        bool concurrently = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discovery = context.GetService<CompositeViewDiscoveryService>();
        var options = context.GetService<IOptions<ManagedViewOptions>>().Value;
        var dependencyResolver = context.GetService<IManagedViewDependencyResolver>();
        var provider = context.GetService<IManagedViewProvider>();

        IReadOnlyList<IManagedViewDefinition> allViews = discovery.DiscoverAll(options);
        var materializedViews = allViews
            .Where(v => v.ViewType == ManagedViewType.Materialized)
            .ToList();

        IReadOnlyList<IManagedViewDefinition> ordered = dependencyResolver.ResolveCreateOrder(materializedViews);

        foreach (IManagedViewDefinition view in ordered)
        {
            string sql = provider.GenerateRefreshSql(view.ViewName, view.Schema, concurrently);
            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Checks for drift between tracked view hashes and current definitions.
    /// </summary>
    public static async Task<ViewDriftReport> CheckViewDriftAsync(
        this DbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var repository = context.GetService<IManagedViewHistoryRepository>();
        var discovery = context.GetService<CompositeViewDiscoveryService>();
        var options = context.GetService<IOptions<ManagedViewOptions>>().Value;
        var hasher = context.GetService<IManagedViewHasher>();

        IReadOnlyList<ManagedViewHistoryEntry> tracked = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<IManagedViewDefinition> current = discovery.DiscoverAll(options);

        var entries = new List<ViewDriftEntry>();
        var trackedLookup = tracked.ToDictionary(
            t => $"{t.Schema}.{t.ViewName}", StringComparer.OrdinalIgnoreCase);
        var currentLookup = current.ToDictionary(
            c => $"{c.Schema}.{c.ViewName}", StringComparer.OrdinalIgnoreCase);

        foreach (var (key, trackedEntry) in trackedLookup)
        {
            if (!currentLookup.TryGetValue(key, out IManagedViewDefinition? currentDef))
            {
                entries.Add(new ViewDriftEntry
                {
                    ViewName = trackedEntry.ViewName,
                    Schema = trackedEntry.Schema,
                    DriftType = ViewDriftType.Missing,
                    ExpectedHash = trackedEntry.Hash
                });
                continue;
            }

            string currentHash = hasher.ComputeHash(currentDef.Sql);
            if (!string.Equals(trackedEntry.Hash, currentHash, StringComparison.Ordinal))
            {
                entries.Add(new ViewDriftEntry
                {
                    ViewName = trackedEntry.ViewName,
                    Schema = trackedEntry.Schema,
                    DriftType = ViewDriftType.Modified,
                    ExpectedHash = trackedEntry.Hash,
                    ActualHash = currentHash
                });
            }
        }

        foreach (var (key, currentDef) in currentLookup)
        {
            if (!trackedLookup.ContainsKey(key))
            {
                entries.Add(new ViewDriftEntry
                {
                    ViewName = currentDef.ViewName,
                    Schema = currentDef.Schema,
                    DriftType = ViewDriftType.Untracked,
                    ActualHash = hasher.ComputeHash(currentDef.Sql)
                });
            }
        }

        return new ViewDriftReport { Entries = entries };
    }

    private static async Task<string> ResolveSchemaAsync(
        DbContext context, string viewName, ManagedViewOptions options,
        CancellationToken cancellationToken)
    {
        var repository = context.GetService<IManagedViewHistoryRepository>();
        IReadOnlyList<ManagedViewHistoryEntry> tracked = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        ManagedViewHistoryEntry? match = tracked.FirstOrDefault(t =>
            t.ViewName.Equals(viewName, StringComparison.OrdinalIgnoreCase));
        return match?.Schema ?? options.DefaultSchema;
    }
}
