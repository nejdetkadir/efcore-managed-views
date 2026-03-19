using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.PostgreSQL.Tests;

public sealed class NpgsqlDbContextOptionsBuilderExtensionsTests
{
    [Fact]
    public void UseNpgsqlManagedViews_AddsExtension()
    {
        var builder = new DbContextOptionsBuilder()
            .UseInMemoryDatabase("test_npgsql_ext");

        builder.UseNpgsqlManagedViews();

        var ext = builder.Options.FindExtension<ManagedViewOptionsExtension>();
        ext.Should().NotBeNull();
    }

    [Fact]
    public void UseNpgsqlManagedViews_WithConfigure_AppliesOptions()
    {
        var builder = new DbContextOptionsBuilder()
            .UseInMemoryDatabase("test_npgsql_ext_cfg");

        builder.UseNpgsqlManagedViews(o => o.DefaultSchema = "npgsql_schema");

        var ext = builder.Options.FindExtension<ManagedViewOptionsExtension>();
        ext!.Options.DefaultSchema.Should().Be("npgsql_schema");
    }

    [Fact]
    public void UseNpgsqlManagedViews_NullBuilder_ThrowsArgumentNullException()
    {
        DbContextOptionsBuilder builder = null!;
        var act = () => builder.UseNpgsqlManagedViews();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseNpgsqlManagedViews_ReturnsSameBuilder()
    {
        var builder = new DbContextOptionsBuilder()
            .UseInMemoryDatabase("test_npgsql_chain");

        var result = builder.UseNpgsqlManagedViews();

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void UseNpgsqlManagedViews_RegistersNpgsqlExtension()
    {
        var builder = new DbContextOptionsBuilder()
            .UseInMemoryDatabase("test_npgsql_ext_reg");

        builder.UseNpgsqlManagedViews();

        var ext = builder.Options.FindExtension<EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions.NpgsqlManagedViewOptionsExtension>();
        ext.Should().NotBeNull();
    }

    [Fact]
    public void NpgsqlExtension_Info_IsDatabaseProvider_IsFalse()
    {
        var ext = new EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions.NpgsqlManagedViewOptionsExtension();
        ext.Info.IsDatabaseProvider.Should().BeFalse();
    }

    [Fact]
    public void NpgsqlExtension_Info_LogFragment_ContainsNpgsqlManagedViews()
    {
        var ext = new EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions.NpgsqlManagedViewOptionsExtension();
        ext.Info.LogFragment.Should().Contain("NpgsqlManagedViews");
    }

    [Fact]
    public void NpgsqlExtension_Info_GetServiceProviderHashCode()
    {
        var ext = new EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions.NpgsqlManagedViewOptionsExtension();
        ext.Info.GetServiceProviderHashCode().Should().Be(0);
    }

    [Fact]
    public void NpgsqlExtension_Info_ShouldUseSameServiceProvider()
    {
        var ext1 = new EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions.NpgsqlManagedViewOptionsExtension();
        var ext2 = new EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions.NpgsqlManagedViewOptionsExtension();
        ext1.Info.ShouldUseSameServiceProvider(ext2.Info).Should().BeTrue();
    }

    [Fact]
    public void NpgsqlExtension_Info_PopulateDebugInfo()
    {
        var ext = new EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions.NpgsqlManagedViewOptionsExtension();
        var debugInfo = new Dictionary<string, string>();
        ext.Info.PopulateDebugInfo(debugInfo);
        debugInfo.Should().ContainKey("NpgsqlManagedViews:Enabled");
    }

    [Fact]
    public void NpgsqlExtension_Validate_DoesNotThrow()
    {
        var ext = new EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions.NpgsqlManagedViewOptionsExtension();
        var builder = new DbContextOptionsBuilder().UseInMemoryDatabase("test_validate_npgsql");
        var act = () => ext.Validate(builder.Options);
        act.Should().NotThrow();
    }

    [Fact]
    public void NpgsqlExtension_ApplyServices_RegistersProvider()
    {
        var ext = new EntityFrameworkCore.ManagedViews.PostgreSQL.Extensions.NpgsqlManagedViewOptionsExtension();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        ext.ApplyServices(services);

        using var provider = services.BuildServiceProvider();
        var viewProvider = provider.GetService<EntityFrameworkCore.ManagedViews.Abstractions.IManagedViewProvider>();
        viewProvider.Should().NotBeNull();
        viewProvider.Should().BeOfType<PostgreSqlManagedViewProvider>();
    }
}
