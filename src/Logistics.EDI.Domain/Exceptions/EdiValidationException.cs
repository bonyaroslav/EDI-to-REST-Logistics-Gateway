namespace Logistics.EDI.Domain.Exceptions;

/// <summary>
/// Represents predictable validation failures that should flow back to the API as
/// structured client errors instead of parser-specific exceptions.
/// </summary>
public sealed class EdiValidationException : Exception
{
    public EdiValidationException(string message)
        : base(message)
    {
    }

    public EdiValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
