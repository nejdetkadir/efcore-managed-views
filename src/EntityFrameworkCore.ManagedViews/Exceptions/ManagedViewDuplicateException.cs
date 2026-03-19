namespace EntityFrameworkCore.ManagedViews.Exceptions;

/// <summary>
/// Thrown when a view is defined in multiple sources.
/// </summary>
public sealed class ManagedViewDuplicateException : ManagedViewException
{
    /// <summary>
    /// The name of the duplicated view.
    /// </summary>
    public string ViewName { get; }

    /// <summary>
    /// The first source where the view was defined.
    /// </summary>
    public string Source1 { get; }

    /// <summary>
    /// The second source where the view was defined.
    /// </summary>
    public string Source2 { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewDuplicateException"/> class.
    /// </summary>
    public ManagedViewDuplicateException()
        : base("A managed view is defined in multiple sources.")
    {
        ViewName = string.Empty;
        Source1 = string.Empty;
        Source2 = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewDuplicateException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ManagedViewDuplicateException(string message)
        : base(message)
    {
        ViewName = string.Empty;
        Source1 = string.Empty;
        Source2 = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewDuplicateException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManagedViewDuplicateException(string message, Exception innerException)
        : base(message, innerException)
    {
        ViewName = string.Empty;
        Source1 = string.Empty;
        Source2 = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewDuplicateException"/> class.
    /// </summary>
    public ManagedViewDuplicateException(string viewName, string source1, string source2)
        : base($"View '{viewName}' is defined in both '{source1}' and '{source2}'. " +
               $"Remove one definition to resolve the conflict.")
    {
        ViewName = viewName;
        Source1 = source1;
        Source2 = source2;
    }
}
