namespace Logistics.EDI.Application.Models;

public sealed record ParsedLoadTenderDocument(
    string? TransactionId,
    string? LoadNumber,
    string? CarrierAlphaCode,
    string? SetPurposeCode,
    string? EstimatedDeliveryDate,
    string? ShipperName,
    IReadOnlyList<ParsedStop> Stops);
