namespace EntityFrameworkCore.ManagedViews.Models;

/// <summary>
/// Result of a drift detection check.
/// </summary>
public sealed class ViewDriftReport
{
    /// <summary>
    /// Whether any drift was detected.
    /// </summary>
    public bool HasDrift => Entries.Count > 0;

    /// <summary>
    /// The individual drift entries.
    /// </summary>
    public IReadOnlyList<ViewDriftEntry> Entries { get; init; } = [];
}
