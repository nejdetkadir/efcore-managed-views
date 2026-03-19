using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.IntegrationTests.Infrastructure;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.PostgreSQL;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.IntegrationTests;

[Collection("PostgreSQL")]
public class DependencyOrderingIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public DependencyOrderingIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ViewsWithDependencies_CreatedInCorrectOrder()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString);
        await using var context = new DbContext(optionsBuilder.Options);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS dep_orders (
                id SERIAL PRIMARY KEY, product TEXT, amount DECIMAL, status TEXT
            );
        """);

        var baseView = new ManagedViewDefinition
        {
            ViewName = "vw_dep_base",
            Schema = "public",
            ViewType = ManagedViewType.View,
            Sql = "SELECT id, product, amount FROM dep_orders WHERE status = 'completed'",
            Source = "test"
        };

        var derivedView = new ManagedViewDefinition
        {
            ViewName = "vw_dep_summary",
            Schema = "public",
            ViewType = ManagedViewType.View,
            Sql = "SELECT product, SUM(amount) as total FROM vw_dep_base GROUP BY product",
            DependsOn = ["vw_dep_base"],
            Source = "test"
        };

        // Resolve order
        var resolver = new TopologicalSortResolver();
        var ordered = resolver.ResolveCreateOrder([baseView, derivedView]);

        ordered[0].ViewName.Should().Be("vw_dep_base");
        ordered[1].ViewName.Should().Be("vw_dep_summary");

        // Create in resolved order
        var provider = new PostgreSqlManagedViewProvider();
        foreach (var def in ordered)
        {
            await context.Database.ExecuteSqlRawAsync(provider.GenerateCreateSql(def));
        }

        // Query derived view
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO dep_orders (product, amount, status)
            VALUES ('Widget', 100.00, 'completed'), ('Widget', 200.00, 'completed'), ('Gadget', 50.00, 'pending');
        """);

        // Re-create views (they already exist from CREATE OR REPLACE)
        var count = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM public.vw_dep_summary")
            .FirstAsync();
        count.Should().Be(1); // Only Widget has completed orders

        // Drop in reverse order
        var dropOrder = resolver.ResolveDropOrder([baseView, derivedView]);
        dropOrder[0].ViewName.Should().Be("vw_dep_summary");
        dropOrder[1].ViewName.Should().Be("vw_dep_base");

        foreach (var def in dropOrder)
        {
            await context.Database.ExecuteSqlRawAsync(provider.GenerateDropSql(def));
        }

        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS dep_orders CASCADE");
    }
}
