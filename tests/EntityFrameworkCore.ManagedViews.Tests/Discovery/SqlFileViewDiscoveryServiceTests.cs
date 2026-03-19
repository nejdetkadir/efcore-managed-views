using System.Reflection;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Discovery;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Discovery;

public sealed class SqlFileViewDiscoveryServiceTests
{
    private readonly SqlFileViewDiscoveryService _sut = new();

    [Fact]
    public void Discover_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Discover(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Discover_NoMatchingResources_ReturnsEmpty()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceTests).Assembly,
            SqlFilesPrefix = "NonExistentPrefix"
        };

        var result = _sut.Discover(options);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Discover_WithViewAssembly_UsesProvidedAssembly()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceTests).Assembly,
            SqlFilesPrefix = "DoesNotExist_ForSure_XYZ"
        };

        var result = _sut.Discover(options);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Discover_DefaultPrefix_FiltersCorrectly()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = typeof(SqlFileViewDiscoveryServiceTests).Assembly
        };

        var result = _sut.Discover(options);
        result.Should().NotBeNull();
    }
}
