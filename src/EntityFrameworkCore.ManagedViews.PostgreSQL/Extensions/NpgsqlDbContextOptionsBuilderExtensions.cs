using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions;

/// <summary>
/// PostgreSQL-specific extension methods for enabling managed views.
/// </summary>
public static class NpgsqlDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Enables managed view support for a PostgreSQL DbContext.
    /// This is a convenience method that calls <see cref="DbContextOptionsBuilderExtensions.UseManagedViews"/>
    /// and registers the PostgreSQL provider.
    /// </summary>
    public static DbContextOptionsBuilder UseNpgsqlManagedViews(
        this DbContextOptionsBuilder builder,
        Action<ManagedViewOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseManagedViews(configure);

        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(
            new NpgsqlManagedViewOptionsExtension());

        return builder;
    }
}

/// <summary>
/// EF Core options extension that registers PostgreSQL managed view services.
/// </summary>
public sealed class NpgsqlManagedViewOptionsExtension : IDbContextOptionsExtension
{
    /// <inheritdoc />
    public DbContextOptionsExtensionInfo Info => new ExtInfo(this);

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton<IManagedViewProvider, PostgreSqlManagedViewProvider>();
    }

    /// <inheritdoc />
    public void Validate(IDbContextOptions options) { }

    private sealed class ExtInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "NpgsqlManagedViews ";
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtInfo;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["NpgsqlManagedViews:Enabled"] = "true";
    }
}
