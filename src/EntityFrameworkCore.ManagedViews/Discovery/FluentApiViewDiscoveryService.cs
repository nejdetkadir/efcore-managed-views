using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Models;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.ManagedViews.Discovery;

/// <summary>
/// Discovers view definitions from EF Core model annotations set via fluent API.
/// </summary>
public sealed class FluentApiViewDiscoveryService : IManagedViewDiscovery
{
    private readonly DbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentApiViewDiscoveryService"/> class.
    /// </summary>
    public FluentApiViewDiscoveryService(DbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public IReadOnlyList<IManagedViewDefinition> Discover(ManagedViewOptions options)
    {
        var annotation = _context.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions);

        if (annotation?.Value is not List<ManagedViewDefinition> definitions)
        {
            return [];
        }

        return definitions;
    }
}
