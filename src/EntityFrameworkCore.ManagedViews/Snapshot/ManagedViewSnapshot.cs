namespace EntityFrameworkCore.ManagedViews.Snapshot;

/// <summary>
/// Represents a point-in-time snapshot of all managed view definitions.
/// Serialized as JSON and stored alongside EF Core's ModelSnapshot.
/// </summary>
public sealed class ManagedViewSnapshot
{
    /// <summary>
    /// The schema version of this snapshot format.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// When this snapshot was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// All view entries in this snapshot.
    /// </summary>
    public IReadOnlyList<ManagedViewSnapshotEntry> Views { get; init; } = [];
}
