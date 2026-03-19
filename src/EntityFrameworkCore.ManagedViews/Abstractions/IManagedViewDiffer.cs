using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.Snapshot;

namespace EntityFrameworkCore.ManagedViews.Abstractions;

/// <summary>
/// Compares current view definitions against a previous snapshot
/// to determine which views need to be created, updated, or dropped.
/// </summary>
public interface IManagedViewDiffer
{
    /// <summary>
    /// Computes the diff between current definitions and a previous snapshot.
    /// </summary>
    /// <param name="currentDefinitions">Current view definitions from discovery.</param>
    /// <param name="previousSnapshot">Previous snapshot (null if first migration).</param>
    /// <returns>Ordered list of diffs to apply.</returns>
    IReadOnlyList<ManagedViewDiff> ComputeDiff(
        IReadOnlyList<IManagedViewDefinition> currentDefinitions,
        ManagedViewSnapshot? previousSnapshot);
}
