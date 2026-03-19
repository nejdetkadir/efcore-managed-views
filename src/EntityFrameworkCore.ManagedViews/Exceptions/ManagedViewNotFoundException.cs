namespace EntityFrameworkCore.ManagedViews.Exceptions;

/// <summary>
/// Thrown when a referenced view definition cannot be found.
/// </summary>
public sealed class ManagedViewNotFoundException : ManagedViewException
{
    /// <summary>
    /// The name of the view that was not found.
    /// </summary>
    public string ViewName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewNotFoundException"/> class.
    /// </summary>
    public ManagedViewNotFoundException()
        : base("A managed view was not found.")
    {
        ViewName = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewNotFoundException"/> class.
    /// </summary>
    public ManagedViewNotFoundException(string viewName)
        : base($"Managed view '{viewName}' not found. " +
               $"Ensure the .sql file exists or the view is defined via fluent API.")
    {
        ViewName = viewName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManagedViewNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        ViewName = string.Empty;
    }
}
