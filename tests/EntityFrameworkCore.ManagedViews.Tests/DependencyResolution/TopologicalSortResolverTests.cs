using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.Exceptions;
using EntityFrameworkCore.ManagedViews.Models;
using FluentAssertions;
using Xunit;

namespace EntityFrameworkCore.ManagedViews.Tests.DependencyResolution;

public sealed class TopologicalSortResolverTests
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
    public void ResolveCreateOrder_NoDependencies_ReturnsSorted()
    {
        var definitions = new[]
        {
            CreateView("C"),
            CreateView("A"),
            CreateView("B")
        };

        IReadOnlyList<IManagedViewDefinition> result = _sut.ResolveCreateOrder(definitions);

        result.Select(d => d.ViewName).Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public void ResolveCreateOrder_LinearDependency_ReturnsDependenciesFirst()
    {
        var definitions = new[]
        {
            CreateView("C", "B"),
            CreateView("B", "A"),
            CreateView("A")
        };

        IReadOnlyList<IManagedViewDefinition> result = _sut.ResolveCreateOrder(definitions);

        result.Select(d => d.ViewName).Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public void ResolveCreateOrder_DiamondDependency_ResolvesCorrectly()
    {
        var definitions = new[]
        {
            CreateView("D", "B", "C"),
            CreateView("C", "A"),
            CreateView("B", "A"),
            CreateView("A")
        };

        IReadOnlyList<IManagedViewDefinition> result = _sut.ResolveCreateOrder(definitions);

        result[0].ViewName.Should().Be("A");
        result[^1].ViewName.Should().Be("D");
        result.Select(d => d.ViewName).ToList().IndexOf("B").Should().BeLessThan(result.Select(d => d.ViewName).ToList().IndexOf("D"));
        result.Select(d => d.ViewName).ToList().IndexOf("C").Should().BeLessThan(result.Select(d => d.ViewName).ToList().IndexOf("D"));
    }

    [Fact]
    public void ResolveDropOrder_ReturnsReverseOfCreateOrder()
    {
        var definitions = new[]
        {
            CreateView("C", "B"),
            CreateView("B", "A"),
            CreateView("A")
        };

        IReadOnlyList<IManagedViewDefinition> createOrder = _sut.ResolveCreateOrder(definitions);
        IReadOnlyList<IManagedViewDefinition> dropOrder = _sut.ResolveDropOrder(definitions);

        dropOrder.Select(d => d.ViewName).Should().ContainInOrder("C", "B", "A");
    }

    [Fact]
    public void ResolveCreateOrder_CircularDependency_ThrowsException()
    {
        var definitions = new[]
        {
            CreateView("A", "C"),
            CreateView("B", "A"),
            CreateView("C", "B")
        };

        Action act = () => _sut.ResolveCreateOrder(definitions);

        act.Should().Throw<ManagedViewDependencyCycleException>();
    }

    [Fact]
    public void ResolveCreateOrder_ExternalDependency_IgnoresGracefully()
    {
        var definitions = new[]
        {
            CreateView("B", "A", "external_view"),
            CreateView("A")
        };

        IReadOnlyList<IManagedViewDefinition> result = _sut.ResolveCreateOrder(definitions);

        result.Select(d => d.ViewName).Should().ContainInOrder("A", "B");
    }
}
