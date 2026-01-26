namespace NetBank.Errors;

/// <summary>
/// Exception that carries a module-specific error identifier and origin classification.
/// </summary>
public sealed class ModuleException : Exception
{
    public object ErrorPayload { get; }
    public ErrorOrigin Origin { get; }

    public ModuleException(object errorPayload, ErrorOrigin origin, string internalLogMessage)
        : base(internalLogMessage)
    {
        ErrorPayload = errorPayload;
        Origin = origin;
    }
}