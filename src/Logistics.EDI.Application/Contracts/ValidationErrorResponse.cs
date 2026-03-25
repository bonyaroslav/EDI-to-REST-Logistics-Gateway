namespace Logistics.EDI.Application.Contracts;

public sealed record ValidationErrorResponse(
    string Error,
    string Message,
    int Status);
