namespace EntityFrameworkCore.ManagedViews.Snapshot;

/// <summary>
/// Represents an index entry within a snapshot.
/// </summary>
public sealed class ManagedViewSnapshotIndexEntry
{
    /// <summary>
    /// The name of the index.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The columns included in the index.
    /// </summary>
    public required string Columns { get; init; }
}
