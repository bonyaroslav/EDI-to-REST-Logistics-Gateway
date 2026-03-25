namespace Logistics.EDI.Application.Contracts;

public sealed record StopDto(
    int Sequence,
    string Type,
    string? Name,
    string? ScheduledDateTime);
