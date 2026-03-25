namespace Logistics.EDI.Application.Contracts;

public sealed record LoadTenderResponse(
    string TransactionId,
    string LoadNumber,
    string CarrierAlphaCode,
    string SetPurpose,
    string? EstimatedDeliveryDate,
    string? ShipperName,
    IReadOnlyList<StopDto> Stops,
    string Status);
