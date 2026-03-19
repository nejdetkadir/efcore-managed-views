using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.IntegrationTests.Infrastructure;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.PostgreSQL;
using EntityFrameworkCore.ManagedViews.Tracking;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.IntegrationTests;

[Collection("PostgreSQL")]
public class EndToEndWorkflowTests
{
    private readonly PostgreSqlFixture _fixture;

    public EndToEndWorkflowTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FullLifecycle_CreateView_TrackIt_DetectNoChange_ModifyView_DetectDrift()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString);
        await using var context = new DbContext(optionsBuilder.Options);

        // Setup
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS e2e_products (
                id SERIAL PRIMARY KEY, name TEXT, price DECIMAL, is_active BOOLEAN DEFAULT true
            );
            DELETE FROM e2e_products;
            INSERT INTO e2e_products (name, price) VALUES ('A', 10.00), ('B', 20.00), ('C', 30.00);
        """);

        var provider = new PostgreSqlManagedViewProvider();
        var managedViewOptions = Options.Create(new ManagedViewOptions());
        var hasher = new Sha256ViewHasher(managedViewOptions);
        var repo = new ManagedViewHistoryRepository(context, managedViewOptions);
        await repo.EnsureCreatedAsync();

        // Step 1: Create view
        string viewSql = "SELECT id, name, price FROM e2e_products WHERE is_active = true";
        var definition = new ManagedViewDefinition
        {
            ViewName = "vw_e2e_products",
            Schema = "public",
            ViewType = ManagedViewType.View,
            Sql = viewSql,
            Source = "test"
        };

        await context.Database.ExecuteSqlRawAsync(provider.GenerateCreateSql(definition));

        // Step 2: Record in tracking table
        string hash = hasher.ComputeHash(viewSql);
        await repo.RecordAppliedAsync("vw_e2e_products", "public", hash, ManagedViewType.View);

        // Step 3: Verify tracking
        var trackedHash = await repo.GetHashAsync("vw_e2e_products", "public");
        trackedHash.Should().Be(hash);

        // Step 4: Query the view
        var count = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM public.vw_e2e_products")
            .FirstAsync();
        count.Should().Be(3);

        // Step 5: Same SQL should produce same hash (no drift)
        string sameHash = hasher.ComputeHash(viewSql);
        sameHash.Should().Be(hash);

        // Step 6: Modify the view SQL
        string modifiedSql = "SELECT id, name, price FROM e2e_products WHERE is_active = true AND price > 15.00";
        string newHash = hasher.ComputeHash(modifiedSql);
        newHash.Should().NotBe(hash, "modified SQL should produce a different hash");

        // Step 7: Apply modified view
        var modifiedDef = new ManagedViewDefinition
        {
            ViewName = "vw_e2e_products",
            Schema = "public",
            ViewType = ManagedViewType.View,
            Sql = modifiedSql,
            Source = "test"
        };
        await context.Database.ExecuteSqlRawAsync(provider.GenerateCreateSql(modifiedDef));
        await repo.RecordAppliedAsync("vw_e2e_products", "public", newHash, ManagedViewType.View);

        // Step 8: Verify modified view returns filtered results
        var filteredCount = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM public.vw_e2e_products")
            .FirstAsync();
        filteredCount.Should().Be(2); // B (20) and C (30)

        // Step 9: Verify updated hash in tracking
        var updatedHash = await repo.GetHashAsync("vw_e2e_products", "public");
        updatedHash.Should().Be(newHash);

        // Cleanup
        await context.Database.ExecuteSqlRawAsync(provider.GenerateDropSql(definition));
        await repo.RecordRemovedAsync("vw_e2e_products", "public");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS e2e_products CASCADE");
    }

    [Fact]
    public async Task MaterializedView_CreateRefreshDrop_FullCycle()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString);
        await using var context = new DbContext(optionsBuilder.Options);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS e2e_sales (
                id SERIAL PRIMARY KEY, product TEXT, amount DECIMAL, sale_date DATE DEFAULT CURRENT_DATE
            );
            DELETE FROM e2e_sales;
            INSERT INTO e2e_sales (product, amount) VALUES ('X', 100), ('X', 200), ('Y', 300);
        """);

        var provider = new PostgreSqlManagedViewProvider();
        var managedViewOptions = Options.Create(new ManagedViewOptions());
        var hasher = new Sha256ViewHasher(managedViewOptions);
        var repo = new ManagedViewHistoryRepository(context, managedViewOptions);
        await repo.EnsureCreatedAsync();

        // Create materialized view with index
        string mvSql = "SELECT product, SUM(amount) AS total, COUNT(*) AS num_sales FROM e2e_sales GROUP BY product";
        var mvDef = new ManagedViewDefinition
        {
            ViewName = "mv_e2e_sales_summary",
            Schema = "public",
            ViewType = ManagedViewType.Materialized,
            Sql = mvSql,
            Indexes = [new ManagedViewIndex("idx_mv_e2e_product", "product")],
            Source = "test"
        };

        // Drop if exists from previous test run
        await context.Database.ExecuteSqlRawAsync(provider.GenerateDropSql(mvDef));
        await context.Database.ExecuteSqlRawAsync(provider.GenerateCreateSql(mvDef));

        // Create index
        foreach (string indexSql in provider.GenerateCreateIndexSql(mvDef))
        {
            await context.Database.ExecuteSqlRawAsync(indexSql);
        }

        // Track it
        await repo.RecordAppliedAsync("mv_e2e_sales_summary", "public", hasher.ComputeHash(mvSql), ManagedViewType.Materialized);

        // Query materialized view
        var results = await context.Database
            .SqlQueryRaw<decimal>("SELECT total AS \"Value\" FROM public.mv_e2e_sales_summary WHERE product = 'X'")
            .FirstAsync();
        results.Should().Be(300m);

        // Add more data - mat view is stale
        await context.Database.ExecuteSqlRawAsync("INSERT INTO e2e_sales (product, amount) VALUES ('X', 400)");

        // Stale: mat view still shows old total
        var staleResult = await context.Database
            .SqlQueryRaw<decimal>("SELECT total AS \"Value\" FROM public.mv_e2e_sales_summary WHERE product = 'X'")
            .FirstAsync();
        staleResult.Should().Be(300m);

        // Refresh
        await context.Database.ExecuteSqlRawAsync(
            provider.GenerateRefreshSql("mv_e2e_sales_summary", "public", concurrently: false));

        // After refresh: shows new total
        var refreshedResult = await context.Database
            .SqlQueryRaw<decimal>("SELECT total AS \"Value\" FROM public.mv_e2e_sales_summary WHERE product = 'X'")
            .FirstAsync();
        refreshedResult.Should().Be(700m);

        // Cleanup
        await context.Database.ExecuteSqlRawAsync(provider.GenerateDropSql(mvDef));
        await repo.RecordRemovedAsync("mv_e2e_sales_summary", "public");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS e2e_sales CASCADE");
    }
}
