using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Exceptions;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.Discovery;

public sealed class CompositeViewDiscoveryServiceTests
{
    private static ManagedViewDefinition CreateDef(string name, string schema = "public", string source = "test") => new()
    {
        ViewName = name,
        Schema = schema,
        Sql = $"SELECT * FROM {name}",
        Source = source
    };

    [Fact]
    public void DiscoverAll_NoSources_ReturnsEmpty()
    {
        var sut = new CompositeViewDiscoveryService([]);
        var result = sut.DiscoverAll(new ManagedViewOptions());
        result.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverAll_SingleSource_ReturnsDefinitions()
    {
        var mock = Substitute.For<IManagedViewDiscovery>();
        mock.Discover(Arg.Any<ManagedViewOptions>()).Returns([CreateDef("vw_a"), CreateDef("vw_b")]);

        var sut = new CompositeViewDiscoveryService([mock]);
        var result = sut.DiscoverAll(new ManagedViewOptions());

        result.Should().HaveCount(2);
    }

    [Fact]
    public void DiscoverAll_MultipleSources_AggregatesAll()
    {
        var source1 = Substitute.For<IManagedViewDiscovery>();
        source1.Discover(Arg.Any<ManagedViewOptions>()).Returns([CreateDef("vw_a")]);
        var source2 = Substitute.For<IManagedViewDiscovery>();
        source2.Discover(Arg.Any<ManagedViewOptions>()).Returns([CreateDef("vw_b")]);

        var sut = new CompositeViewDiscoveryService([source1, source2]);
        var result = sut.DiscoverAll(new ManagedViewOptions());

        result.Should().HaveCount(2);
        result.Select(d => d.ViewName).Should().BeEquivalentTo(["vw_a", "vw_b"]);
    }

    [Fact]
    public void DiscoverAll_DuplicateViewName_ThrowsDuplicateException()
    {
        var source1 = Substitute.For<IManagedViewDiscovery>();
        source1.Discover(Arg.Any<ManagedViewOptions>()).Returns([CreateDef("vw_dup", source: "sql_file")]);
        var source2 = Substitute.For<IManagedViewDiscovery>();
        source2.Discover(Arg.Any<ManagedViewOptions>()).Returns([CreateDef("vw_dup", source: "fluent_api")]);

        var sut = new CompositeViewDiscoveryService([source1, source2]);
        var act = () => sut.DiscoverAll(new ManagedViewOptions());

        act.Should().Throw<ManagedViewDuplicateException>()
            .Where(ex => ex.ViewName == "vw_dup" && ex.Source1 == "sql_file" && ex.Source2 == "fluent_api");
    }

    [Fact]
    public void DiscoverAll_SameNameDifferentSchema_DoesNotThrow()
    {
        var source = Substitute.For<IManagedViewDiscovery>();
        source.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            CreateDef("vw_test", "public"),
            CreateDef("vw_test", "catalog")
        ]);

        var sut = new CompositeViewDiscoveryService([source]);
        var result = sut.DiscoverAll(new ManagedViewOptions());

        result.Should().HaveCount(2);
    }

    [Fact]
    public void DiscoverAll_CaseInsensitiveDuplicateDetection()
    {
        var source = Substitute.For<IManagedViewDiscovery>();
        source.Discover(Arg.Any<ManagedViewOptions>()).Returns([
            CreateDef("VW_TEST", "PUBLIC", "source1"),
            CreateDef("vw_test", "public", "source2")
        ]);

        var sut = new CompositeViewDiscoveryService([source]);
        var act = () => sut.DiscoverAll(new ManagedViewOptions());

        act.Should().Throw<ManagedViewDuplicateException>();
    }

    [Fact]
    public void DiscoverAll_EmptySource_ReturnsEmpty()
    {
        var mock = Substitute.For<IManagedViewDiscovery>();
        mock.Discover(Arg.Any<ManagedViewOptions>()).Returns([]);

        var sut = new CompositeViewDiscoveryService([mock]);
        var result = sut.DiscoverAll(new ManagedViewOptions());

        result.Should().BeEmpty();
    }
}
