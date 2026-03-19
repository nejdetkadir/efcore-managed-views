using System.Security.Cryptography;
using System.Text;
using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using Microsoft.Extensions.Options;

namespace EntityFrameworkCore.ManagedViews.Hashing;

/// <summary>
/// Computes SHA256 hashes of normalized SQL for change detection.
/// </summary>
public sealed class Sha256ViewHasher : IManagedViewHasher
{
    private readonly SqlNormalizer _normalizer;
    private readonly ManagedViewOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sha256ViewHasher"/> class.
    /// </summary>
    public Sha256ViewHasher(IOptions<ManagedViewOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _normalizer = new SqlNormalizer();
        _options = options.Value;
    }

    /// <inheritdoc />
    public string ComputeHash(string sql)
    {
        string normalized = _options.NormalizeSqlBeforeHashing
            ? _normalizer.Normalize(sql, _options)
            : sql;

        byte[] bytes = Encoding.UTF8.GetBytes(normalized);
        byte[] hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexStringLower(hashBytes);
    }
}
