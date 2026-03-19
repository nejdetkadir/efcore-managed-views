namespace EntityFrameworkCore.ManagedViews.Configuration;

/// <summary>
/// Constants for EF Core model annotation keys used by ManagedViews.
/// </summary>
public static class ManagedViewsAnnotationNames
{
    /// <summary>
    /// Annotation key for storing fluent API view definitions on the model.
    /// </summary>
    public const string ViewDefinitions = "ManagedViews:Definitions";

    /// <summary>
    /// Annotation key marking an entity as mapped to a managed view.
    /// </summary>
    public const string IsManagedView = "ManagedViews:IsManagedView";

    /// <summary>
    /// Annotation key for the managed view name on an entity type.
    /// </summary>
    public const string ManagedViewName = "ManagedViews:ViewName";
}
