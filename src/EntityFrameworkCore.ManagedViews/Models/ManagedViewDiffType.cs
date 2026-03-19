namespace EntityFrameworkCore.ManagedViews.Models;

/// <summary>
/// The type of change detected for a view.
/// </summary>
public enum ManagedViewDiffType
{
    /// <summary>View is new and needs to be created.</summary>
    Added,

    /// <summary>View SQL has changed and needs to be recreated.</summary>
    Modified,

    /// <summary>View was removed from definitions and needs to be dropped.</summary>
    Removed
}
