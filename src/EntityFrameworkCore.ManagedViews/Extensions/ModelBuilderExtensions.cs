using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFrameworkCore.ManagedViews.Extensions;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> to define managed views via fluent API.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Defines a managed view using the fluent API.
    /// The view will be tracked and migrated alongside table schema changes.
    /// </summary>
    public static ModelBuilder HasManagedView(
        this ModelBuilder builder,
        string viewName,
        Action<ManagedViewBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewName);
        ArgumentNullException.ThrowIfNull(configure);

        var viewBuilder = new ManagedViewBuilder();
        configure(viewBuilder);

        var annotation = builder.Model.FindAnnotation(ManagedViewsAnnotationNames.ViewDefinitions);

        var definitions = annotation?.Value as List<ManagedViewDefinition>
            ?? new List<ManagedViewDefinition>();

        definitions.Add(new ManagedViewDefinition
        {
            ViewName = viewName,
            Schema = viewBuilder.SchemaValue ?? "public",
            ViewType = viewBuilder.ViewTypeValue,
            Sql = viewBuilder.SqlValue
                ?? throw new InvalidOperationException(
                    $"View '{viewName}' must have SQL defined via .AsSql()"),
            DependsOn = viewBuilder.Dependencies,
            Indexes = viewBuilder.IndexDefinitions,
            Source = $"FluentApi:{builder.Model.GetType().Name}"
        });

        builder.Model.SetAnnotation(
            ManagedViewsAnnotationNames.ViewDefinitions, definitions);

        return builder;
    }

    /// <summary>
    /// Maps an entity to a managed view for querying.
    /// The entity must be configured as keyless (HasNoKey).
    /// </summary>
    public static EntityTypeBuilder<TEntity> ToManagedView<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string viewName,
        string? schema = null) where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewName);

        builder.ToView(viewName, schema);
        builder.HasAnnotation(ManagedViewsAnnotationNames.IsManagedView, true);
        builder.HasAnnotation(ManagedViewsAnnotationNames.ManagedViewName, viewName);
        return builder;
    }
}
