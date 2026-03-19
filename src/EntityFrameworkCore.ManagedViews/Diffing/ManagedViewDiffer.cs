using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.Snapshot;

namespace EntityFrameworkCore.ManagedViews.Diffing;

/// <summary>
/// Compares current view definitions against a previous snapshot to produce diffs.
/// </summary>
public sealed class ManagedViewDiffer : IManagedViewDiffer
{
    private readonly IManagedViewHasher _hasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewDiffer"/> class.
    /// </summary>
    public ManagedViewDiffer(IManagedViewHasher hasher)
    {
        _hasher = hasher;
    }

    /// <inheritdoc />
    public IReadOnlyList<ManagedViewDiff> ComputeDiff(
        IReadOnlyList<IManagedViewDefinition> currentDefinitions,
        ManagedViewSnapshot? previousSnapshot)
    {
        ArgumentNullException.ThrowIfNull(currentDefinitions);
        var diffs = new List<ManagedViewDiff>();

        var previousLookup = previousSnapshot?.Views
            .ToDictionary(v => $"{v.Schema}.{v.ViewName}", StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, ManagedViewSnapshotEntry>(StringComparer.OrdinalIgnoreCase);

        var currentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (IManagedViewDefinition current in currentDefinitions)
        {
            string key = $"{current.Schema}.{current.ViewName}";
            currentKeys.Add(key);

            string currentHash = _hasher.ComputeHash(current.Sql);

            if (!previousLookup.TryGetValue(key, out ManagedViewSnapshotEntry? previous))
            {
                diffs.Add(new ManagedViewDiff
                {
                    Definition = current,
                    DiffType = ManagedViewDiffType.Added,
                    CurrentHash = currentHash
                });
            }
            else if (!string.Equals(currentHash, previous.Hash, StringComparison.Ordinal))
            {
                diffs.Add(new ManagedViewDiff
                {
                    Definition = current,
                    DiffType = ManagedViewDiffType.Modified,
                    PreviousHash = previous.Hash,
                    CurrentHash = currentHash
                });
            }
        }

        foreach (ManagedViewSnapshotEntry previous in previousSnapshot?.Views ?? [])
        {
            string key = $"{previous.Schema}.{previous.ViewName}";

            if (!currentKeys.Contains(key))
            {
                diffs.Add(new ManagedViewDiff
                {
                    Definition = new ManagedViewDefinition
                    {
                        ViewName = previous.ViewName,
                        Schema = previous.Schema,
                        ViewType = Enum.Parse<ManagedViewType>(previous.Type),
                        Sql = previous.Sql,
                        Source = "snapshot"
                    },
                    DiffType = ManagedViewDiffType.Removed,
                    PreviousHash = previous.Hash
                });
            }
        }

        return diffs;
    }
}
