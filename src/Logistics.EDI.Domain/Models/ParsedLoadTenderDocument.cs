namespace Logistics.EDI.Domain.Models;

public sealed record ParsedLoadTenderDocument(
    string? TransactionId,
    string? LoadNumber,
    string? CarrierAlphaCode,
    string? SetPurposeCode,
    DateOnly? EstimatedDeliveryDate,
    string? ShipperName,
    IReadOnlyList<ParsedStop> Stops);
