namespace EntityFrameworkCore.ManagedViews.Exceptions;

/// <summary>
/// Thrown when a .sql file has invalid metadata or structure.
/// </summary>
public sealed class ManagedViewSqlParseException : ManagedViewException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewSqlParseException"/> class.
    /// </summary>
    public ManagedViewSqlParseException() : base("A managed view SQL file has invalid metadata or structure.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewSqlParseException"/> class.
    /// </summary>
    public ManagedViewSqlParseException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewSqlParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManagedViewSqlParseException(string message, Exception innerException) : base(message, innerException) { }
}
