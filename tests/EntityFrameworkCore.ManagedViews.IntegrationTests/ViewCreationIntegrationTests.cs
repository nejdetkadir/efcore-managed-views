using EntityFrameworkCore.ManagedViews.IntegrationTests.Infrastructure;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.PostgreSQL;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class ViewCreationIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;
    public ViewCreationIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateRegularView_CanBeQueried()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString);
        await using var context = new DbContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS products (
                id SERIAL PRIMARY KEY, name TEXT, price DECIMAL, is_active BOOLEAN
            );
            DELETE FROM products;
            INSERT INTO products (name, price, is_active)
            VALUES ('Rose Bouquet', 49.99, true), ('Dead Flowers', 9.99, false);
        """);

        var provider = new PostgreSqlManagedViewProvider();
        var definition = new ManagedViewDefinition
        {
            ViewName = "vw_active_products_test",
            Schema = "public",
            ViewType = ManagedViewType.View,
            Sql = "SELECT id, name, price FROM products WHERE is_active = true",
            Source = "test"
        };

        await context.Database.ExecuteSqlRawAsync(provider.GenerateCreateSql(definition));

        var count = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM public.vw_active_products_test")
            .FirstAsync();

        count.Should().Be(1);

        await context.Database.ExecuteSqlRawAsync(provider.GenerateDropSql(definition));
    }

    [Fact]
    public async Task CreateMaterializedView_WithRefresh_Works()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString);
        await using var context = new DbContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS orders_test (
                id SERIAL PRIMARY KEY, total DECIMAL
            );
            DELETE FROM orders_test;
            INSERT INTO orders_test (total) VALUES (100.00), (200.00);
        """);

        var provider = new PostgreSqlManagedViewProvider();
        var definition = new ManagedViewDefinition
        {
            ViewName = "mv_order_stats_test",
            Schema = "public",
            ViewType = ManagedViewType.Materialized,
            Sql = "SELECT COUNT(*)::int as order_count FROM orders_test",
            Source = "test"
        };

        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS public.mv_order_stats_test");
        await context.Database.ExecuteSqlRawAsync(provider.GenerateCreateSql(definition));

        var count = await context.Database
            .SqlQueryRaw<int>("SELECT order_count AS \"Value\" FROM public.mv_order_stats_test")
            .FirstAsync();
        count.Should().Be(2);

        await context.Database.ExecuteSqlRawAsync("INSERT INTO orders_test (total) VALUES (300.00)");
        await context.Database.ExecuteSqlRawAsync(
            provider.GenerateRefreshSql("mv_order_stats_test", "public", false));

        var refreshed = await context.Database
            .SqlQueryRaw<int>("SELECT order_count AS \"Value\" FROM public.mv_order_stats_test")
            .FirstAsync();
        refreshed.Should().Be(3);

        await context.Database.ExecuteSqlRawAsync(provider.GenerateDropSql(definition));
    }
}
