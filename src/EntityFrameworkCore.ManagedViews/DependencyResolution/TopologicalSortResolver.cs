using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Exceptions;

namespace EntityFrameworkCore.ManagedViews.DependencyResolution;

/// <summary>
/// Performs topological sort on view definitions based on their DependsOn relationships.
/// Uses Kahn's algorithm for cycle detection and deterministic ordering.
/// </summary>
public sealed class TopologicalSortResolver : IManagedViewDependencyResolver
{
    /// <inheritdoc />
    public IReadOnlyList<IManagedViewDefinition> ResolveCreateOrder(
        IReadOnlyList<IManagedViewDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return TopologicalSort(definitions);
    }

    /// <inheritdoc />
    public IReadOnlyList<IManagedViewDefinition> ResolveDropOrder(
        IReadOnlyList<IManagedViewDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        List<IManagedViewDefinition> createOrder = TopologicalSort(definitions);
        createOrder.Reverse();
        return createOrder;
    }

    private static List<IManagedViewDefinition> TopologicalSort(
        IReadOnlyList<IManagedViewDefinition> definitions)
    {
        var lookup = definitions.ToDictionary(d => d.ViewName, StringComparer.OrdinalIgnoreCase);
        var inDegree = definitions.ToDictionary(d => d.ViewName, _ => 0,
            StringComparer.OrdinalIgnoreCase);
        var adjacency = definitions.ToDictionary(d => d.ViewName, _ => new List<string>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (IManagedViewDefinition def in definitions)
        {
            foreach (string dep in def.DependsOn)
            {
                if (adjacency.TryGetValue(dep, out List<string>? dependents))
                {
                    dependents.Add(def.ViewName);
                    inDegree[def.ViewName]++;
                }
            }
        }

        var queue = new Queue<string>(
            inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key).Order());
        var result = new List<IManagedViewDefinition>();

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            result.Add(lookup[current]);

            foreach (string neighbor in adjacency[current].Order())
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (result.Count != definitions.Count)
        {
            List<string> cycleNodes = inDegree
                .Where(kv => kv.Value > 0)
                .Select(kv => kv.Key)
                .ToList();

            throw new ManagedViewDependencyCycleException(
                $"Circular dependency detected among views: {string.Join(" \u2192 ", cycleNodes)}. " +
                $"Break the cycle by removing or restructuring dependencies.",
                cycleNodes);
        }

        return result;
    }
}
