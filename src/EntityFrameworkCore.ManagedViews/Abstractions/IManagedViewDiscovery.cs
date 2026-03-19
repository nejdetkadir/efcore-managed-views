using EntityFrameworkCore.ManagedViews.Configuration;

namespace EntityFrameworkCore.ManagedViews.Abstractions;

/// <summary>
/// Discovers managed view definitions from a specific source
/// (e.g., embedded .sql files, fluent API configuration).
/// </summary>
public interface IManagedViewDiscovery
{
    /// <summary>
    /// Discovers all view definitions from this source.
    /// </summary>
    /// <param name="options">Global configuration options.</param>
    /// <returns>Collection of discovered view definitions.</returns>
    IReadOnlyList<IManagedViewDefinition> Discover(ManagedViewOptions options);
}
