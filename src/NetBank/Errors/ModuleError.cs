namespace NetBank.Errors;

/// <summary>
/// Exception that carries a module-specific error identifier and origin classification.
/// </summary>
public sealed class ModuleException : Exception
{
    public ModuleErrorIdentifier ErrorIdentifier { get; }
    public ErrorOrigin Origin { get; }

    public ModuleException(
        ModuleErrorIdentifier errorIdentifier,
        ErrorOrigin origin,
        string message)
        : base(message)
    {
        ErrorIdentifier = errorIdentifier;
        Origin = origin;
    }

    public ModuleException(
        ModuleErrorIdentifier errorIdentifier,
        ErrorOrigin origin,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ErrorIdentifier = errorIdentifier;
        Origin = origin;
    }

    public override string ToString()
    {
        return $"[{ErrorIdentifier}] [{Origin}] {Message}{Environment.NewLine}{StackTrace}";
    }
}