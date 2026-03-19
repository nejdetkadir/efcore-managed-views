namespace EntityFrameworkCore.ManagedViews.Models;

/// <summary>
/// Represents a single drift entry in a drift detection report.
/// </summary>
public sealed class ViewDriftEntry
{
    /// <summary>
    /// The name of the view.
    /// </summary>
    public required string ViewName { get; init; }

    /// <summary>
    /// The database schema of the view.
    /// </summary>
    public required string Schema { get; init; }

    /// <summary>
    /// The type of drift detected.
    /// </summary>
    public required ViewDriftType DriftType { get; init; }

    /// <summary>
    /// The expected hash from the tracking table.
    /// </summary>
    public string? ExpectedHash { get; init; }

    /// <summary>
    /// The actual hash computed from the current definition.
    /// </summary>
    public string? ActualHash { get; init; }
}
