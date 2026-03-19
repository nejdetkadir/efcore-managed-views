using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.IntegrationTests.Infrastructure;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.Tracking;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.IntegrationTests;

[Collection("PostgreSQL")]
public class TrackingTableIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public TrackingTableIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    private (DbContext context, ManagedViewHistoryRepository repo) CreateRepo()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(_fixture.ConnectionString);
        var context = new DbContext(optionsBuilder.Options);
        var viewOptions = Options.Create(new ManagedViewOptions());
        var repo = new ManagedViewHistoryRepository(context, viewOptions);
        return (context, repo);
    }

    [Fact]
    public async Task EnsureCreated_CreatesTrackingTable()
    {
        var (context, repo) = CreateRepo();
        await using var _ = context;

        await repo.EnsureCreatedAsync();

        var entries = await repo.GetAllAsync();
        entries.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordApplied_ThenGetHash_ReturnsHash()
    {
        var (context, repo) = CreateRepo();
        await using var _ = context;
        await repo.EnsureCreatedAsync();

        await repo.RecordAppliedAsync("vw_track_test", "public", "hash123", ManagedViewType.View);

        var hash = await repo.GetHashAsync("vw_track_test", "public");
        hash.Should().Be("hash123");

        await repo.RecordRemovedAsync("vw_track_test", "public");
    }

    [Fact]
    public async Task RecordApplied_Twice_UpdatesHash()
    {
        var (context, repo) = CreateRepo();
        await using var _ = context;
        await repo.EnsureCreatedAsync();

        await repo.RecordAppliedAsync("vw_update_test", "public", "old_hash", ManagedViewType.View);
        await repo.RecordAppliedAsync("vw_update_test", "public", "new_hash", ManagedViewType.View);

        var hash = await repo.GetHashAsync("vw_update_test", "public");
        hash.Should().Be("new_hash");

        await repo.RecordRemovedAsync("vw_update_test", "public");
    }

    [Fact]
    public async Task RecordRemoved_DeletesEntry()
    {
        var (context, repo) = CreateRepo();
        await using var _ = context;
        await repo.EnsureCreatedAsync();

        await repo.RecordAppliedAsync("vw_remove_test", "public", "hash", ManagedViewType.View);
        await repo.RecordRemovedAsync("vw_remove_test", "public");

        var hash = await repo.GetHashAsync("vw_remove_test", "public");
        hash.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ReturnsTrackedEntries()
    {
        var (context, repo) = CreateRepo();
        await using var _ = context;
        await repo.EnsureCreatedAsync();

        await repo.RecordAppliedAsync("vw_getall_1", "public", "h1", ManagedViewType.View);
        await repo.RecordAppliedAsync("mv_getall_2", "public", "h2", ManagedViewType.Materialized);

        var entries = await repo.GetAllAsync();
        entries.Should().Contain(e => e.ViewName == "vw_getall_1");
        entries.Should().Contain(e => e.ViewName == "mv_getall_2" && e.ViewType == "Materialized");

        await repo.RecordRemovedAsync("vw_getall_1", "public");
        await repo.RecordRemovedAsync("mv_getall_2", "public");
    }

    [Fact]
    public async Task GetHash_NonExistent_ReturnsNull()
    {
        var (context, repo) = CreateRepo();
        await using var _ = context;
        await repo.EnsureCreatedAsync();

        var hash = await repo.GetHashAsync("nonexistent_view_xyz", "public");
        hash.Should().BeNull();
    }
}
