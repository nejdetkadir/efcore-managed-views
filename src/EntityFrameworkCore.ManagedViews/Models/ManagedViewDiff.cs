using EntityFrameworkCore.ManagedViews.Abstractions;

namespace EntityFrameworkCore.ManagedViews.Models;

/// <summary>
/// Represents a detected change between current definitions and previous snapshot.
/// </summary>
public sealed class ManagedViewDiff
{
    /// <summary>
    /// The view definition associated with this diff.
    /// </summary>
    public required IManagedViewDefinition Definition { get; init; }

    /// <summary>
    /// The type of change detected.
    /// </summary>
    public required ManagedViewDiffType DiffType { get; init; }

    /// <summary>
    /// The hash from the previous snapshot, if any.
    /// </summary>
    public string? PreviousHash { get; init; }

    /// <summary>
    /// The hash from the current definition, if any.
    /// </summary>
    public string? CurrentHash { get; init; }
}
