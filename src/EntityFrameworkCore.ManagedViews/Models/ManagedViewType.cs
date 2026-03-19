namespace EntityFrameworkCore.ManagedViews.Models;

/// <summary>
/// Types of database views supported by the package.
/// </summary>
public enum ManagedViewType
{
    /// <summary>Standard database view (CREATE VIEW).</summary>
    View = 0,

    /// <summary>Materialized view with stored data (CREATE MATERIALIZED VIEW).</summary>
    Materialized = 1
}
