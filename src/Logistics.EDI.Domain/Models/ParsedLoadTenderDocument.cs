namespace Logistics.EDI.Domain.Models;

/// <summary>
/// Canonical parsed representation shared between Infrastructure and Application.
/// Missing business values are represented as <see langword="null"/> so Application
/// can own the user-facing validation behavior.
/// </summary>
public sealed record ParsedLoadTenderDocument(
    string? TransactionId,
    string? LoadNumber,
    string? CarrierAlphaCode,
    string? SetPurposeCode,
    DateOnly? EstimatedDeliveryDate,
    string? ShipperName,
    IReadOnlyList<ParsedStop> Stops);
