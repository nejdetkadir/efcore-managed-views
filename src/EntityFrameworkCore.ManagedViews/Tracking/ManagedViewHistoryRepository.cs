using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EntityFrameworkCore.ManagedViews.Tracking;

/// <summary>
/// Manages the __ManagedViewsHistory table using raw SQL through the DbContext's
/// database connection. Does not use EF Core entities to avoid circular dependencies.
/// </summary>
public sealed class ManagedViewHistoryRepository : IManagedViewHistoryRepository
{
    private readonly DbContext _context;
    private readonly ManagedViewOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewHistoryRepository"/> class.
    /// </summary>
    public ManagedViewHistoryRepository(DbContext context, IOptions<ManagedViewOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _context = context;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        string sql = GetCreateTableSql();
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string GetCreateTableSql()
    {
        string tableName = GetQualifiedTableName();

        return $"""
            CREATE TABLE IF NOT EXISTS {tableName} (
                "ViewName"   TEXT        NOT NULL,
                "Schema"     TEXT        NOT NULL DEFAULT 'public',
                "Hash"       TEXT        NOT NULL,
                "ViewType"   TEXT        NOT NULL DEFAULT 'View',
                "AppliedAt"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT "PK_{_options.TrackingTableName}" PRIMARY KEY ("ViewName", "Schema")
            )
            """;
    }

    /// <inheritdoc />
    public async Task RecordAppliedAsync(string viewName, string schema, string hash,
        ManagedViewType viewType, CancellationToken cancellationToken = default)
    {
        string tableName = GetQualifiedTableName();

        string sql =
            "INSERT INTO " + tableName + " (\"ViewName\", \"Schema\", \"Hash\", \"ViewType\", \"AppliedAt\") " +
            "VALUES ({0}, {1}, {2}, {3}, NOW()) " +
            "ON CONFLICT (\"ViewName\", \"Schema\") " +
            "DO UPDATE SET \"Hash\" = EXCLUDED.\"Hash\", \"ViewType\" = EXCLUDED.\"ViewType\", " +
            "\"AppliedAt\" = NOW()";

        await _context.Database.ExecuteSqlRawAsync(
            sql, [viewName, schema, hash, viewType.ToString()], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecordRemovedAsync(string viewName, string schema,
        CancellationToken cancellationToken = default)
    {
        string tableName = GetQualifiedTableName();

        string sql =
            "DELETE FROM " + tableName + " WHERE \"ViewName\" = {0} AND \"Schema\" = {1}";

        await _context.Database.ExecuteSqlRawAsync(
            sql, [viewName, schema], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetHashAsync(string viewName, string schema,
        CancellationToken cancellationToken = default)
    {
        string tableName = GetQualifiedTableName();

#pragma warning disable EF1003 // Table name comes from config, not user input
        var result = await _context.Database
            .SqlQueryRaw<string>(
                "SELECT \"Hash\" AS \"Value\" FROM " + tableName + " WHERE \"ViewName\" = {0} AND \"Schema\" = {1}",
                viewName, schema)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1003

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ManagedViewHistoryEntry>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        string tableName = GetQualifiedTableName();

#pragma warning disable EF1003 // Table name comes from config, not user input
        return await _context.Database
            .SqlQueryRaw<ManagedViewHistoryEntry>(
                "SELECT \"ViewName\", \"Schema\", \"Hash\", \"ViewType\", \"AppliedAt\" FROM " + tableName)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1003
    }

    private string GetQualifiedTableName()
    {
        string? schema = _options.TrackingTableSchema;
        string table = $"\"{_options.TrackingTableName}\"";
        return schema is not null ? $"\"{schema}\".{table}" : table;
    }
}
