using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.Exceptions;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.DependencyResolution;

public sealed class TopologicalSortResolverEdgeCaseTests
{
    private readonly TopologicalSortResolver _sut = new();

    private static ManagedViewDefinition CreateView(string name, params string[] dependsOn) => new()
    {
        ViewName = name,
        Schema = "public",
        Sql = $"SELECT 1 FROM {name}",
        DependsOn = dependsOn,
        Source = "test"
    };

    [Fact]
    public void ResolveCreateOrder_EmptyList_ReturnsEmpty()
    {
        var result = _sut.ResolveCreateOrder([]);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ResolveDropOrder_EmptyList_ReturnsEmpty()
    {
        var result = _sut.ResolveDropOrder([]);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ResolveCreateOrder_SingleView_ReturnsSingle()
    {
        var result = _sut.ResolveCreateOrder([CreateView("A")]);
        result.Should().ContainSingle();
        result[0].ViewName.Should().Be("A");
    }

    [Fact]
    public void ResolveCreateOrder_NullDefinitions_ThrowsArgumentNullException()
    {
        var act = () => _sut.ResolveCreateOrder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResolveDropOrder_NullDefinitions_ThrowsArgumentNullException()
    {
        var act = () => _sut.ResolveDropOrder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResolveCreateOrder_SelfDependency_ThrowsCycleException()
    {
        var definitions = new[] { CreateView("A", "A") };

        var act = () => _sut.ResolveCreateOrder(definitions);

        act.Should().Throw<ManagedViewDependencyCycleException>();
    }

    [Fact]
    public void ResolveCreateOrder_TwoWayCycle_ThrowsCycleException()
    {
        var definitions = new[]
        {
            CreateView("A", "B"),
            CreateView("B", "A")
        };

        var act = () => _sut.ResolveCreateOrder(definitions);

        act.Should().Throw<ManagedViewDependencyCycleException>();
    }

    [Fact]
    public void ResolveCreateOrder_LongChain_ResolvesCorrectly()
    {
        var definitions = new[]
        {
            CreateView("E", "D"),
            CreateView("D", "C"),
            CreateView("C", "B"),
            CreateView("B", "A"),
            CreateView("A")
        };

        var result = _sut.ResolveCreateOrder(definitions);
        var names = result.Select(d => d.ViewName).ToList();

        names.IndexOf("A").Should().BeLessThan(names.IndexOf("B"));
        names.IndexOf("B").Should().BeLessThan(names.IndexOf("C"));
        names.IndexOf("C").Should().BeLessThan(names.IndexOf("D"));
        names.IndexOf("D").Should().BeLessThan(names.IndexOf("E"));
    }

    [Fact]
    public void ResolveDropOrder_LongChain_ReversesOrder()
    {
        var definitions = new[]
        {
            CreateView("C", "B"),
            CreateView("B", "A"),
            CreateView("A")
        };

        var result = _sut.ResolveDropOrder(definitions);
        var names = result.Select(d => d.ViewName).ToList();

        names.IndexOf("C").Should().BeLessThan(names.IndexOf("B"));
        names.IndexOf("B").Should().BeLessThan(names.IndexOf("A"));
    }

    [Fact]
    public void ResolveCreateOrder_MultipleDependenciesOnSameView()
    {
        var definitions = new[]
        {
            CreateView("C", "A"),
            CreateView("B", "A"),
            CreateView("A")
        };

        var result = _sut.ResolveCreateOrder(definitions);
        var names = result.Select(d => d.ViewName).ToList();

        names.IndexOf("A").Should().BeLessThan(names.IndexOf("B"));
        names.IndexOf("A").Should().BeLessThan(names.IndexOf("C"));
    }

    [Fact]
    public void ResolveCreateOrder_ComplexGraph_WithDiamond()
    {
        var definitions = new[]
        {
            CreateView("F", "D", "E"),
            CreateView("E", "C"),
            CreateView("D", "B", "C"),
            CreateView("C", "A"),
            CreateView("B", "A"),
            CreateView("A")
        };

        var result = _sut.ResolveCreateOrder(definitions);
        var names = result.Select(d => d.ViewName).ToList();

        names[0].Should().Be("A");
        names[^1].Should().Be("F");
    }
}
