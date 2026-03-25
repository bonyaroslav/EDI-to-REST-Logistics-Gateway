namespace Logistics.EDI.Application.Contracts;

/// <summary>
/// Stable structured error response returned for validation and request-shape failures.
/// </summary>
public sealed record ValidationErrorResponse(
    string Error,
    string Message,
    int Status);
