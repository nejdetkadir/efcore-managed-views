namespace EntityFrameworkCore.ManagedViews.Exceptions;

/// <summary>
/// Thrown for provider-specific errors (e.g., unsupported operations).
/// </summary>
public sealed class ManagedViewProviderException : ManagedViewException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewProviderException"/> class.
    /// </summary>
    public ManagedViewProviderException() : base("A provider-specific error occurred.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewProviderException"/> class.
    /// </summary>
    public ManagedViewProviderException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewProviderException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManagedViewProviderException(string message, Exception innerException) : base(message, innerException) { }
}
