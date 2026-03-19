namespace EntityFrameworkCore.ManagedViews.Models;

/// <summary>
/// Represents a row in the __ManagedViewsHistory tracking table.
/// </summary>
public sealed class ManagedViewHistoryEntry
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
    /// The SHA256 hash of the view SQL.
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// The type of view (View or Materialized).
    /// </summary>
    public required string ViewType { get; init; }

    /// <summary>
    /// When the view was created or last updated.
    /// </summary>
    public required DateTimeOffset AppliedAt { get; init; }
}
