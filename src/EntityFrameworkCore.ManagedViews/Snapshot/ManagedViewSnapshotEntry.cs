namespace EntityFrameworkCore.ManagedViews.Snapshot;

/// <summary>
/// Represents a single view entry within a snapshot.
/// </summary>
public sealed class ManagedViewSnapshotEntry
{
    /// <summary>
    /// The name of the view.
    /// </summary>
    public required string ViewName { get; init; }

    /// <summary>
    /// The database schema.
    /// </summary>
    public required string Schema { get; init; }

    /// <summary>
    /// The type of view ("View" or "Materialized").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The SHA256 hash of the normalized SQL.
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// The raw SQL body.
    /// </summary>
    public required string Sql { get; init; }

    /// <summary>
    /// Names of views this view depends on.
    /// </summary>
    public IReadOnlyList<string> DependsOn { get; init; } = [];

    /// <summary>
    /// Index definitions for materialized views.
    /// </summary>
    public IReadOnlyList<ManagedViewSnapshotIndexEntry> Indexes { get; init; } = [];
}
