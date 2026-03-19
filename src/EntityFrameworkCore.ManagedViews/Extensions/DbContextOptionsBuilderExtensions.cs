using EntityFrameworkCore.ManagedViews.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.ManagedViews.Extensions;

/// <summary>
/// Extension methods for <see cref="DbContextOptionsBuilder"/> to enable managed views.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Enables managed view support for this DbContext.
    /// </summary>
    /// <param name="builder">The DbContext options builder.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    public static DbContextOptionsBuilder UseManagedViews(
        this DbContextOptionsBuilder builder,
        Action<ManagedViewOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new ManagedViewOptions();
        configure?.Invoke(options);

        var extension = new ManagedViewOptionsExtension(options);
        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

        return builder;
    }
}
