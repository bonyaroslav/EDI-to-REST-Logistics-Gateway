namespace Logistics.EDI.Domain.Models;

public sealed record ParsedStop(
    int Sequence,
    string? TypeCode,
    string? Name,
    DateTimeOffset? ScheduledDateTime);
