namespace EntityFrameworkCore.ManagedViews.Models;

/// <summary>
/// Represents an index on a materialized view.
/// </summary>
public sealed record ManagedViewIndex(string Name, string Columns);
