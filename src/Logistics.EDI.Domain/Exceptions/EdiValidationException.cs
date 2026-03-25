namespace Logistics.EDI.Domain.Exceptions;

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
