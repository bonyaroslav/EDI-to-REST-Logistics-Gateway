namespace Logistics.EDI.Application.Contracts;

/// <summary>
/// Public response contract for the portfolio demo's 204-to-JSON translation flow.
/// </summary>
public sealed record LoadTenderResponse(
    string TransactionId,
    string LoadNumber,
    string CarrierAlphaCode,
    string SetPurpose,
    string? EstimatedDeliveryDate,
    string? ShipperName,
    IReadOnlyList<StopDto> Stops,
    string Status);
