using EntityFrameworkCore.ManagedViews.Models;

namespace EntityFrameworkCore.ManagedViews.Abstractions;

/// <summary>
/// Manages the __ManagedViewsHistory tracking table for runtime change detection
/// and drift detection.
/// </summary>
public interface IManagedViewHistoryRepository
{
    /// <summary>
    /// Ensures the tracking table exists in the database.
    /// </summary>
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a view was created or updated.
    /// </summary>
    Task RecordAppliedAsync(string viewName, string schema, string hash,
        ManagedViewType viewType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a view was dropped.
    /// </summary>
    Task RecordRemovedAsync(string viewName, string schema,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current hash for a view, or null if not tracked.
    /// </summary>
    Task<string?> GetHashAsync(string viewName, string schema,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tracked view entries for drift detection.
    /// </summary>
    Task<IReadOnlyList<ManagedViewHistoryEntry>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the SQL to create the tracking table.
    /// Used inside migrations.
    /// </summary>
    string GetCreateTableSql();
}
