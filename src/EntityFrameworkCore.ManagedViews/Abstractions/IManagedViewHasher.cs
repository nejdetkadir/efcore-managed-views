namespace EntityFrameworkCore.ManagedViews.Abstractions;

/// <summary>
/// Computes a deterministic hash of a view's SQL definition for change detection.
/// </summary>
public interface IManagedViewHasher
{
    /// <summary>
    /// Computes the hash of a SQL string.
    /// The SQL is normalized before hashing if configured.
    /// </summary>
    string ComputeHash(string sql);
}
