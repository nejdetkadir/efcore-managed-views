using EntityFrameworkCore.ManagedViews.Abstractions;

namespace EntityFrameworkCore.ManagedViews.Models;

/// <summary>
/// Concrete implementation of a managed view definition.
/// </summary>
public sealed class ManagedViewDefinition : IManagedViewDefinition
{
    /// <inheritdoc />
    public string ViewName { get; init; } = null!;

    /// <inheritdoc />
    public string Schema { get; init; } = "public";

    /// <inheritdoc />
    public ManagedViewType ViewType { get; init; } = ManagedViewType.View;

    /// <inheritdoc />
    public string Sql { get; init; } = null!;

    /// <inheritdoc />
    public IReadOnlyList<string> DependsOn { get; init; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ManagedViewIndex> Indexes { get; init; } = [];

    /// <inheritdoc />
    public string Source { get; init; } = null!;
}
