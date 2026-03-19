using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Extensions;
using EntityFrameworkCore.ManagedViews.IntegrationTests.Infrastructure;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.IntegrationTests;

#pragma warning disable CA1812

internal sealed class TestDiscoveryExtension : IDbContextOptionsExtension
{
    private readonly IManagedViewDiscovery _discovery;

    public TestDiscoveryExtension(IManagedViewDiscovery discovery) => _discovery = discovery;

    public DbContextOptionsExtensionInfo Info => new ExtInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(_discovery);
    }

    public void Validate(IDbContextOptions options) { }

    private sealed class ExtInfo(IDbContextOptionsExtension ext) : DbContextOptionsExtensionInfo(ext)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "TestDiscovery ";
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => false;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) { }
    }
}

#pragma warning restore CA1812

[Collection("PostgreSQL")]
public class DbContextExtensionsFullTests
{
    private readonly PostgreSqlFixture _fixture;

    public DbContextExtensionsFullTests(PostgreSqlFixture fixture) => _fixture = fixture;

    private DbContext CreateContext(IManagedViewDiscovery? discovery = null)
    {
        var builder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .EnableServiceProviderCaching(false);

        if (discovery is not null)
        {
            ((IDbContextOptionsBuilderInfrastructure)builder)
                .AddOrUpdateExtension(new TestDiscoveryExtension(discovery));
        }

        builder.UseNpgsqlManagedViews();

        return new DbContext(builder.Options);
    }

    [Fact]
    public async Task RefreshMaterializedViewAsync_RefreshesSuccessfully()
    {
        await using var context = CreateContext();
        var repo = context.GetService<IManagedViewHistoryRepository>();
        await repo.EnsureCreatedAsync();

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ext_r1 (id SERIAL PRIMARY KEY, val INT);
            DROP MATERIALIZED VIEW IF EXISTS public.mv_ext_r1 CASCADE;
            CREATE MATERIALIZED VIEW public.mv_ext_r1 AS SELECT id, val FROM ext_r1;
        """);

        await context.RefreshMaterializedViewAsync("mv_ext_r1");

        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS public.mv_ext_r1 CASCADE;");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS ext_r1 CASCADE;");
    }

    [Fact]
    public async Task RefreshMaterializedViewAsync_Concurrently()
    {
        await using var context = CreateContext();
        var repo = context.GetService<IManagedViewHistoryRepository>();
        await repo.EnsureCreatedAsync();

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ext_rc1 (id SERIAL PRIMARY KEY, val INT);
            DROP MATERIALIZED VIEW IF EXISTS public.mv_ext_rc1 CASCADE;
            CREATE MATERIALIZED VIEW public.mv_ext_rc1 AS SELECT id, val FROM ext_rc1;
            CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_ext_rc1 ON public.mv_ext_rc1 (id);
        """);

        await context.RefreshMaterializedViewAsync("mv_ext_rc1", concurrently: true);

        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS public.mv_ext_rc1 CASCADE;");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS ext_rc1 CASCADE;");
    }

    [Fact]
    public async Task RefreshMaterializedViewAsync_NullContext_Throws()
    {
        DbContext ctx = null!;
        var act = async () => await ctx.RefreshMaterializedViewAsync("test");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RefreshMaterializedViewAsync_NullViewName_Throws()
    {
        await using var context = CreateContext();
        var act = async () => await context.RefreshMaterializedViewAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RefreshMaterializedViewAsync_EmptyViewName_Throws()
    {
        await using var context = CreateContext();
        var act = async () => await context.RefreshMaterializedViewAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RefreshMaterializedViewAsync_UsesTrackedSchema()
    {
        await using var context = CreateContext();
        var repo = context.GetService<IManagedViewHistoryRepository>();
        await repo.EnsureCreatedAsync();
        await repo.RecordAppliedAsync("mv_sch1", "public", "h1", ManagedViewType.Materialized);

        await context.Database.ExecuteSqlRawAsync("""
            DROP MATERIALIZED VIEW IF EXISTS public.mv_sch1 CASCADE;
            CREATE MATERIALIZED VIEW public.mv_sch1 AS SELECT 1 AS val;
        """);

        await context.RefreshMaterializedViewAsync("mv_sch1");

        await repo.RecordRemovedAsync("mv_sch1", "public");
        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS public.mv_sch1 CASCADE;");
    }

    [Fact]
    public async Task RefreshMaterializedViewAsync_NoTracked_UsesDefaultSchema()
    {
        await using var context = CreateContext();
        var repo = context.GetService<IManagedViewHistoryRepository>();
        await repo.EnsureCreatedAsync();

        await context.Database.ExecuteSqlRawAsync("""
            DROP MATERIALIZED VIEW IF EXISTS public.mv_def1 CASCADE;
            CREATE MATERIALIZED VIEW public.mv_def1 AS SELECT 1 AS val;
        """);

        await context.RefreshMaterializedViewAsync("mv_def1");

        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS public.mv_def1 CASCADE;");
    }

    [Fact]
    public async Task RefreshAllMaterializedViewsAsync_NullContext_Throws()
    {
        DbContext ctx = null!;
        var act = async () => await ctx.RefreshAllMaterializedViewsAsync();
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RefreshAllMaterializedViewsAsync_WithMaterializedViews()
    {
        var mockDiscovery = Substitute.For<IManagedViewDiscovery>();
        mockDiscovery.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            new ManagedViewDefinition
            {
                ViewName = "mv_all_test1", Schema = "public",
                ViewType = ManagedViewType.Materialized,
                Sql = "SELECT 1 AS val", Source = "test"
            }
        ]);

        await using var context = CreateContext(mockDiscovery);

        await context.Database.ExecuteSqlRawAsync("""
            DROP MATERIALIZED VIEW IF EXISTS public.mv_all_test1 CASCADE;
            CREATE MATERIALIZED VIEW public.mv_all_test1 AS SELECT 1 AS val;
        """);

        await context.RefreshAllMaterializedViewsAsync();

        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS public.mv_all_test1 CASCADE;");
    }

    [Fact]
    public async Task RefreshAllMaterializedViewsAsync_ConcurrentlyWithViews()
    {
        var mockDiscovery = Substitute.For<IManagedViewDiscovery>();
        mockDiscovery.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            new ManagedViewDefinition
            {
                ViewName = "mv_all_conc1", Schema = "public",
                ViewType = ManagedViewType.Materialized,
                Sql = "SELECT 1 AS id", Source = "test"
            }
        ]);

        await using var context = CreateContext(mockDiscovery);

        await context.Database.ExecuteSqlRawAsync("""
            DROP MATERIALIZED VIEW IF EXISTS public.mv_all_conc1 CASCADE;
            CREATE MATERIALIZED VIEW public.mv_all_conc1 AS SELECT 1 AS id;
            CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_all_conc1 ON public.mv_all_conc1 (id);
        """);

        await context.RefreshAllMaterializedViewsAsync(concurrently: true);

        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS public.mv_all_conc1 CASCADE;");
    }

    [Fact]
    public async Task RefreshAllMaterializedViewsAsync_FiltersOutRegularViews()
    {
        var mockDiscovery = Substitute.For<IManagedViewDiscovery>();
        mockDiscovery.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            new ManagedViewDefinition
            {
                ViewName = "vw_regular1", Schema = "public",
                ViewType = ManagedViewType.View,
                Sql = "SELECT 1", Source = "test"
            }
        ]);

        await using var context = CreateContext(mockDiscovery);
        await context.RefreshAllMaterializedViewsAsync();
    }

    [Fact]
    public async Task RefreshAllMaterializedViewsAsync_NoViews_Succeeds()
    {
        await using var context = CreateContext();
        await context.RefreshAllMaterializedViewsAsync();
    }

    [Fact]
    public async Task CheckViewDriftAsync_NullContext_Throws()
    {
        DbContext ctx = null!;
        var act = async () => await ctx.CheckViewDriftAsync();
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckViewDriftAsync_NoTracked_NoDrift()
    {
        await using var context = CreateContext();
        var repo = context.GetService<IManagedViewHistoryRepository>();
        await repo.EnsureCreatedAsync();

        var report = await context.CheckViewDriftAsync();
        report.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckViewDriftAsync_TrackedMissing_DetectsMissing()
    {
        await using var context = CreateContext();
        var repo = context.GetService<IManagedViewHistoryRepository>();
        await repo.EnsureCreatedAsync();

        await repo.RecordAppliedAsync("vw_miss1", "public", "hash1", ManagedViewType.View);

        var report = await context.CheckViewDriftAsync();

        report.HasDrift.Should().BeTrue();
        report.Entries.Should().Contain(e => e.ViewName == "vw_miss1" && e.DriftType == ViewDriftType.Missing);
        report.Entries.First(e => e.ViewName == "vw_miss1").ExpectedHash.Should().Be("hash1");

        await repo.RecordRemovedAsync("vw_miss1", "public");
    }

    [Fact]
    public async Task CheckViewDriftAsync_ModifiedView_DetectsDrift()
    {
        var hasher = context_GetService_Hasher();
        string oldHash = hasher.ComputeHash("SELECT old_definition");

        var mockDiscovery = Substitute.For<IManagedViewDiscovery>();
        mockDiscovery.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            new ManagedViewDefinition
            {
                ViewName = "vw_drift_mod1", Schema = "public",
                ViewType = ManagedViewType.View,
                Sql = "SELECT new_definition", Source = "test"
            }
        ]);

        await using var context = CreateContext(mockDiscovery);
        var repo = context.GetService<IManagedViewHistoryRepository>();
        await repo.EnsureCreatedAsync();

        await repo.RecordAppliedAsync("vw_drift_mod1", "public", oldHash, ManagedViewType.View);

        var report = await context.CheckViewDriftAsync();

        report.HasDrift.Should().BeTrue();
        report.Entries.Should().Contain(e =>
            e.ViewName == "vw_drift_mod1" && e.DriftType == ViewDriftType.Modified);

        var entry = report.Entries.First(e => e.ViewName == "vw_drift_mod1");
        entry.ExpectedHash.Should().Be(oldHash);
        entry.ActualHash.Should().NotBe(oldHash);

        await repo.RecordRemovedAsync("vw_drift_mod1", "public");
    }

    [Fact]
    public async Task CheckViewDriftAsync_UntrackedView_DetectsUntracked()
    {
        var mockDiscovery = Substitute.For<IManagedViewDiscovery>();
        mockDiscovery.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            new ManagedViewDefinition
            {
                ViewName = "vw_untrack1", Schema = "public",
                ViewType = ManagedViewType.View,
                Sql = "SELECT 1", Source = "test"
            }
        ]);

        await using var context = CreateContext(mockDiscovery);
        var repo = context.GetService<IManagedViewHistoryRepository>();
        await repo.EnsureCreatedAsync();

        var report = await context.CheckViewDriftAsync();

        report.Entries.Should().Contain(e =>
            e.ViewName == "vw_untrack1" && e.DriftType == ViewDriftType.Untracked);
        report.Entries.First(e => e.ViewName == "vw_untrack1").ActualHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckViewDriftAsync_NoDrift_WhenHashesMatch()
    {
        var hasher = context_GetService_Hasher();
        string sql = "SELECT matching_sql";
        string hash = hasher.ComputeHash(sql);

        var mockDiscovery = Substitute.For<IManagedViewDiscovery>();
        mockDiscovery.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            new ManagedViewDefinition
            {
                ViewName = "vw_match1", Schema = "public",
                ViewType = ManagedViewType.View,
                Sql = sql, Source = "test"
            }
        ]);

        await using var context = CreateContext(mockDiscovery);
        var repo = context.GetService<IManagedViewHistoryRepository>();
        await repo.EnsureCreatedAsync();

        await repo.RecordAppliedAsync("vw_match1", "public", hash, ManagedViewType.View);

        var report = await context.CheckViewDriftAsync();

        report.Entries.Should().NotContain(e =>
            e.ViewName == "vw_match1" && (e.DriftType == ViewDriftType.Modified || e.DriftType == ViewDriftType.Missing));

        await repo.RecordRemovedAsync("vw_match1", "public");
    }

    private static Hashing.Sha256ViewHasher context_GetService_Hasher()
    {
        return new Hashing.Sha256ViewHasher(
            Microsoft.Extensions.Options.Options.Create(new ManagedViewOptions()));
    }
}
