using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Exceptions;

namespace EntityFrameworkCore.ManagedViews.Discovery;

/// <summary>
/// Aggregates view definitions from all discovery sources and detects duplicates.
/// </summary>
public sealed class CompositeViewDiscoveryService
{
    private readonly IEnumerable<IManagedViewDiscovery> _discoveryServices;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeViewDiscoveryService"/> class.
    /// </summary>
    public CompositeViewDiscoveryService(IEnumerable<IManagedViewDiscovery> discoveryServices)
    {
        _discoveryServices = discoveryServices;
    }

    /// <summary>
    /// Discovers all view definitions from all sources and validates for duplicates.
    /// </summary>
    public IReadOnlyList<IManagedViewDefinition> DiscoverAll(ManagedViewOptions options)
    {
        var allDefinitions = new List<IManagedViewDefinition>();
        var seen = new Dictionary<string, IManagedViewDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (IManagedViewDiscovery service in _discoveryServices)
        {
            IReadOnlyList<IManagedViewDefinition> definitions = service.Discover(options);

            foreach (IManagedViewDefinition definition in definitions)
            {
                string key = $"{definition.Schema}.{definition.ViewName}";

                if (seen.TryGetValue(key, out IManagedViewDefinition? existing))
                {
                    throw new ManagedViewDuplicateException(
                        definition.ViewName, existing.Source, definition.Source);
                }

                seen[key] = definition;
                allDefinitions.Add(definition);
            }
        }

        return allDefinitions;
    }
}
