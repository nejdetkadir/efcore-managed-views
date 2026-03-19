using System.Reflection;

namespace EntityFrameworkCore.ManagedViews.Configuration;

/// <summary>
/// Global options for the ManagedViews package.
/// </summary>
public sealed class ManagedViewOptions
{
    /// <summary>
    /// Assembly containing embedded .sql resources.
    /// Defaults to the assembly containing the DbContext.
    /// </summary>
    public Assembly? ViewAssembly { get; set; }

    /// <summary>
    /// Resource name prefix filter for .sql file discovery.
    /// The package looks for embedded resources whose names contain this prefix.
    /// Default: "Views"
    /// </summary>
    public string SqlFilesPrefix { get; set; } = "Views";

    /// <summary>
    /// Schema for the __ManagedViewsHistory tracking table.
    /// Defaults to null (uses the database's default schema).
    /// </summary>
    public string? TrackingTableSchema { get; set; }

    /// <summary>
    /// Name of the tracking table.
    /// Default: "__ManagedViewsHistory"
    /// </summary>
    public string TrackingTableName { get; set; } = "__ManagedViewsHistory";

    /// <summary>
    /// Whether to automatically create the tracking table during migration.
    /// Default: true
    /// </summary>
    public bool AutoCreateTrackingTable { get; set; } = true;

    /// <summary>
    /// Default schema for views when not explicitly specified.
    /// Default: "public" (PostgreSQL convention)
    /// </summary>
    public string DefaultSchema { get; set; } = "public";

    /// <summary>
    /// Whether to normalize SQL whitespace before hashing.
    /// Prevents formatting-only changes from triggering new migrations.
    /// Default: true
    /// </summary>
    public bool NormalizeSqlBeforeHashing { get; set; } = true;

    /// <summary>
    /// Whether to strip SQL comments before hashing.
    /// Default: true
    /// </summary>
    public bool StripCommentsBeforeHashing { get; set; } = true;
}
