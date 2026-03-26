namespace Logistics.EDI.Domain.Models;

/// <summary>
/// Parsed stop data needed by Application for v1 mapping and validation.
/// Sequence is API-visible order and TypeCode is the parser-normalized stop kind.
/// </summary>
public sealed record ParsedStop(
    int Sequence,
    string? TypeCode,
    string? Name);
