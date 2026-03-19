using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.Diffing;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Extensions;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.IntegrationTests.Infrastructure;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.PostgreSQL;
using EntityFrameworkCore.ManagedViews.Snapshot;
using EntityFrameworkCore.ManagedViews.Tracking;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.IntegrationTests;

[Collection("PostgreSQL")]
public class DbContextExtensionsIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public DbContextExtensionsIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    private DbContext CreateConfiguredContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .UseManagedViews();

        var context = new DbContext(optionsBuilder.Options);
        return context;
    }

    [Fact]
    public async Task RefreshMaterializedViewAsync_RefreshesView()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .UseManagedViews();
        await using var context = new DbContext(optionsBuilder.Options);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ext_refresh_test (id SERIAL PRIMARY KEY, val INT);
            DROP MATERIALIZED VIEW IF EXISTS public.mv_ext_refresh CASCADE;
            CREATE MATERIALIZED VIEW public.mv_ext_refresh AS SELECT id, val FROM ext_refresh_test;
        """);

        var services = new ServiceCollection();
        services.AddSingleton<IManagedViewProvider, PostgreSqlManagedViewProvider>();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ManagedViewOptions()));
        services.AddScoped<IManagedViewHistoryRepository>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ManagedViewOptions>>();
            return new ManagedViewHistoryRepository(context, opts);
        });

        using var provider = services.BuildServiceProvider();
        var viewProvider = provider.GetRequiredService<IManagedViewProvider>();
        string sql = viewProvider.GenerateRefreshSql("mv_ext_refresh", "public", false);
        await context.Database.ExecuteSqlRawAsync(sql);

        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS public.mv_ext_refresh CASCADE;");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS ext_refresh_test CASCADE;");
    }

    [Fact]
    public async Task CheckViewDrift_NoDrift_ReturnsNoDrift()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString);
        await using var context = new DbContext(optionsBuilder.Options);

        var managedViewOptions = Microsoft.Extensions.Options.Options.Create(new ManagedViewOptions());
        var hasher = new Sha256ViewHasher(managedViewOptions);
        var repo = new ManagedViewHistoryRepository(context, managedViewOptions);
        await repo.EnsureCreatedAsync();

        string viewSql = "SELECT 1 AS val";
        string hash = hasher.ComputeHash(viewSql);

        await repo.RecordAppliedAsync("vw_drift_check", "public", hash, ManagedViewType.View);

        var trackedHash = await repo.GetHashAsync("vw_drift_check", "public");
        trackedHash.Should().Be(hash);

        await repo.RecordRemovedAsync("vw_drift_check", "public");
    }

    [Fact]
    public async Task CheckViewDrift_ModifiedView_DetectsDrift()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString);
        await using var context = new DbContext(optionsBuilder.Options);

        var managedViewOptions = Microsoft.Extensions.Options.Options.Create(new ManagedViewOptions());
        var hasher = new Sha256ViewHasher(managedViewOptions);
        var repo = new ManagedViewHistoryRepository(context, managedViewOptions);
        await repo.EnsureCreatedAsync();

        string originalHash = hasher.ComputeHash("SELECT 1 AS val");
        string modifiedHash = hasher.ComputeHash("SELECT 2 AS val");

        await repo.RecordAppliedAsync("vw_drift_mod", "public", originalHash, ManagedViewType.View);

        var trackedHash = await repo.GetHashAsync("vw_drift_mod", "public");
        trackedHash.Should().Be(originalHash);
        trackedHash.Should().NotBe(modifiedHash);

        await repo.RecordRemovedAsync("vw_drift_mod", "public");
    }

    [Fact]
    public async Task ManagedViewHistoryRepository_WithSchema_QualifiesTableName()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString);
        await using var context = new DbContext(optionsBuilder.Options);

        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS test_schema");

        var managedViewOptions = Microsoft.Extensions.Options.Options.Create(
            new ManagedViewOptions { TrackingTableSchema = "test_schema" });
        var repo = new ManagedViewHistoryRepository(context, managedViewOptions);

        await repo.EnsureCreatedAsync();
        await repo.RecordAppliedAsync("vw_schema_test", "test_schema", "hash1", ManagedViewType.View);

        var hash = await repo.GetHashAsync("vw_schema_test", "test_schema");
        hash.Should().Be("hash1");

        var allEntries = await repo.GetAllAsync();
        allEntries.Should().Contain(e => e.ViewName == "vw_schema_test");

        await repo.RecordRemovedAsync("vw_schema_test", "test_schema");

        string createSql = repo.GetCreateTableSql();
        createSql.Should().Contain("\"test_schema\"");
    }
}
