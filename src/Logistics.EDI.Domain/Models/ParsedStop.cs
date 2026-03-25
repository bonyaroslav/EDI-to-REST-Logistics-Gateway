namespace Logistics.EDI.Domain.Models;

/// <summary>
/// Parsed stop data needed by Application for v1 mapping and validation.
/// Sequence is API-visible order, TypeCode is the parser-normalized stop kind,
/// and the optional fields are already normalized enough for direct serialization.
/// </summary>
public sealed record ParsedStop(
    int Sequence,
    string? TypeCode,
    string? Name,
    DateTimeOffset? ScheduledDateTime);
