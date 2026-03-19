namespace EntityFrameworkCore.ManagedViews.Abstractions;

/// <summary>
/// Resolves view dependencies and produces a topologically sorted
/// execution order for CREATE and DROP operations.
/// </summary>
public interface IManagedViewDependencyResolver
{
    /// <summary>
    /// Returns views in CREATE order (dependencies first).
    /// Throws <see cref="Exceptions.ManagedViewDependencyCycleException"/> if a cycle is detected.
    /// </summary>
    IReadOnlyList<IManagedViewDefinition> ResolveCreateOrder(
        IReadOnlyList<IManagedViewDefinition> definitions);

    /// <summary>
    /// Returns views in DROP order (dependents first, then dependencies).
    /// This is the reverse of CREATE order.
    /// </summary>
    IReadOnlyList<IManagedViewDefinition> ResolveDropOrder(
        IReadOnlyList<IManagedViewDefinition> definitions);
}
