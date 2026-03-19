namespace EntityFrameworkCore.ManagedViews.Exceptions;

/// <summary>
/// Thrown when a circular dependency is detected among view definitions.
/// </summary>
public sealed class ManagedViewDependencyCycleException : ManagedViewException
{
    /// <summary>
    /// The nodes involved in the dependency cycle.
    /// </summary>
    public IReadOnlyList<string> CycleNodes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewDependencyCycleException"/> class.
    /// </summary>
    public ManagedViewDependencyCycleException()
        : base("A circular dependency was detected among managed view definitions.")
    {
        CycleNodes = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewDependencyCycleException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ManagedViewDependencyCycleException(string message)
        : base(message)
    {
        CycleNodes = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewDependencyCycleException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManagedViewDependencyCycleException(string message, Exception innerException)
        : base(message, innerException)
    {
        CycleNodes = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedViewDependencyCycleException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="cycleNodes">The nodes involved in the dependency cycle.</param>
    public ManagedViewDependencyCycleException(string message,
        IReadOnlyList<string>? cycleNodes = null)
        : base(message)
    {
        CycleNodes = cycleNodes ?? [];
    }
}
