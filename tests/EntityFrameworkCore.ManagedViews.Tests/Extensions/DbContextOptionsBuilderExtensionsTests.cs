using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Extensions;

public sealed class DbContextOptionsBuilderExtensionsTests
{
    [Fact]
    public void UseManagedViews_AddsExtension()
    {
        var builder = new DbContextOptionsBuilder()
            .UseInMemoryDatabase("test_usemv");

        builder.UseManagedViews();

        var ext = builder.Options.FindExtension<ManagedViewOptionsExtension>();
        ext.Should().NotBeNull();
    }

    [Fact]
    public void UseManagedViews_WithConfigure_AppliesOptions()
    {
        var builder = new DbContextOptionsBuilder()
            .UseInMemoryDatabase("test_usemv_config");

        builder.UseManagedViews(o => o.DefaultSchema = "custom_schema");

        var ext = builder.Options.FindExtension<ManagedViewOptionsExtension>();
        ext.Should().NotBeNull();
        ext!.Options.DefaultSchema.Should().Be("custom_schema");
    }

    [Fact]
    public void UseManagedViews_WithoutConfigure_UsesDefaults()
    {
        var builder = new DbContextOptionsBuilder()
            .UseInMemoryDatabase("test_usemv_default");

        builder.UseManagedViews();

        var ext = builder.Options.FindExtension<ManagedViewOptionsExtension>();
        ext!.Options.DefaultSchema.Should().Be("public");
        ext.Options.TrackingTableName.Should().Be("__ManagedViewsHistory");
    }

    [Fact]
    public void UseManagedViews_ReturnsSameBuilder()
    {
        var builder = new DbContextOptionsBuilder()
            .UseInMemoryDatabase("test_usemv_chain");

        var result = builder.UseManagedViews();

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void UseManagedViews_NullBuilder_ThrowsArgumentNullException()
    {
        DbContextOptionsBuilder builder = null!;
        var act = () => builder.UseManagedViews();
        act.Should().Throw<ArgumentNullException>();
    }
}
