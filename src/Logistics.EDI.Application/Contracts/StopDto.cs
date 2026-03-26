namespace Logistics.EDI.Application.Contracts;

/// <summary>
/// Minimal API-facing stop shape for the translated load tender response.
/// </summary>
public sealed record StopDto(
    int Sequence,
    string Type,
    string? Name);
