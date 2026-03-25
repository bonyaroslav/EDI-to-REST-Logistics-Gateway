namespace Logistics.EDI.Application.Models;

public sealed record ParsedStop(
    int Sequence,
    string? TypeCode,
    string? Name,
    string? ScheduledDateTime);
