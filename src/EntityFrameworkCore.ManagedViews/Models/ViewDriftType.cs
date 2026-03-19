namespace EntityFrameworkCore.ManagedViews.Models;

/// <summary>
/// Types of drift that can be detected between tracked views and the database.
/// </summary>
public enum ViewDriftType
{
    /// <summary>View exists in tracking table but not in database.</summary>
    Missing,

    /// <summary>View exists but its definition has changed outside of migrations.</summary>
    Modified,

    /// <summary>View exists in database but is not tracked (manually created).</summary>
    Untracked
}
