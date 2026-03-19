namespace EntityFrameworkCore.ManagedViews.Exceptions;

/// <summary>
/// Base exception for all ManagedViews errors.
/// </summary>
public class ManagedViewException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewException"/> class.
    /// </summary>
    public ManagedViewException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewException"/> class.
    /// </summary>
    public ManagedViewException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewException"/> class.
    /// </summary>
    public ManagedViewException(string message, Exception inner) : base(message, inner) { }
}
