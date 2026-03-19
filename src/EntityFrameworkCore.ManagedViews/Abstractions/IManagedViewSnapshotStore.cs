using EntityFrameworkCore.ManagedViews.Snapshot;

namespace EntityFrameworkCore.ManagedViews.Abstractions;

/// <summary>
/// Persists and retrieves managed view snapshots for design-time change detection.
/// </summary>
public interface IManagedViewSnapshotStore
{
    /// <summary>
    /// Loads the previous snapshot from the migrations directory.
    /// Returns null if no snapshot exists (first migration).
    /// </summary>
    ManagedViewSnapshot? Load(string migrationsDirectory, string contextName);

    /// <summary>
    /// Saves the current snapshot to the migrations directory.
    /// </summary>
    void Save(ManagedViewSnapshot snapshot, string migrationsDirectory, string contextName);
}
