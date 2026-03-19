using EntityFrameworkCore.ManagedViews.Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Configuration;

public sealed class ManagedViewOptionsExtensionTests
{
    [Fact]
    public void Constructor_StoresOptions()
    {
        var options = new ManagedViewOptions { DefaultSchema = "custom" };
        var ext = new ManagedViewOptionsExtension(options);
        ext.Options.Should().BeSameAs(options);
    }

    [Fact]
    public void Info_ReturnsNonNull()
    {
        var ext = new ManagedViewOptionsExtension(new ManagedViewOptions());
        ext.Info.Should().NotBeNull();
    }

    [Fact]
    public void Info_IsDatabaseProvider_IsFalse()
    {
        var ext = new ManagedViewOptionsExtension(new ManagedViewOptions());
        ext.Info.IsDatabaseProvider.Should().BeFalse();
    }

    [Fact]
    public void Info_LogFragment_ContainsManagedViews()
    {
        var ext = new ManagedViewOptionsExtension(new ManagedViewOptions());
        ext.Info.LogFragment.Should().Contain("ManagedViews");
    }

    [Fact]
    public void Info_GetServiceProviderHashCode_ReturnsZero()
    {
        var ext = new ManagedViewOptionsExtension(new ManagedViewOptions());
        ext.Info.GetServiceProviderHashCode().Should().Be(0);
    }

    [Fact]
    public void Info_ShouldUseSameServiceProvider_SameType_ReturnsTrue()
    {
        var ext1 = new ManagedViewOptionsExtension(new ManagedViewOptions());
        var ext2 = new ManagedViewOptionsExtension(new ManagedViewOptions());
        ext1.Info.ShouldUseSameServiceProvider(ext2.Info).Should().BeTrue();
    }

    [Fact]
    public void Info_PopulateDebugInfo_AddsEnabledEntry()
    {
        var ext = new ManagedViewOptionsExtension(new ManagedViewOptions());
        var debugInfo = new Dictionary<string, string>();
        ext.Info.PopulateDebugInfo(debugInfo);
        debugInfo.Should().ContainKey("ManagedViews:Enabled");
        debugInfo["ManagedViews:Enabled"].Should().Be("true");
    }

    [Fact]
    public void Validate_DoesNotThrow()
    {
        var ext = new ManagedViewOptionsExtension(new ManagedViewOptions());
        var optionsBuilder = new DbContextOptionsBuilder().UseInMemoryDatabase("test_validate");
        var act = () => ext.Validate(optionsBuilder.Options);
        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyServices_RegistersOptions()
    {
        var options = new ManagedViewOptions { DefaultSchema = "test_schema" };
        var ext = new ManagedViewOptionsExtension(options);
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        ext.ApplyServices(services);

        using var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
        var resolved = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<Microsoft.Extensions.Options.IOptions<ManagedViewOptions>>(provider);
        resolved.Should().NotBeNull();
        resolved!.Value.DefaultSchema.Should().Be("test_schema");
    }
}
